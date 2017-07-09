using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StaticHttpServer
{
    /// <summary>
    /// Yes, I know of TcpListener. This class implements the functionality
    /// itself, because it's in the spirit of the challenge.
    /// </summary>
    public class HttpServer : IDisposable
    {
        private const string DefaultPage = "index.html";
        private readonly int _maxNumConnections;
        private readonly Socket _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        private readonly string _hostIp;
        private readonly int _port;
        private readonly Uri _documentRoot;

        public HttpServer(string hostIp, int port, string documentRoot, int maxNumConnections = 10)
        {
            _port = port;
            _documentRoot = new Uri(documentRoot);
            _maxNumConnections = maxNumConnections;
            _hostIp = hostIp;
            _listener.Bind(new IPEndPoint(IPAddress.Parse(_hostIp), port));
        }

        public void Start()
        {
            _listener.Listen(_maxNumConnections);
            while (true)
            {
                Console.WriteLine("Waiting for Connection at {0}:{1}", _hostIp, _port);
                using (var handler = _listener.Accept())
                {
                    HandleConnection(handler);
                }
            }
        }

        private void HandleConnection(Socket handler)
        {
            string requestString = null;
            while (true)
            {
                var fragment = new byte[1024];
                var fragmetLength = handler.Receive(fragment);
                requestString += Encoding.ASCII.GetString(fragment, 0, fragmetLength);
                if (requestString.EndsWith("\r\n\r\n"))
                {
                    break;
                }
            }
            var request = HttpRequest.Parse(requestString);
            Console.WriteLine("Connection from {0} {1} {2} {3}",
                handler.RemoteEndPoint,
                request.Method,
                request.Path,
                request.Version);
            var file = new Uri(Path.Combine(_documentRoot.AbsolutePath, request.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            if (!_documentRoot.IsBaseOf(file))
            {
                handler.Send(Encoding.ASCII.GetBytes("HTTP/1.0 403 Forbidden"));
            }
            else
            {
                if (string.IsNullOrEmpty(Path.GetFileName(file.AbsolutePath)))
                {
                    file = new Uri(Path.Combine(file.AbsolutePath, DefaultPage));
                }
                SendFile(handler, file.AbsolutePath);
            }
            handler.Close();
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
                handler.Send(Encoding.ASCII.GetBytes("HTTP/1.0 404 Not Found"));
            }
        }

        private static void SendData(Socket handler, string content, string contentType)
        {
            var data = "HTTP/1.0 200 OK\r\n";
            data += $"Content-Type: {contentType}\r\n";
            data += "\r\n";
            data += $"{content}\r\n";
            handler.Send(Encoding.ASCII.GetBytes(data));
        }

        public void Dispose()
        {
            _listener?.Dispose();
        }
    }
}