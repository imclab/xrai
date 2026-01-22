// ConferenceLayoutManager.cs - Auto-positioning for multi-user holograms (spec-003)
// Inspired by Apple Vision Pro Spatial Personas
// Handles semi-circle layout, gap filling, context-aware positioning
//
// Modes:
// - Theater: Side-by-side (for shared viewing)
// - Table: Semi-circle facing center (for collaboration)
// - Freeform: Users positioned where they joined
// - Grid: Stress testing grid layout

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetavidoVFX.H3M.Network
{
    /// <summary>
    /// Layout modes inspired by Apple Vision Pro Spatial Personas.
    /// </summary>
    public enum ConferenceLayoutMode
    {
        /// <summary>Side-by-side layout for shared viewing (movie, presentation).</summary>
        Theater,
        /// <summary>Semi-circle facing center for collaboration (game, meeting).</summary>
        Table,
        /// <summary>Users stay where they joined (AR passthrough).</summary>
        Freeform,
        /// <summary>Grid layout for stress testing many users.</summary>
        Grid
    }

    /// <summary>
    /// Represents a seat position in the conference layout.
    /// Similar to Apple's "Seat Pose" concept.
    /// </summary>
    [Serializable]
    public struct SeatPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public int SeatIndex;
        public bool IsOccupied;
        public string OccupantId;

        public static SeatPose Empty(int index, Vector3 pos, Quaternion rot) => new SeatPose
        {
            Position = pos,
            Rotation = rot,
            SeatIndex = index,
            IsOccupied = false,
            OccupantId = null
        };
    }

    /// <summary>
    /// Manages auto-positioning of remote holograms in conference mode.
    /// Implements Vision Pro Spatial Persona-style layouts.
    /// </summary>
    public class ConferenceLayoutManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Layout Settings")]
        [SerializeField] private ConferenceLayoutMode _layoutMode = ConferenceLayoutMode.Table;
        [SerializeField] private Transform _centerPoint;
        [SerializeField] private float _radius = 2f;
        [SerializeField] private float _hologramHeight = 0.5f;
        [SerializeField] private float _hologramScale = 0.15f;

        [Header("Table Mode (Semi-circle)")]
        [SerializeField] private float _arcAngle = 180f;
        [SerializeField] private float _startAngle = -90f;
        [SerializeField] private int _maxSeats = 20;

        [Header("Theater Mode (Side-by-side)")]
        [SerializeField] private float _theaterSpacing = 0.8f;
        [SerializeField] private float _theaterDistance = 3f;
        [SerializeField] private int _theaterRows = 2;

        [Header("Grid Mode (Stress Testing)")]
        [SerializeField] private int _gridColumns = 5;
        [SerializeField] private float _gridSpacing = 0.6f;

        [Header("Animation")]
        [SerializeField] private float _positionLerpSpeed = 3f;
        [SerializeField] private float _rotationLerpSpeed = 5f;
        [SerializeField] private bool _animateOnJoin = true;

        [Header("Debug")]
        [SerializeField] private bool _debugDraw = true;
        [SerializeField] private bool _showSeatGizmos = true;

        #endregion

        #region Private Fields

        private readonly List<SeatPose> _seats = new();
        private readonly Dictionary<string, RemoteHologramState> _holograms = new();
        private Camera _mainCamera;

        #endregion

        #region Events

        public event Action<string, SeatPose> OnHologramSeated;
        public event Action<string> OnHologramUnseated;
        public event Action OnLayoutChanged;

        #endregion

        #region Properties

        public ConferenceLayoutMode LayoutMode
        {
            get => _layoutMode;
            set
            {
                if (_layoutMode != value)
                {
                    _layoutMode = value;
                    RegenerateSeats();
                    ReassignAllSeats();
                    OnLayoutChanged?.Invoke();
                }
            }
        }

        public int ActiveHologramCount => _holograms.Count;
        public int AvailableSeatCount => _seats.FindAll(s => !s.IsOccupied).Count;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (_centerPoint == null)
            {
                // Default center point in front of camera
                var go = new GameObject("ConferenceCenter");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.forward * _radius;
                _centerPoint = go.transform;
            }
        }

        private void Start()
        {
            RegenerateSeats();
        }

        private void Update()
        {
            UpdateHologramPositions();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Register a remote hologram and assign it to a seat.
        /// </summary>
        public SeatPose RegisterHologram(string peerId, Transform hologramTransform)
        {
            if (_holograms.ContainsKey(peerId))
            {
                Debug.LogWarning($"[ConferenceLayout] Hologram {peerId} already registered");
                return GetSeatForPeer(peerId);
            }

            // Find next available seat
            int seatIndex = FindNextAvailableSeat();
            if (seatIndex < 0)
            {
                // No seats available, expand
                ExpandSeats(1);
                seatIndex = FindNextAvailableSeat();
            }

            // Assign seat
            var seat = _seats[seatIndex];
            seat.IsOccupied = true;
            seat.OccupantId = peerId;
            _seats[seatIndex] = seat;

            // Create hologram state
            var state = new RemoteHologramState
            {
                PeerId = peerId,
                Transform = hologramTransform,
                TargetSeat = seat,
                CurrentPosition = _animateOnJoin ? GetSpawnPosition() : seat.Position,
                CurrentRotation = seat.Rotation
            };
            _holograms[peerId] = state;

            // Initial positioning
            if (!_animateOnJoin)
            {
                hologramTransform.position = seat.Position;
                hologramTransform.rotation = seat.Rotation;
            }
            hologramTransform.localScale = Vector3.one * _hologramScale;

            OnHologramSeated?.Invoke(peerId, seat);
            Debug.Log($"[ConferenceLayout] Registered {peerId} at seat {seatIndex}");

            return seat;
        }

        /// <summary>
        /// Unregister a hologram when user leaves.
        /// </summary>
        public void UnregisterHologram(string peerId)
        {
            if (!_holograms.TryGetValue(peerId, out var state))
            {
                Debug.LogWarning($"[ConferenceLayout] Hologram {peerId} not found");
                return;
            }

            // Free the seat
            int seatIndex = state.TargetSeat.SeatIndex;
            if (seatIndex >= 0 && seatIndex < _seats.Count)
            {
                var seat = _seats[seatIndex];
                seat.IsOccupied = false;
                seat.OccupantId = null;
                _seats[seatIndex] = seat;
            }

            _holograms.Remove(peerId);
            OnHologramUnseated?.Invoke(peerId);

            Debug.Log($"[ConferenceLayout] Unregistered {peerId}");
        }

        /// <summary>
        /// Get the seat assigned to a peer.
        /// </summary>
        public SeatPose GetSeatForPeer(string peerId)
        {
            if (_holograms.TryGetValue(peerId, out var state))
            {
                return state.TargetSeat;
            }
            return default;
        }

        /// <summary>
        /// Get all registered hologram peer IDs.
        /// </summary>
        public string[] GetAllPeerIds()
        {
            var ids = new string[_holograms.Count];
            _holograms.Keys.CopyTo(ids, 0);
            return ids;
        }

        /// <summary>
        /// Update center point (e.g., when shared content moves).
        /// </summary>
        public void SetCenterPoint(Vector3 position)
        {
            _centerPoint.position = position;
            RegenerateSeats();
            ReassignAllSeats();
        }

        /// <summary>
        /// Force re-layout all holograms.
        /// </summary>
        public void RefreshLayout()
        {
            RegenerateSeats();
            ReassignAllSeats();
        }

        #endregion

        #region Seat Generation

        private void RegenerateSeats()
        {
            _seats.Clear();

            switch (_layoutMode)
            {
                case ConferenceLayoutMode.Table:
                    GenerateTableSeats();
                    break;
                case ConferenceLayoutMode.Theater:
                    GenerateTheaterSeats();
                    break;
                case ConferenceLayoutMode.Grid:
                    GenerateGridSeats();
                    break;
                case ConferenceLayoutMode.Freeform:
                    // Freeform doesn't pre-generate seats
                    break;
            }
        }

        private void GenerateTableSeats()
        {
            // Semi-circle around center point, facing inward
            float angleStep = _arcAngle / Mathf.Max(1, _maxSeats - 1);
            Vector3 center = _centerPoint.position;

            for (int i = 0; i < _maxSeats; i++)
            {
                float angle = (_startAngle + i * angleStep) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * _radius;
                Vector3 position = center + offset + Vector3.up * _hologramHeight;

                // Face center
                Quaternion rotation = Quaternion.LookRotation(center - position);
                rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0); // Keep upright

                _seats.Add(SeatPose.Empty(i, position, rotation));
            }
        }

        private void GenerateTheaterSeats()
        {
            // Side-by-side rows facing forward
            Vector3 center = _centerPoint.position;
            Vector3 forward = _centerPoint.forward;
            Vector3 right = _centerPoint.right;

            int seatsPerRow = Mathf.CeilToInt((float)_maxSeats / _theaterRows);
            int seatIndex = 0;

            for (int row = 0; row < _theaterRows; row++)
            {
                float rowOffset = row * _theaterSpacing;
                int seatsInThisRow = Mathf.Min(seatsPerRow, _maxSeats - seatIndex);
                float rowWidth = (seatsInThisRow - 1) * _theaterSpacing;

                for (int col = 0; col < seatsInThisRow; col++)
                {
                    float colOffset = col * _theaterSpacing - rowWidth / 2;
                    Vector3 position = center
                        + forward * (_theaterDistance + rowOffset)
                        + right * colOffset
                        + Vector3.up * _hologramHeight;

                    // Face the shared content (opposite of forward)
                    Quaternion rotation = Quaternion.LookRotation(-forward);

                    _seats.Add(SeatPose.Empty(seatIndex, position, rotation));
                    seatIndex++;
                }
            }
        }

        private void GenerateGridSeats()
        {
            // Grid layout for stress testing
            Vector3 center = _centerPoint.position;
            int rows = Mathf.CeilToInt((float)_maxSeats / _gridColumns);

            int seatIndex = 0;
            for (int row = 0; row < rows && seatIndex < _maxSeats; row++)
            {
                for (int col = 0; col < _gridColumns && seatIndex < _maxSeats; col++)
                {
                    float x = (col - (_gridColumns - 1) / 2f) * _gridSpacing;
                    float z = (row - (rows - 1) / 2f) * _gridSpacing;
                    Vector3 position = center + new Vector3(x, _hologramHeight, z);

                    // Face camera
                    Vector3 toCamera = _mainCamera != null
                        ? (_mainCamera.transform.position - position).normalized
                        : Vector3.back;
                    Quaternion rotation = Quaternion.LookRotation(toCamera);
                    rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);

                    _seats.Add(SeatPose.Empty(seatIndex, position, rotation));
                    seatIndex++;
                }
            }
        }

        private void ExpandSeats(int count)
        {
            _maxSeats += count;
            RegenerateSeats();
        }

        #endregion

        #region Seat Assignment

        private int FindNextAvailableSeat()
        {
            // Find first empty seat
            for (int i = 0; i < _seats.Count; i++)
            {
                if (!_seats[i].IsOccupied)
                    return i;
            }
            return -1;
        }

        private void ReassignAllSeats()
        {
            // Collect all current holograms
            var peerIds = new List<string>(_holograms.Keys);

            // Clear all seat assignments
            for (int i = 0; i < _seats.Count; i++)
            {
                var seat = _seats[i];
                seat.IsOccupied = false;
                seat.OccupantId = null;
                _seats[i] = seat;
            }

            // Reassign in order
            foreach (var peerId in peerIds)
            {
                var state = _holograms[peerId];
                int seatIndex = FindNextAvailableSeat();
                if (seatIndex >= 0)
                {
                    var seat = _seats[seatIndex];
                    seat.IsOccupied = true;
                    seat.OccupantId = peerId;
                    _seats[seatIndex] = seat;

                    state.TargetSeat = seat;
                    _holograms[peerId] = state;
                }
            }
        }

        #endregion

        #region Position Updates

        private void UpdateHologramPositions()
        {
            float dt = Time.deltaTime;

            foreach (var kvp in _holograms)
            {
                var state = kvp.Value;
                if (state.Transform == null) continue;

                // Lerp to target position
                state.CurrentPosition = Vector3.Lerp(
                    state.CurrentPosition,
                    state.TargetSeat.Position,
                    dt * _positionLerpSpeed
                );

                // Slerp to target rotation
                state.CurrentRotation = Quaternion.Slerp(
                    state.CurrentRotation,
                    state.TargetSeat.Rotation,
                    dt * _rotationLerpSpeed
                );

                state.Transform.position = state.CurrentPosition;
                state.Transform.rotation = state.CurrentRotation;

                _holograms[kvp.Key] = state;
            }
        }

        private Vector3 GetSpawnPosition()
        {
            // Spawn above and lerp down for a nice "drop in" effect
            if (_mainCamera != null)
            {
                return _mainCamera.transform.position + Vector3.up * 2f + _mainCamera.transform.forward * _radius;
            }
            return _centerPoint.position + Vector3.up * 3f;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!_debugDraw || !_showSeatGizmos) return;

            Gizmos.color = Color.cyan;

            // Draw center
            if (_centerPoint != null)
            {
                Gizmos.DrawWireSphere(_centerPoint.position, 0.1f);
            }

            // Draw seats
            foreach (var seat in _seats)
            {
                Gizmos.color = seat.IsOccupied ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(seat.Position, Vector3.one * 0.2f);

                // Draw facing direction
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(seat.Position, seat.Rotation * Vector3.forward * 0.3f);
            }
        }

        #endregion

        #region Internal Types

        private struct RemoteHologramState
        {
            public string PeerId;
            public Transform Transform;
            public SeatPose TargetSeat;
            public Vector3 CurrentPosition;
            public Quaternion CurrentRotation;
        }

        #endregion
    }
}
