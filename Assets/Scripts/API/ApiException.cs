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
    }

    [Serializable]
    public class ApiErrorResponse
    {
        public string status;
        public string message;
    }
}