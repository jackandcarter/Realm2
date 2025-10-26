using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Terrain
{
    public class RealmChunkState
    {
        private readonly Dictionary<string, RealmChunkStructure> _structures = new Dictionary<string, RealmChunkStructure>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RealmChunkPlot> _plots = new Dictionary<string, RealmChunkPlot>(StringComparer.OrdinalIgnoreCase);

        public RealmChunkState(RealmChunkSnapshot snapshot)
        {
            ApplySnapshot(snapshot);
        }

        public string ChunkId { get; private set; }
        public string RealmId { get; private set; }
        public int ChunkX { get; private set; }
        public int ChunkZ { get; private set; }
        public string Payload { get; private set; }
        public bool IsDeleted { get; private set; }
        public string CreatedAt { get; private set; }
        public string UpdatedAt { get; private set; }

        public void ApplySnapshot(RealmChunkSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ChunkId = snapshot.chunkId;
            RealmId = snapshot.realmId;
            ChunkX = snapshot.chunkX;
            ChunkZ = snapshot.chunkZ;
            Payload = snapshot.payload;
            IsDeleted = snapshot.isDeleted;
            CreatedAt = snapshot.createdAt;
            UpdatedAt = snapshot.updatedAt;

            _structures.Clear();
            if (snapshot.structures != null)
            {
                foreach (var structure in snapshot.structures)
                {
                    if (structure == null || structure.isDeleted || string.IsNullOrEmpty(structure.structureId))
                    {
                        continue;
                    }

                    _structures[structure.structureId] = CloneStructure(structure);
                }
            }

            _plots.Clear();
            if (snapshot.plots != null)
            {
                foreach (var plot in snapshot.plots)
                {
                    if (plot == null || plot.isDeleted || string.IsNullOrEmpty(plot.plotId))
                    {
                        continue;
                    }

                    _plots[plot.plotId] = ClonePlot(plot);
                }
            }
        }

        public void ApplyChange(RealmChunkChange change)
        {
            if (change == null)
            {
                return;
            }

            if (change.chunk != null)
            {
                ApplySnapshot(change.chunk);
            }

            if (change.structures != null)
            {
                foreach (var structure in change.structures)
                {
                    if (structure == null || string.IsNullOrEmpty(structure.structureId))
                    {
                        continue;
                    }

                    if (structure.isDeleted)
                    {
                        _structures.Remove(structure.structureId);
                    }
                    else
                    {
                        _structures[structure.structureId] = CloneStructure(structure);
                    }
                }
            }

            if (change.plots != null)
            {
                foreach (var plot in change.plots)
                {
                    if (plot == null || string.IsNullOrEmpty(plot.plotId))
                    {
                        continue;
                    }

                    if (plot.isDeleted)
                    {
                        _plots.Remove(plot.plotId);
                    }
                    else
                    {
                        _plots[plot.plotId] = ClonePlot(plot);
                    }
                }
            }

            if (!string.IsNullOrEmpty(change.createdAt))
            {
                UpdatedAt = change.createdAt;
            }
        }

        public RealmChunkSnapshot ToSnapshot()
        {
            return new RealmChunkSnapshot
            {
                chunkId = ChunkId,
                realmId = RealmId,
                chunkX = ChunkX,
                chunkZ = ChunkZ,
                payload = Payload,
                isDeleted = IsDeleted,
                createdAt = CreatedAt,
                updatedAt = UpdatedAt,
                structures = _structures.Values.Select(CloneStructure).ToArray(),
                plots = _plots.Values.Select(ClonePlot).ToArray(),
            };
        }

        public RealmChunkState Clone()
        {
            return new RealmChunkState(ToSnapshot());
        }

        private static RealmChunkStructure CloneStructure(RealmChunkStructure original)
        {
            return new RealmChunkStructure
            {
                structureId = original.structureId,
                realmId = original.realmId,
                chunkId = original.chunkId,
                structureType = original.structureType,
                data = original.data,
                isDeleted = original.isDeleted,
                createdAt = original.createdAt,
                updatedAt = original.updatedAt,
            };
        }

        private static RealmChunkPlot ClonePlot(RealmChunkPlot original)
        {
            return new RealmChunkPlot
            {
                plotId = original.plotId,
                realmId = original.realmId,
                chunkId = original.chunkId,
                plotIdentifier = original.plotIdentifier,
                ownerUserId = original.ownerUserId,
                data = original.data,
                isDeleted = original.isDeleted,
                createdAt = original.createdAt,
                updatedAt = original.updatedAt,
            };
        }
    }

    public class RealmChunkCache
    {
        private const int MaxRecentAuthoritativeChanges = 128;

        private readonly Dictionary<string, RealmChunkState> _chunks = new Dictionary<string, RealmChunkState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PredictionRecord> _pendingPredictions = new Dictionary<string, PredictionRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _predictionOrder = new List<string>();
        private readonly HashSet<string> _recentAuthoritativeChangeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> _recentAuthoritativeChangeQueue = new Queue<string>();

        public event Action<RealmChunkState> ChunkUpdated;
        public event Action<string> ChunkRemoved;

        public string LastServerTimestamp { get; private set; }

        public void ApplySnapshot(RealmChunkSnapshotResponse response)
        {
            if (response == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(response.serverTimestamp))
            {
                LastServerTimestamp = response.serverTimestamp;
            }

            if (response.chunks == null)
            {
                return;
            }

            foreach (var snapshot in response.chunks)
            {
                if (snapshot == null || string.IsNullOrEmpty(snapshot.chunkId))
                {
                    continue;
                }

                if (snapshot.isDeleted)
                {
                    if (_chunks.Remove(snapshot.chunkId))
                    {
                        ChunkRemoved?.Invoke(snapshot.chunkId);
                    }

                    continue;
                }

                if (_chunks.TryGetValue(snapshot.chunkId, out var state))
                {
                    state.ApplySnapshot(snapshot);
                }
                else
                {
                    state = new RealmChunkState(snapshot);
                    _chunks[snapshot.chunkId] = state;
                }

                ChunkUpdated?.Invoke(state.Clone());
            }
        }

        public void ApplyChanges(RealmChunkChangeFeedResponse response)
        {
            if (response == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(response.serverTimestamp))
            {
                LastServerTimestamp = response.serverTimestamp;
            }

            if (response.changes == null)
            {
                return;
            }

            foreach (var change in response.changes)
            {
                ApplyChange(change);
            }
        }

        public void ApplyChange(RealmChunkChange change, bool isAuthoritative = true)
        {
            if (change == null || string.IsNullOrEmpty(change.chunkId))
            {
                return;
            }

            if (isAuthoritative && !string.IsNullOrEmpty(change.changeId))
            {
                if (_recentAuthoritativeChangeIds.Contains(change.changeId))
                {
                    return;
                }

                TrackAuthoritativeChange(change.changeId);
            }

            if (isAuthoritative && !string.IsNullOrEmpty(change.createdAt))
            {
                LastServerTimestamp = change.createdAt;
            }

            if (!_chunks.TryGetValue(change.chunkId, out var state))
            {
                if (change.chunk != null && !change.chunk.isDeleted)
                {
                    state = new RealmChunkState(change.chunk);
                    _chunks[change.chunk.chunkId] = state;
                    ChunkUpdated?.Invoke(state.Clone());
                }

                return;
            }

            state.ApplyChange(change);

            if (change.chunk != null && change.chunk.isDeleted)
            {
                _chunks.Remove(change.chunk.chunkId);
                ChunkRemoved?.Invoke(change.chunk.chunkId);
                return;
            }

            if (state.IsDeleted)
            {
                _chunks.Remove(change.chunkId);
                ChunkRemoved?.Invoke(change.chunkId);
                return;
            }

            ChunkUpdated?.Invoke(state.Clone());
        }

        public void ApplyPredictedChange(string requestId, RealmChunkChange predictedChange)
        {
            if (string.IsNullOrWhiteSpace(requestId) || predictedChange == null || string.IsNullOrEmpty(predictedChange.chunkId))
            {
                return;
            }

            if (_pendingPredictions.ContainsKey(requestId))
            {
                return;
            }

            RealmChunkSnapshot previousSnapshot = null;
            if (_chunks.TryGetValue(predictedChange.chunkId, out var existing))
            {
                previousSnapshot = existing.ToSnapshot();
            }

            var record = new PredictionRecord
            {
                RequestId = requestId,
                ChunkId = predictedChange.chunkId,
                PreviousSnapshot = previousSnapshot,
                PredictedChange = CloneChange(predictedChange)
            };

            _pendingPredictions[requestId] = record;
            _predictionOrder.Add(requestId);

            ApplyChange(predictedChange, isAuthoritative: false);
        }

        public void ConfirmPrediction(string requestId, RealmChunkChange authoritativeChange)
        {
            if (!string.IsNullOrWhiteSpace(requestId) && _pendingPredictions.Remove(requestId))
            {
                _predictionOrder.Remove(requestId);
            }

            if (authoritativeChange != null)
            {
                ApplyChange(authoritativeChange, isAuthoritative: true);
            }
        }

        public void RejectPrediction(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            if (!_pendingPredictions.TryGetValue(requestId, out var rejected))
            {
                return;
            }

            var index = _predictionOrder.IndexOf(requestId);
            var reapply = new List<PredictionRecord>();
            if (index >= 0)
            {
                for (var i = index + 1; i < _predictionOrder.Count; i++)
                {
                    var id = _predictionOrder[i];
                    if (_pendingPredictions.TryGetValue(id, out var pending))
                    {
                        reapply.Add(pending);
                        _pendingPredictions.Remove(id);
                    }
                }

                _predictionOrder.RemoveRange(index, _predictionOrder.Count - index);
            }

            _pendingPredictions.Remove(requestId);
            RestoreState(rejected);

            foreach (var pending in reapply)
            {
                ApplyPredictedChange(pending.RequestId, CloneChange(pending.PredictedChange));
            }
        }

        public IReadOnlyCollection<RealmChunkState> GetAllChunks()
        {
            var list = new List<RealmChunkState>(_chunks.Count);
            foreach (var state in _chunks.Values)
            {
                list.Add(state.Clone());
            }

            return list;
        }

        public bool TryGet(string chunkId, out RealmChunkState state)
        {
            if (_chunks.TryGetValue(chunkId, out var existing))
            {
                state = existing.Clone();
                return true;
            }

            state = null;
            return false;
        }

        public void Clear()
        {
            _chunks.Clear();
            _pendingPredictions.Clear();
            _predictionOrder.Clear();
            _recentAuthoritativeChangeIds.Clear();
            _recentAuthoritativeChangeQueue.Clear();
            LastServerTimestamp = null;
        }

        private void RestoreState(PredictionRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (record.PreviousSnapshot == null)
            {
                if (_chunks.Remove(record.ChunkId))
                {
                    ChunkRemoved?.Invoke(record.ChunkId);
                }

                return;
            }

            var restored = new RealmChunkState(record.PreviousSnapshot);
            _chunks[record.ChunkId] = restored;
            ChunkUpdated?.Invoke(restored.Clone());
        }

        private void TrackAuthoritativeChange(string changeId)
        {
            if (string.IsNullOrEmpty(changeId))
            {
                return;
            }

            _recentAuthoritativeChangeIds.Add(changeId);
            _recentAuthoritativeChangeQueue.Enqueue(changeId);

            while (_recentAuthoritativeChangeQueue.Count > MaxRecentAuthoritativeChanges)
            {
                var expired = _recentAuthoritativeChangeQueue.Dequeue();
                _recentAuthoritativeChangeIds.Remove(expired);
            }
        }

        private static RealmChunkChange CloneChange(RealmChunkChange change)
        {
            if (change == null)
            {
                return null;
            }

            return new RealmChunkChange
            {
                changeId = change.changeId,
                realmId = change.realmId,
                chunkId = change.chunkId,
                changeType = change.changeType,
                createdAt = change.createdAt,
                chunk = CloneSnapshot(change.chunk),
                structures = change.structures != null ? Array.ConvertAll(change.structures, CloneStructure) : null,
                plots = change.plots != null ? Array.ConvertAll(change.plots, ClonePlot) : null,
            };
        }

        private static RealmChunkSnapshot CloneSnapshot(RealmChunkSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            return new RealmChunkSnapshot
            {
                chunkId = snapshot.chunkId,
                realmId = snapshot.realmId,
                chunkX = snapshot.chunkX,
                chunkZ = snapshot.chunkZ,
                payload = snapshot.payload,
                isDeleted = snapshot.isDeleted,
                createdAt = snapshot.createdAt,
                updatedAt = snapshot.updatedAt,
                structures = snapshot.structures != null ? Array.ConvertAll(snapshot.structures, CloneStructure) : null,
                plots = snapshot.plots != null ? Array.ConvertAll(snapshot.plots, ClonePlot) : null,
            };
        }

        private static RealmChunkStructure CloneStructure(RealmChunkStructure original)
        {
            if (original == null)
            {
                return null;
            }

            return new RealmChunkStructure
            {
                structureId = original.structureId,
                realmId = original.realmId,
                chunkId = original.chunkId,
                structureType = original.structureType,
                data = original.data,
                isDeleted = original.isDeleted,
                createdAt = original.createdAt,
                updatedAt = original.updatedAt,
            };
        }

        private static RealmChunkPlot ClonePlot(RealmChunkPlot original)
        {
            if (original == null)
            {
                return null;
            }

            return new RealmChunkPlot
            {
                plotId = original.plotId,
                realmId = original.realmId,
                chunkId = original.chunkId,
                plotIdentifier = original.plotIdentifier,
                ownerUserId = original.ownerUserId,
                data = original.data,
                isDeleted = original.isDeleted,
                createdAt = original.createdAt,
                updatedAt = original.updatedAt,
            };
        }

        private sealed class PredictionRecord
        {
            public string RequestId;
            public string ChunkId;
            public RealmChunkSnapshot PreviousSnapshot;
            public RealmChunkChange PredictedChange;
        }
    }
}
