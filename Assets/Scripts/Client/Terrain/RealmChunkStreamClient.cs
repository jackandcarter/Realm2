using System;
using System.Collections;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client;
using UnityEngine;

namespace Client.Terrain
{
    public class RealmChunkStreamClient : IDisposable
    {
        private readonly string _baseUrl;
        private readonly SynchronizationContext _syncContext;
        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellation;
        private Task _receiveTask;
        private string _activeRealmId;
        private readonly object _sendLock = new object();

        public event Action<string> Subscribed;
        public event Action<string> Unsubscribed;
        public event Action<string, RealmChunkChange> MutationAcknowledged;
        public event Action<string, string> MutationRejected;
        public event Action<string> ConnectionClosed;

        public RealmChunkStreamClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public IEnumerator Connect(
            string realmId,
            Action<RealmChunkChange> onChange,
            Action<ApiError> onError,
            Action onCompleted = null)
        {
            if (string.IsNullOrWhiteSpace(realmId))
            {
                onError?.Invoke(new ApiError(-1, "Realm id is required for realtime terrain updates."));
                yield break;
            }

            if (string.IsNullOrEmpty(SessionManager.AuthToken))
            {
                onError?.Invoke(new ApiError(-1, "You must be signed in before subscribing to terrain updates."));
                yield break;
            }

            Disconnect();

            _socket = new ClientWebSocket();
            _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            _cancellation = new CancellationTokenSource();
            _activeRealmId = realmId;

            var uri = BuildSocketUri();
            Exception connectException = null;
            var connectTask = _socket.ConnectAsync(uri, CancellationToken.None);
            yield return WaitForTask(connectTask, ex => connectException = ex);
            if (connectException != null || _socket.State != WebSocketState.Open)
            {
                var error = connectException ?? new Exception("Failed to establish realtime connection.");
                onError?.Invoke(CreateApiError(error));
                Disconnect();
                yield break;
            }

            Exception subscribeException = null;
            var subscribeTask = SendAsync(new SubscribeRequest { type = "subscribe", realmId = realmId });
            yield return WaitForTask(subscribeTask, ex => subscribeException = ex);
            if (subscribeException != null)
            {
                onError?.Invoke(CreateApiError(subscribeException));
                Disconnect();
                yield break;
            }

            _receiveTask = ReceiveLoopAsync(realmId, onChange, onError, onCompleted);
        }

        public void SendMutation(string realmId, string chunkId, RealmChunkChangeRequest requestBody, string requestId)
        {
            if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(chunkId))
            {
                return;
            }

            if (_socket == null || _socket.State != WebSocketState.Open)
            {
                return;
            }

            var payload = new MutationRequest
            {
                type = "mutation",
                realmId = realmId,
                chunkId = chunkId,
                requestId = string.IsNullOrWhiteSpace(requestId) ? null : requestId,
                changeType = requestBody?.changeType,
                chunk = requestBody?.chunk,
                structures = requestBody?.structures,
                plots = requestBody?.plots
            };

            _ = SendAsync(payload);
        }

