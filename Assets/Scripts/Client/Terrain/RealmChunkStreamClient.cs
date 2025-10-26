using System;
using System.Collections;
using System.Text;
using Client;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Terrain
{
    public class RealmChunkStreamClient : IDisposable
    {
        private readonly string _baseUrl;
        private UnityWebRequest _activeRequest;
        private SseDownloadHandler _downloadHandler;

        public RealmChunkStreamClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public IEnumerator Connect(
            string realmId,
            Action<RealmChunkChange> onChange,
            Action<ApiError> onError,
            Action onCompleted = null)
        {
            Disconnect();

            var url = $"{_baseUrl}/realms/{realmId}/chunks/stream";
            _downloadHandler = new SseDownloadHandler(message => HandleMessage(message, onChange));
            _activeRequest = UnityWebRequest.Get(url);
            _activeRequest.downloadHandler = _downloadHandler;
            _activeRequest.SetRequestHeader("Accept", "text/event-stream");
            AttachAuthHeader(_activeRequest);

            var operation = _activeRequest.SendWebRequest();
            while (!operation.isDone)
            {
                yield return null;
            }

            if (_activeRequest.result == UnityWebRequest.Result.Success || _activeRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                onCompleted?.Invoke();
            }
            else
            {
                onError?.Invoke(ApiError.FromRequest(_activeRequest));
            }

            DisposeRequest();
        }

        public void Disconnect()
        {
            if (_activeRequest != null)
            {
                _activeRequest.Abort();
                DisposeRequest();
            }
        }

        private void HandleMessage(string message, Action<RealmChunkChange> onChange)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                var change = JsonUtility.FromJson<RealmChunkChange>(message);
                if (!string.IsNullOrEmpty(change?.chunkId))
                {
                    onChange?.Invoke(change);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse chunk change event: {ex}");
            }
        }

        private void DisposeRequest()
        {
            _downloadHandler?.Dispose();
            _downloadHandler = null;
            _activeRequest?.Dispose();
            _activeRequest = null;
        }

        private static void AttachAuthHeader(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(SessionManager.AuthToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {SessionManager.AuthToken}");
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        private sealed class SseDownloadHandler : DownloadHandlerScript
        {
            private readonly Action<string> _onMessage;
            private readonly StringBuilder _buffer = new();

            public SseDownloadHandler(Action<string> onMessage)
                : base(new byte[1024])
            {
                _onMessage = onMessage;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength <= 0)
                {
                    return true;
                }

                var chunk = Encoding.UTF8.GetString(data, 0, dataLength);
                _buffer.Append(chunk);

                var text = _buffer.ToString();
                int index;
                while ((index = text.IndexOf("\n\n", StringComparison.Ordinal)) >= 0)
                {
                    var eventBlock = text.Substring(0, index);
                    text = text.Substring(index + 2);
                    ProcessBlock(eventBlock);
                }

                _buffer.Length = 0;
                _buffer.Append(text);
                return true;
            }

            private void ProcessBlock(string block)
            {
                if (string.IsNullOrWhiteSpace(block))
                {
                    return;
                }

                var lines = block.Split('\n');
                var builder = new StringBuilder();
                foreach (var line in lines)
                {
                    if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append(line.Substring(5).TrimStart());
                    }
                }

                if (builder.Length > 0)
                {
                    _onMessage?.Invoke(builder.ToString());
                }
            }
        }
    }
}
