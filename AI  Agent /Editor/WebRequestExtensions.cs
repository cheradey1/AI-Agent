using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityAIAgent
{
    /// <summary>
    /// Extension methods to make UnityWebRequestAsyncOperation awaitable
    /// </summary>
    public static class WebRequestExtensions
    {
        public struct UnityWebRequestAwaiter : INotifyCompletion
        {
            private UnityWebRequestAsyncOperation _asyncOp;
            private Action _continuation;
            private CancellationToken _cancellationToken;
            private CancellationTokenRegistration _registration;

            public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp, CancellationToken cancellationToken = default)
            {
                _asyncOp = asyncOp;
                _continuation = null;
                _cancellationToken = cancellationToken;
                _registration = default;
            }

            public bool IsCompleted => _asyncOp.isDone;

            public void OnCompleted(Action continuation)
            {
                _continuation = continuation;
                var localContinuation = _continuation; // Локальна копія для лямбда-виразу
                _asyncOp.completed += _ => localContinuation?.Invoke();

                // Register cancellation if token is provided
                if (_cancellationToken != CancellationToken.None)
                {
                    // Create local copies to use in the lambda
                    var asyncOp = _asyncOp;
                    var cont = _continuation;
                    
                    _registration = _cancellationToken.Register(() => 
                    {
                        // Abort the request if cancellation is requested
                        if (asyncOp != null && !asyncOp.isDone)
                        {
                            Debug.Log("WebRequest cancelled via token");
                            asyncOp.webRequest.Abort();
                            cont?.Invoke();
                        }
                    });
                }
            }

            public UnityWebRequest GetResult()
            {
                // Clean up registration when getting result
                _registration.Dispose();
                
                if (_cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Web request was cancelled");
                }

                return _asyncOp.webRequest;
            }
        }

        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            return new UnityWebRequestAwaiter(asyncOp);
        }

        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp, CancellationToken cancellationToken)
        {
            return new UnityWebRequestAwaiter(asyncOp, cancellationToken);
        }

        /// <summary>
        /// Sets a timeout for the web request
        /// </summary>
        public static UnityWebRequest WithTimeout(this UnityWebRequest request, int timeoutSeconds)
        {
            request.timeout = timeoutSeconds;
            return request;
        }
        
        /// <summary>
        /// Checks if the web request completed with an error
        /// </summary>
        public static bool HasError(this UnityWebRequest request)
        {
            #if UNITY_2020_1_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
            #else
            return request.isNetworkError || request.isHttpError;
            #endif
        }
        
        /// <summary>
        /// Gets a user-friendly error message from a web request
        /// </summary>
        public static string GetErrorMessage(this UnityWebRequest request)
        {
            if (!request.HasError())
                return string.Empty;
                
            string errorMsg = $"Error ({request.responseCode}): {request.error}";
            
            if (!string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                errorMsg += $"\nResponse: {request.downloadHandler.text}";
            }
            
            return errorMsg;
        }
    }

    public class WebRequestWithProgress
    {
        public UnityWebRequest request;
        public Action<string> OnRequestDoneString;
        public Action<byte[]> OnRequestDone;
        public Action<string> OnRequestFailed;

        public WebRequestWithProgress(UnityWebRequest request)
        {
            this.request = request;
        }

        public WebRequestWithProgress BeginDownload()
        {
            var self = this; // Копія this для лямбда-виразів
            DownloadHandlerBuffer downloadHandler = new DownloadHandlerBuffer();
            request.downloadHandler = downloadHandler;

            request.SendWebRequest().completed += operation =>
            {
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    self.OnRequestFailed?.Invoke(request.error);
                    return;
                }

                self.OnRequestDone?.Invoke(downloadHandler.data);
                self.OnRequestDoneString?.Invoke(downloadHandler.text);
            };

            return this;
        }
    }
}
