using System.Net;

namespace NanoleafAPI
{
    public readonly struct Request
    {
        public readonly string IP;
        public readonly string Port;
        public readonly string? AuthToken = null;
        public readonly string Endpoint;
        public readonly Command? Command;
        public readonly HttpMethod Method;
        public readonly HttpStatusCode[] ExpectedResponseStatusCode;

        public Request(string ip, string port, string? authToken, string endpoint, Command? command, HttpMethod method, params HttpStatusCode[] expectedResponseStatusCode)
        {
            IP = ip;
            Port = port;
            AuthToken = authToken;
            Endpoint = endpoint;
            Command = command;
            Method = method;
            ExpectedResponseStatusCode = expectedResponseStatusCode;
        }
    }
}