        public void Disconnect()
        {
            _activeRealmId = null;

            if (_cancellation != null && !_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
            }

            if (_receiveTask != null)
            {
                try
                {
                    _receiveTask.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException)
                {
                    // Ignore exceptions raised while shutting down the receive loop.
                }
                finally
                {
                    _receiveTask = null;
                }
            }

            if (_socket != null)
            {
                try
                {
                    if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived)
                    {
                        _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "disconnect", CancellationToken.None).Wait(100);
                    }
                }
                catch (Exception)
                {
                    // swallow close errors to avoid crashing during teardown
                }
                finally
                {
                    _socket.Dispose();
                    _socket = null;
                }
            }

            _cancellation?.Dispose();
            _cancellation = null;
        }

        public void Dispose()
        {
            Disconnect();
        }

        private Uri BuildSocketUri()
        {
            var builder = new UriBuilder(_baseUrl);
            builder.Scheme = string.Equals(builder.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
            var path = builder.Path?.TrimEnd('/') ?? string.Empty;
            builder.Path = string.IsNullOrEmpty(path) ? "/ws/chunks" : $"{path}/ws/chunks";
            builder.Query = string.IsNullOrEmpty(SessionManager.AuthToken)
                ? string.Empty
                : $"token={Uri.EscapeDataString(SessionManager.AuthToken)}";
            return builder.Uri;
        }

        private static IEnumerator WaitForTask(Task task, Action<Exception> onError)
        {
            if (task == null)
            {
                yield break;
            }

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted && onError != null)
            {
                onError.Invoke(task.Exception?.GetBaseException() ?? task.Exception);
            }
        }

        private Task SendAsync<T>(T payload) where T : class
        {
            if (_socket == null)
            {
                return Task.CompletedTask;
            }

            string json;
            try
            {
                json = JsonUtility.ToJson(payload);
            }
            catch (ArgumentException ex)
            {
                return Task.FromException(ex);
            }

            var bytes = Encoding.UTF8.GetBytes(json ?? string.Empty);
            var segment = new ArraySegment<byte>(bytes);

            lock (_sendLock)
            {
                if (_socket == null || _socket.State != WebSocketState.Open)
                {
                    return Task.CompletedTask;
                }

                try
                {
                    return _socket.SendAsync(
                        segment,
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        _cancellation?.Token ?? CancellationToken.None);
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }
        }

        private async Task ReceiveLoopAsync(
            string realmId,
            Action<RealmChunkChange> onChange,
            Action<ApiError> onError,
            Action onCompleted)
        {
            var buffer = new byte[8192];
            var token = _cancellation?.Token ?? CancellationToken.None;

            try
            {
                while (_socket != null && _socket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    using var stream = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await CloseSocketAsync().ConfigureAwait(false);
                            break;
                        }

                        if (result.Count > 0)
                        {
                            stream.Write(buffer, 0, result.Count);
                        }
                    }
                    while (!result.EndOfMessage && !token.IsCancellationRequested);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var message = Encoding.UTF8.GetString(stream.ToArray());
                    HandleServerMessage(realmId, message, onChange, onError);
                }
            }
            catch (OperationCanceledException)
            {
                // expected when shutting down the socket
            }
            catch (Exception ex)
            {
                Dispatch(() => onError?.Invoke(CreateApiError(ex)));
            }
            finally
            {
                Dispatch(() => ConnectionClosed?.Invoke(realmId));
                Dispatch(onCompleted);
            }
        }

        private Task CloseSocketAsync()
        {
            if (_socket == null)
            {
                return Task.CompletedTask;
            }

            try
            {
                if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived)
                {
                    return _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server closed", CancellationToken.None);
                }
            }
            catch (Exception)
            {
                // ignore close failures and allow cleanup to continue
            }

            return Task.CompletedTask;
        }

        private void HandleServerMessage(
            string fallbackRealmId,
            string payload,
            Action<RealmChunkChange> onChange,
            Action<ApiError> onError)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            ServerEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<ServerEnvelope>(payload);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning($"Received malformed realtime payload: {payload}");
                return;
            }

            if (envelope == null || string.IsNullOrEmpty(envelope.type))
            {
                return;
            }

            var realmId = string.IsNullOrEmpty(envelope.realmId) ? fallbackRealmId : envelope.realmId;

            switch (envelope.type)
            {
                case "change":
                    if (envelope.change != null)
                    {
                        Dispatch(() => onChange?.Invoke(envelope.change));
                    }

                    break;
                case "mutationAck":
                    Dispatch(() => MutationAcknowledged?.Invoke(envelope.requestId ?? string.Empty, envelope.change));
                    break;
                case "mutationRejected":
                    Dispatch(() => MutationRejected?.Invoke(envelope.requestId ?? string.Empty, envelope.error ?? "Change rejected by the server."));
                    break;
                case "subscribed":
                    Dispatch(() => Subscribed?.Invoke(realmId));
                    break;
                case "unsubscribed":
                    Dispatch(() => Unsubscribed?.Invoke(realmId));
                    break;
                case "error":
                    if (!string.IsNullOrEmpty(envelope.error))
                    {
                        Dispatch(() => onError?.Invoke(new ApiError(-1, envelope.error)));
                    }

                    break;
                case "ready":
                case "pong":
                default:
                    break;
            }
        }

        private static ApiError CreateApiError(Exception ex)
        {
            var message = ex?.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "An unexpected error occurred while processing realtime terrain updates.";
            }

            return new ApiError(-1, message);
        }

        private void Dispatch(Action action)
        {
            if (action == null)
            {
                return;
            }

            _syncContext.Post(_ => action.Invoke(), null);
        }

        [Serializable]
        private class SubscribeRequest
        {
            public string type;
            public string realmId;
        }

        [Serializable]
        private class MutationRequest
        {
            public string type;
            public string realmId;
            public string chunkId;
            public string requestId;
            public string changeType;
            public RealmChunkMutation chunk;
            public RealmChunkStructureMutation[] structures;
            public RealmChunkPlotMutation[] plots;
        }

        [Serializable]
        private class ServerEnvelope
        {
            public string type;
            public string realmId;
            public string requestId;
            public string error;
            public RealmChunkChange change;
        }
    }
}
