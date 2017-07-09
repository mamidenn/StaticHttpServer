using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticHttpServer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var server = new HttpServer("127.0.0.1", 8080, @"C:\Users\Martin\Desktop\");
            server.Start();
        }
    }
}
