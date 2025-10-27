using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client.Map
{
    /// <summary>
    /// Centralised runtime access point for the collection of map pins. Provides filtered views and selection/highlight
    /// tracking so UI controllers can stay in sync across the world and mini maps.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapPinService : MonoBehaviour
    {
        [SerializeField] private List<MapPinData> pins = new();

        private readonly List<MapPinData> _worldPins = new();
        private readonly List<MapPinData> _unlockedWorldPins = new();
        private readonly Dictionary<string, List<MapPinData>> _pinsByZone =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<MapPinData>> _unlockedPinsByZone =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MapPinData> _pinsById =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _pinUnlockState =
            new(StringComparer.OrdinalIgnoreCase);

        private MapPinData _selectedPin;
        private MapPinData _highlightedPin;

        /// <summary>
        /// Raised whenever the filtered set of world map pins changes (due to unlocks or data updates).
        /// </summary>
        public event Action<IReadOnlyList<MapPinData>> WorldPinsChanged;

        /// <summary>
        /// Raised whenever the filtered set of pins for the provided zone identifier changes.
        /// </summary>
        public event Action<string, IReadOnlyList<MapPinData>> ZonePinsChanged;

        /// <summary>
        /// Raised whenever the selected pin reference changes.
        /// </summary>
        public event Action<MapPinData> SelectedPinChanged;

        /// <summary>
        /// Raised whenever the highlighted pin reference changes.
        /// </summary>
        public event Action<MapPinData> HighlightedPinChanged;

        /// <summary>
        /// Pin currently flagged as selected by the user.
        /// </summary>
        public MapPinData SelectedPin => _selectedPin;

        /// <summary>
        /// Pin currently highlighted (typically due to pointer hover).
        /// </summary>
        public MapPinData HighlightedPin => _highlightedPin;

        private void Awake()
        {
            RebuildCaches();
        }

        private void OnValidate()
        {
            RebuildCaches();
        }

        /// <summary>
        /// Returns the set of pins that should appear on the world map.
        /// </summary>
        public IReadOnlyList<MapPinData> GetWorldPins(bool includeLocked = false)
        {
            return includeLocked ? _worldPins : _unlockedWorldPins;
        }

        /// <summary>
        /// Returns the set of pins that should appear on the mini map for the provided zone identifier.
        /// </summary>
        public IReadOnlyList<MapPinData> GetMiniMapPins(string zoneId, bool includeLocked = false)
        {
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                return Array.Empty<MapPinData>();
            }

            var lookup = includeLocked ? _pinsByZone : _unlockedPinsByZone;
            if (lookup.TryGetValue(zoneId, out var pinsForZone))
            {
                return pinsForZone;
            }

            return Array.Empty<MapPinData>();
        }

        /// <summary>
        /// Attempts to retrieve a pin from its identifier.
        /// </summary>
        public bool TryGetPin(string id, out MapPinData pin)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                pin = null;
                return false;
            }

            return _pinsById.TryGetValue(id, out pin);
        }

        /// <summary>
        /// Updates the unlocked state for the provided pin identifier.
        /// </summary>
        public void SetPinUnlocked(string id, bool unlocked)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (_pinUnlockState.TryGetValue(id, out var current) && current == unlocked)
            {
                return;
            }

            _pinUnlockState[id] = unlocked;
            RefreshUnlockedCaches();
        }

        /// <summary>
        /// Returns whether the pin with the provided identifier is currently unlocked.
        /// </summary>
        public bool IsPinUnlocked(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            return _pinUnlockState.TryGetValue(id, out var unlocked) && unlocked;
        }

        /// <summary>
        /// Flags the provided pin as the selected entry.
        /// </summary>
        public void SetSelectedPin(MapPinData pin)
        {
            if (_selectedPin == pin)
            {
                return;
            }

            _selectedPin = pin;
            SelectedPinChanged?.Invoke(_selectedPin);
        }

        /// <summary>
        /// Clears any selection currently active on the service.
        /// </summary>
        public void ClearSelectedPin()
        {
            if (_selectedPin == null)
            {
                return;
            }

            _selectedPin = null;
            SelectedPinChanged?.Invoke(null);
        }

        /// <summary>
        /// Flags the provided pin as highlighted.
        /// </summary>
        public void SetHighlightedPin(MapPinData pin)
        {
            if (_highlightedPin == pin)
            {
                return;
            }

            _highlightedPin = pin;
            HighlightedPinChanged?.Invoke(_highlightedPin);
        }

        /// <summary>
        /// Clears the highlighted pin reference.
        /// </summary>
        public void ClearHighlightedPin()
        {
            if (_highlightedPin == null)
            {
                return;
            }

            _highlightedPin = null;
            HighlightedPinChanged?.Invoke(null);
        }

        /// <summary>
        /// Adds a pin to the managed collection at runtime.
        /// </summary>
        public void AddPin(MapPinData pin)
        {
            if (pin == null)
            {
                return;
            }

            if (!pins.Contains(pin))
            {
                pins.Add(pin);
            }

            RebuildCaches();
        }

        /// <summary>
        /// Removes a pin from the managed collection at runtime.
        /// </summary>
        public void RemovePin(MapPinData pin)
        {
            if (pin == null)
            {
                return;
            }

            if (pins.Remove(pin))
            {
                RebuildCaches();
            }
        }

        private void RebuildCaches()
        {
            _worldPins.Clear();
            _pinsByZone.Clear();
            _pinsById.Clear();

            if (pins == null)
            {
                return;
            }

            foreach (var pin in pins)
            {
                if (pin == null || string.IsNullOrWhiteSpace(pin.Id))
                {
                    continue;
                }

                _worldPins.Add(pin);
                _pinsById[pin.Id] = pin;

                if (!_pinUnlockState.ContainsKey(pin.Id))
                {
                    _pinUnlockState[pin.Id] = pin.UnlockedByDefault;
                }

                foreach (var zoneId in pin.ZoneIds)
                {
                    if (string.IsNullOrWhiteSpace(zoneId))
                    {
                        continue;
                    }

                    if (!_pinsByZone.TryGetValue(zoneId, out var zoneList))
                    {
                        zoneList = new List<MapPinData>();
                        _pinsByZone.Add(zoneId, zoneList);
                    }

                    if (!zoneList.Contains(pin))
                    {
                        zoneList.Add(pin);
                    }
                }
            }

            RefreshUnlockedCaches();
        }

        private void RefreshUnlockedCaches()
        {
            _unlockedWorldPins.Clear();
            foreach (var pin in _worldPins)
            {
                if (IsPinUnlocked(pin.Id))
                {
                    _unlockedWorldPins.Add(pin);
                }
            }

            _unlockedPinsByZone.Clear();
            foreach (var kvp in _pinsByZone)
            {
                var unlockedList = new List<MapPinData>();
                foreach (var pin in kvp.Value)
                {
                    if (IsPinUnlocked(pin.Id))
                    {
                        unlockedList.Add(pin);
                    }
                }

                _unlockedPinsByZone[kvp.Key] = unlockedList;
            }

            WorldPinsChanged?.Invoke(_unlockedWorldPins);

            foreach (var kvp in _unlockedPinsByZone)
            {
                ZonePinsChanged?.Invoke(kvp.Key, kvp.Value);
            }

            if (_selectedPin != null && !IsPinUnlocked(_selectedPin.Id))
            {
                ClearSelectedPin();
            }

            if (_highlightedPin != null && !IsPinUnlocked(_highlightedPin.Id))
            {
                ClearHighlightedPin();
            }
        }
    }
}
