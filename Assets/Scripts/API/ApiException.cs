using System;

namespace Api
{
    [Serializable]
    public class ApiException : Exception
    {
        public int responseCode;
        public string errorMessage;

        public ApiException(int code, string message) : base(message)
        {
            responseCode = code;
            errorMessage = message;
        }

        public ApiException(ApiErrorResponse error) : base(error.message)
        {
            responseCode = error.status;
            errorMessage = error.message;
        }
    }

    [Serializable]
    public class ApiErrorResponse
    {
        public int status;
        public string message;
    }
}