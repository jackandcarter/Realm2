using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Client
{
    [Serializable]
    public class ApiError
    {
        [Serializable]
        private class ErrorResponse
        {
            public string message;
            public string error;
        }

        public long StatusCode { get; }
        public string Message { get; }

        public ApiError(long statusCode, string message)
        {
            StatusCode = statusCode;
            Message = string.IsNullOrWhiteSpace(message) ? "An unknown error occurred." : message;
        }

        public static ApiError FromRequest(UnityWebRequest request)
        {
            if (request == null)
            {
                return new ApiError(-1, "Request was null.");
            }

            var statusCode = request.responseCode;
            var message = request.error;

            var body = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    var parsed = JsonUtility.FromJson<ErrorResponse>(body);
                    if (parsed != null)
                    {
                        if (!string.IsNullOrWhiteSpace(parsed.message))
                        {
                            message = parsed.message;
                        }
                        else if (!string.IsNullOrWhiteSpace(parsed.error))
                        {
                            message = parsed.error;
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Swallow JSON parsing issues and fall back to default message.
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "The server returned an empty response.";
            }

            return new ApiError(statusCode, message);
        }

        public override string ToString()
        {
            return $"[{StatusCode}] {Message}";
        }
    }
}
