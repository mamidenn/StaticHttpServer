using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace StaticHttpServer
{
    public enum HttpRequestMethod
    {
        Get,
        Post
    }
    public class HttpRequest
    {
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
        public HttpRequestMethod Method;
        public string Path;
        public string Version;

        public static HttpRequest Parse(string requestString)
        {
            var request = new HttpRequest();
            var lines = requestString.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var firstLine = true;
            foreach (var line in lines)
            {
                if (firstLine)
                {
                    var values = line.Split(' ');
                    var method = TypeDescriptor.GetConverter(typeof(HttpRequestMethod))
                        .ConvertFrom(values[0]);
                    if (method != null)
                    {
                        request.Method = (HttpRequestMethod) method;
                    }
                    else
                    {
                        throw new InvalidHttpRequestException();
                    }
                    request.Path = values[1];
                    request.Version = values[2];
                    firstLine = false;
                }
                else
                {
                    var keyValuePair = line.Split(new[] {": "}, StringSplitOptions.None);
                    request.Headers[keyValuePair[0]] = keyValuePair[1];
                }
            }
            return request;
        }
    }
}