using System;
using System.Collections;
using Client;

namespace Client.Terrain
{
    public class RealmChunkRealtimeController : IDisposable
    {
        private readonly RealmChunkCache _cache;
        private readonly RealmChunkStreamClient _streamClient;
        private string _realmId;

        public RealmChunkRealtimeController(RealmChunkCache cache, RealmChunkStreamClient streamClient)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _streamClient = streamClient ?? throw new ArgumentNullException(nameof(streamClient));

            _streamClient.MutationAcknowledged += HandleMutationAcknowledged;
            _streamClient.MutationRejected += HandleMutationRejected;
        }

        public IEnumerator Connect(string realmId, Action<ApiError> onError = null, Action onCompleted = null)
        {
            _realmId = realmId;
            return _streamClient.Connect(
                realmId,
                change => _cache.ApplyChange(change),
                error => onError?.Invoke(error),
                onCompleted);
        }

        public void PredictAndSubmitChange(
            string chunkId,
            RealmChunkChangeRequest request,
            RealmChunkChange predictedChange,
            string requestId)
        {
            if (string.IsNullOrEmpty(_realmId) || string.IsNullOrWhiteSpace(chunkId))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(requestId) && predictedChange != null)
            {
                _cache.ApplyPredictedChange(requestId, predictedChange);
            }

            _streamClient.SendMutation(_realmId, chunkId, request, requestId);
        }

        private void HandleMutationAcknowledged(string requestId, RealmChunkChange change)
        {
            _cache.ConfirmPrediction(requestId, change);
        }

        private void HandleMutationRejected(string requestId, string _)
        {
            _cache.RejectPrediction(requestId);
        }

        public void Dispose()
        {
            _streamClient.MutationAcknowledged -= HandleMutationAcknowledged;
            _streamClient.MutationRejected -= HandleMutationRejected;
        }
    }
}
