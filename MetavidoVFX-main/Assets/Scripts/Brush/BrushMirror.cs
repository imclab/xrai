// BrushMirror.cs - Symmetry and mirror effects for brush strokes
// Part of Spec 011: OpenBrush Integration
//
// Creates mirrored copies of strokes across one or more planes.
// Supports radial symmetry with configurable count.

using System.Collections.Generic;
using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Mirror handler for creating symmetrical brush strokes.
    /// Supports single plane mirror and radial (n-fold) symmetry.
    /// </summary>
    public class BrushMirror : MonoBehaviour
    {
        public enum MirrorMode
        {
            Off,
            SinglePlane,    // Mirror across one plane
            DoublePlane,    // Mirror across two perpendicular planes
            Radial          // N-fold rotational symmetry
        }

        [Header("Configuration")]
        [SerializeField] MirrorMode _mode = MirrorMode.Off;
        [SerializeField] Transform _mirrorCenter;

        [Header("Single Plane Mirror")]
        [SerializeField] Vector3 _mirrorNormal = Vector3.right;

        [Header("Double Plane Mirror")]
        [SerializeField] Vector3 _secondPlaneNormal = Vector3.forward;

        [Header("Radial Symmetry")]
        [SerializeField, Range(2, 12)] int _radialCount = 4;
        [SerializeField] Vector3 _radialAxis = Vector3.up;

        [Header("Visual Feedback")]
        [SerializeField] bool _showMirrorPlane;
        [SerializeField] Color _mirrorPlaneColor = new Color(0.5f, 0.5f, 1f, 0.3f);
        [SerializeField] float _mirrorPlaneSize = 1f;

        // Properties
        public bool Enabled => _mode != MirrorMode.Off;
        public MirrorMode Mode => _mode;
        public int RadialCount => _radialCount;

        void Awake()
        {
            if (_mirrorCenter == null)
                _mirrorCenter = transform;
        }

        /// <summary>
        /// Create mirror copies of a finalized stroke
        /// </summary>
        public List<BrushStroke> CreateMirrorStrokes(BrushStroke sourceStroke, Transform parent)
        {
            var mirrorStrokes = new List<BrushStroke>();

            if (_mode == MirrorMode.Off || sourceStroke == null)
                return mirrorStrokes;

            var sourceData = sourceStroke.ToData();

            switch (_mode)
            {
                case MirrorMode.SinglePlane:
                    mirrorStrokes.Add(CreateMirroredStroke(sourceData, _mirrorNormal, parent));
                    break;

                case MirrorMode.DoublePlane:
                    // Mirror across first plane
                    mirrorStrokes.Add(CreateMirroredStroke(sourceData, _mirrorNormal, parent));
                    // Mirror across second plane
                    mirrorStrokes.Add(CreateMirroredStroke(sourceData, _secondPlaneNormal, parent));
                    // Mirror across both (corner reflection)
                    var doubleMirrorData = MirrorStrokeData(sourceData, _mirrorNormal);
                    mirrorStrokes.Add(CreateMirroredStroke(doubleMirrorData, _secondPlaneNormal, parent));
                    break;

                case MirrorMode.Radial:
                    float angleStep = 360f / _radialCount;
                    for (int i = 1; i < _radialCount; i++)
                    {
                        float angle = angleStep * i;
                        mirrorStrokes.Add(CreateRotatedStroke(sourceData, angle, parent));
                    }
                    break;
            }

            return mirrorStrokes;
        }

        BrushStroke CreateMirroredStroke(StrokeData sourceData, Vector3 planeNormal, Transform parent)
        {
            var mirroredData = MirrorStrokeData(sourceData, planeNormal);
            return InstantiateStroke(mirroredData, parent);
        }

        BrushStroke CreateRotatedStroke(StrokeData sourceData, float angle, Transform parent)
        {
            var rotatedData = RotateStrokeData(sourceData, angle);
            return InstantiateStroke(rotatedData, parent);
        }

        BrushStroke InstantiateStroke(StrokeData data, Transform parent)
        {
            var brush = BrushManager.Instance?.GetBrush(data.BrushId);
            if (brush == null) return null;

            var go = new GameObject("MirrorStroke");
            go.transform.SetParent(parent);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();

            var stroke = go.AddComponent<BrushStroke>();
            stroke.FromData(data, brush);
            return stroke;
        }

        StrokeData MirrorStrokeData(StrokeData source, Vector3 planeNormal)
        {
            var mirrorCenter = _mirrorCenter != null ? _mirrorCenter.position : Vector3.zero;
            planeNormal = planeNormal.normalized;

            var mirrored = new StrokeData
            {
                BrushId = source.BrushId,
                Color = (float[])source.Color.Clone(),
                Size = source.Size,
                Points = new List<ControlPointData>()
            };

            foreach (var point in source.Points)
            {
                var worldPos = new Vector3(point.Position[0], point.Position[1], point.Position[2]);
                var worldRot = new Quaternion(point.Rotation[0], point.Rotation[1], point.Rotation[2], point.Rotation[3]);

                // Mirror position across plane
                var mirroredPos = MirrorPoint(worldPos, mirrorCenter, planeNormal);

                // Mirror rotation
                var mirroredRot = MirrorRotation(worldRot, planeNormal);

                mirrored.Points.Add(new ControlPointData
                {
                    Position = new float[] { mirroredPos.x, mirroredPos.y, mirroredPos.z },
                    Rotation = new float[] { mirroredRot.x, mirroredRot.y, mirroredRot.z, mirroredRot.w },
                    Pressure = point.Pressure
                });
            }

            return mirrored;
        }

        StrokeData RotateStrokeData(StrokeData source, float angleDegrees)
        {
            var rotationCenter = _mirrorCenter != null ? _mirrorCenter.position : Vector3.zero;
            var rotation = Quaternion.AngleAxis(angleDegrees, _radialAxis);

            var rotated = new StrokeData
            {
                BrushId = source.BrushId,
                Color = (float[])source.Color.Clone(),
                Size = source.Size,
                Points = new List<ControlPointData>()
            };

            foreach (var point in source.Points)
            {
                var worldPos = new Vector3(point.Position[0], point.Position[1], point.Position[2]);
                var worldRot = new Quaternion(point.Rotation[0], point.Rotation[1], point.Rotation[2], point.Rotation[3]);

                // Rotate around center
                var offset = worldPos - rotationCenter;
                var rotatedPos = rotationCenter + rotation * offset;
                var rotatedRot = rotation * worldRot;

                rotated.Points.Add(new ControlPointData
                {
                    Position = new float[] { rotatedPos.x, rotatedPos.y, rotatedPos.z },
                    Rotation = new float[] { rotatedRot.x, rotatedRot.y, rotatedRot.z, rotatedRot.w },
                    Pressure = point.Pressure
                });
            }

            return rotated;
        }

        Vector3 MirrorPoint(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            float distance = Vector3.Dot(point - planePoint, planeNormal);
            return point - 2f * distance * planeNormal;
        }

        Quaternion MirrorRotation(Quaternion rotation, Vector3 planeNormal)
        {
            // Mirror the forward and up vectors
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;

            forward = Vector3.Reflect(forward, planeNormal);
            up = Vector3.Reflect(up, planeNormal);

            if (forward.sqrMagnitude > 0.001f)
                return Quaternion.LookRotation(forward, up);
            return rotation;
        }

        #region Public API

        /// <summary>
        /// Set mirror mode
        /// </summary>
        public void SetMode(MirrorMode mode)
        {
            _mode = mode;
        }

        /// <summary>
        /// Set the mirror center transform
        /// </summary>
        public void SetMirrorCenter(Transform center)
        {
            _mirrorCenter = center;
        }

        /// <summary>
        /// Set single plane mirror normal
        /// </summary>
        public void SetMirrorNormal(Vector3 normal)
        {
            _mirrorNormal = normal.normalized;
        }

        /// <summary>
        /// Set radial symmetry count (2-12)
        /// </summary>
        public void SetRadialCount(int count)
        {
            _radialCount = Mathf.Clamp(count, 2, 12);
        }

        /// <summary>
        /// Set radial axis
        /// </summary>
        public void SetRadialAxis(Vector3 axis)
        {
            _radialAxis = axis.normalized;
        }

        /// <summary>
        /// Cycle to next mirror mode
        /// </summary>
        public void CycleMode()
        {
            _mode = (MirrorMode)(((int)_mode + 1) % System.Enum.GetValues(typeof(MirrorMode)).Length);
        }

        #endregion

        void OnDrawGizmos()
        {
            if (!_showMirrorPlane || _mode == MirrorMode.Off) return;

            var center = _mirrorCenter != null ? _mirrorCenter.position : transform.position;

            Gizmos.color = _mirrorPlaneColor;

            switch (_mode)
            {
                case MirrorMode.SinglePlane:
                    DrawMirrorPlane(center, _mirrorNormal);
                    break;

                case MirrorMode.DoublePlane:
                    DrawMirrorPlane(center, _mirrorNormal);
                    DrawMirrorPlane(center, _secondPlaneNormal);
                    break;

                case MirrorMode.Radial:
                    // Draw radial lines
                    float angleStep = 360f / _radialCount;
                    for (int i = 0; i < _radialCount; i++)
                    {
                        var rotation = Quaternion.AngleAxis(angleStep * i, _radialAxis);
                        var direction = rotation * Vector3.forward;
                        Gizmos.DrawLine(center, center + direction * _mirrorPlaneSize);
                    }
                    break;
            }
        }

        void DrawMirrorPlane(Vector3 center, Vector3 normal)
        {
            // Create rotation to align plane with normal
            var rotation = Quaternion.LookRotation(normal);

            // Draw plane as a square
            var right = rotation * Vector3.up * _mirrorPlaneSize * 0.5f;
            var up = rotation * Vector3.right * _mirrorPlaneSize * 0.5f;

            Vector3[] corners = {
                center - right - up,
                center + right - up,
                center + right + up,
                center - right + up
            };

            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);
        }
    }
}
