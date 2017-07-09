using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace StaticHttpServer
{
    public class ConnectionState
    {
        public Socket Handler;
        public readonly byte[] Buffer = new byte[64];
        public string Data;
    }
    /// <summary>
    /// Yes, I know of TcpListener. This class implements the functionality
    /// itself, because it's in the spirit of the challenge.
    /// </summary>
    public class HttpServer : IDisposable
    {
        private const string DefaultPage = "index.html";
        private readonly Socket _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        private readonly Uri _documentRoot;
        private static readonly ManualResetEvent Semaphore = new ManualResetEvent(true);

        public HttpServer(string hostIp, int port, string documentRoot, int maxNumConnections = 10)
        {
            _documentRoot = new Uri(documentRoot);
            _listener.Bind(new IPEndPoint(IPAddress.Parse(hostIp), port));
            _listener.Listen(maxNumConnections);
        }

        public void Start()
        {
            Console.WriteLine("Waiting for Connections at {0}", _listener.LocalEndPoint);
            while (true)
            {
                Semaphore.Reset();
                _listener.BeginAccept(AcceptCallback, _listener);
                Semaphore.WaitOne();
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Semaphore.Set();
            var listener = (Socket) ar.AsyncState;
            var handler = listener.EndAccept(ar);
            var state = new ConnectionState {Handler = handler};
            handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ReceiveCallback, state);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var state = (ConnectionState) ar.AsyncState;
            var handler = state.Handler;
            var fragmentLength = handler.EndReceive(ar);
            state.Data += Encoding.ASCII.GetString(state.Buffer, 0, fragmentLength);
            if (!state.Data.EndsWith("\r\n\r\n"))
            {
                handler.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ReceiveCallback, state);
            }
            else
            {
                var request = HttpRequest.Parse(state.Data);
                Console.WriteLine("RECV {0} {1} {2} {3}",
                    handler.RemoteEndPoint,
                    request.Method,
                    request.Path,
                    request.Version);
                var file = new Uri(Path.Combine(_documentRoot.AbsolutePath,
                    request.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                if (!_documentRoot.IsBaseOf(file))
                {
                    Send(handler, "HTTP/1.0 403 Forbidden");
                }
                else
                {
                    if (string.IsNullOrEmpty(Path.GetFileName(file.AbsolutePath)))
                    {
                        file = new Uri(Path.Combine(file.AbsolutePath, DefaultPage));
                    }
                    SendFile(handler, file.AbsolutePath);
                }
            }
        }

        private static void Send(Socket handler, string message)
        {
            var data = Encoding.ASCII.GetBytes(message);
            handler.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            var handler = (Socket) ar.AsyncState;
            handler.EndSend(ar);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
            handler.Dispose();
        }

        private static void SendFile(Socket handler, string absolutePath)
        {
            if (File.Exists(absolutePath))
            {
                var content = File.ReadAllText(absolutePath);
                SendData(handler, content, "text/html");
            }
            else
            {
                Send(handler, "HTTP/1.0 404 Not Found");
            }
        }

        private static void SendData(Socket handler, string content, string contentType)
        {
            var data = "HTTP/1.0 200 OK\r\n";
            data += $"Content-Type: {contentType}\r\n";
            data += "\r\n";
            data += $"{content}\r\n";
            Send(handler, data);
        }

        public void Dispose()
        {
            _listener?.Dispose();
        }
    }
}