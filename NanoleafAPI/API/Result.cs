using System.Net;

namespace NanoleafAPI
{
    public readonly struct Result<T>
    {
        public readonly Request Request;
        public readonly HttpStatusCode? StatusCode = null;
        public readonly bool Success = false;
        public readonly T? ResponseValue = default;
        public readonly Exception? Exception = null;

        public Result(in Request request, in HttpStatusCode? statusCode, in T? responsevalue)
        {
            Request = request;
            StatusCode = statusCode;
            Success = true;
            ResponseValue = responsevalue;
        }
        public Result(in Request request, in HttpStatusCode? statusCode, in Exception? exception)
        {
            Request = request;
            StatusCode = statusCode;
            Exception = exception;
        }
        public Result(in Request request, in Exception? exception)
        {
            Request = request;
            Exception = exception;
        }
        public Result(in Request request, in HttpStatusCode? statusCode)
        {
            Request = request;
            StatusCode = statusCode;
        }
        public Result(in Request request)
        {
            Request = request;
        }
    }
}