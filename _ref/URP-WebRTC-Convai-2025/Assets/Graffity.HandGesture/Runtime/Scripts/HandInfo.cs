using System;
using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;


namespace Graffity.HandGesture
{


    /// <summary>
    /// Class that converts and manages library hand information
    /// </summary>
    public sealed class HandInfo
    {


        /// <summary> Hand type </summary>
        public enum HandType
        {
            Left,
            Right,
        }


        /// <summary> XRHand </summary>
        public XRHand Hand { get; private set; }
        /// <summary> Hand type </summary>
        public HandType Type { get; private set; }

        Dictionary<XRHandJointID, JointInfo> m_jointInfoList = new();
        // <summary> List of Joint Information </summary>
        public IDictionary<XRHandJointID, JointInfo> JointInfoList => m_jointInfoList;

        Transform CameraTransform { get; set; } = null;

        // <summary> Wrist Rotation </summary>
        public Quaternion Rotation { get; private set; } = new();
        // <summary> Coordinates between wrist and middleProximal </summary>
        public Vector3 Position { get; private set; } = new();
        // <summary> move vector </summary>
        public Vector3 MoveVec { get; private set; } = new();
        // <summary> Movement vector of the wrist rotation reference </summary>
        public Vector3 LocalMoveVec { get; private set; } = new();
        // <summary> Coordinates with respect to the camera coordinates </summary>
        public Vector3 CameraLocalPosition { get; private set; } = new();
        // <summary> Movement vector with respect to camera rotation </summary>
        public Vector3 CameraLocalMoveVec { get; private set; } = new();

        // <summary> Palm Direction </summary>
        public Vector3 PalmForward { get; private set; } = new();

        // <summary> Coordinates of the sphere using 4 points: Wrist, ThumbProximal (thumb), IndexProximal (index finger), LittleProximal (little finger). </summary>
        public Vector3 SpherePosition { get; private set; } = new();
        // <summary> Radius of the sphere using 4 points: wrist, ThumbProximal (thumb), IndexProximal (index finger), LittleProximal (little finger). </summary>
        public float SphereRadius { get; private set; } = new();

        // Since it is not possible to acquire the latest pose for each frame,
        // MoveVec is assumed to be unmoved if it has not been moved for the number of frames in STOP_MAX_COUNT.
        int StopCount { get; set; } = 0;
        readonly int STOP_MAX_COUNT = 10;


        public HandInfo(HandType handType)
        {
            Type = handType;
        }


        /// <summary>
        /// Update Information
        /// </summary>
        /// <param name="hand"> XRHand </param>
        public void Update(XRHand hand)
        {
            Hand = hand;
            CameraTransform = Camera.main.transform;
            JointInfoUpdate();
            HandUpdate();
        }


        void AddJointInfo(XRHandJointID jointID)
        {
            JointInfo jointInfo;
            if (!TryGetJointInfo(jointID, out jointInfo))
            {
                jointInfo = new JointInfo(jointID);
                m_jointInfoList.Add(jointID, jointInfo);
            }
        }

        void JointInfoUpdate()
        {
            foreach (XRHandJointID jointID in Enum.GetValues(typeof(XRHandJointID)))
            {
                AddJointInfo(jointID);
            }
#if UNITY_VISIONOS
            AddJointInfo((XRHandJointID)UnityEngine.XR.VisionOS.VisionOSHandJointID.ForearmArm);
#endif
            foreach (var jointInfo in m_jointInfoList)
            {
                jointInfo.Value.Update(this);
            }
        }

        void HandUpdate()
        {
            // Palm coordinates update
            UpdatePosition();
            // Update palm orientation
            UpdatePalmForward();
            // Palm sphere update
            UpdateSphere();
        }

        void UpdatePosition()
        {
            bool isValid = true;
            isValid &= TryGetEnableJointInfo(XRHandJointID.Wrist, out var wrist);
            isValid &= TryGetEnableJointInfo(XRHandJointID.MiddleProximal, out var middleProximal);
            // 手の座標を更新
            if (!isValid) return;
            var position = wrist.CurrentPose.position + (middleProximal.CurrentPose.position - wrist.CurrentPose.position) / 2.0f;
            if (position == Position)
            {
                // 数フレームジョイントの更新が遅れてるみたいなので完全に止まっているかを判定するために数フレーム無視する
                StopCount++;
                if (StopCount < STOP_MAX_COUNT)
                {
                    return;
                }
            }
            StopCount = 0;
            Rotation = wrist.CurrentPose.rotation;
            MoveVec = position - Position;
            Position = position;
            LocalMoveVec = Rotation * MoveVec.normalized * MoveVec.magnitude;
            CameraLocalPosition = Position - CameraTransform.transform.position;
            CameraLocalMoveVec = CameraTransform.InverseTransformDirection(MoveVec.normalized) * MoveVec.magnitude;
        }

        void UpdatePalmForward()
        {
            bool isValid = true;
            isValid &= TryGetJointInfo(XRHandJointID.Wrist, out var wrist);
            isValid &= TryGetJointInfo(XRHandJointID.MiddleProximal, out var middleProximal);
            isValid &= TryGetJointInfo(XRHandJointID.ThumbProximal, out var thumbProximal);
            if (!isValid) return;
            PalmForward = Type switch
            {
                HandType.Left => Vector3.Cross((thumbProximal.CurrentPose.position - wrist.CurrentPose.position).normalized,
                                        (middleProximal.CurrentPose.position - wrist.CurrentPose.position).normalized),
                HandType.Right => Vector3.Cross((middleProximal.CurrentPose.position - wrist.CurrentPose.position).normalized,
                                        (thumbProximal.CurrentPose.position - wrist.CurrentPose.position).normalized),
                _ => Vector3.zero
            };
            PalmForward.Normalize();
        }

        void UpdateSphere()
        {
            bool isValid = true;
            isValid &= TryGetJointInfo(XRHandJointID.Wrist, out var wrist);
            isValid &= TryGetJointInfo(XRHandJointID.ThumbProximal, out var thumbProximal);
            isValid &= TryGetJointInfo(XRHandJointID.IndexProximal, out var indexProximal);
            isValid &= TryGetJointInfo(XRHandJointID.LittleProximal, out var littleProximal);
            if (!isValid) return;
            // Find the smallest sphere passing through 4 points
            Calc4PointBS(wrist.CurrentPose.position,
                    thumbProximal.CurrentPose.position,
                    indexProximal.CurrentPose.position,
                    littleProximal.CurrentPose.position,
                    out var outPosition,
                    out var outRadius);
            if (outRadius != 0f)
            {
                SpherePosition = outPosition;
                SphereRadius = outRadius;
            }
        }

        static void Calc3PointBS(in Vector3 p0, in Vector3 p1, in Vector3 p2, out Vector3 outPosition, out float outRadius)
        {
            // obtuse triangle
            var vp1p0 = p1 - p0;
            var vp2p0 = p2 - p0;
            var vp0p1 = p0 - p1;
            var vp0p2 = p0 - p2;
            var vp1p2 = p1 - p2;
            var vp2p1 = p2 - p1;
            float dot0 = Vector3.Dot(vp1p0, vp2p0);
            if (dot0 <= 0.0f)
            {
                outPosition = (p1 + p2) / 2.0f;
                outRadius = Vector3.Distance(p1, p2) / 2.0f;
                return;
            }
            float dot1 = Vector3.Dot(vp0p1, vp2p1);
            if (dot1 <= 0.0f)
            {
                outPosition = (p0 + p2) / 2.0f;
                outRadius = Vector3.Distance(p0, p2) / 2.0f;
                return;
            }
            float dot2 = Vector3.Dot(vp0p2, vp1p2);
            if (dot2 <= 0.0f)
            {
                outPosition = (p0 + p1) / 2.0f;
                outRadius = Vector3.Distance(p0, p1) / 2.0f;
                return;
            }
            // acute triangle
            Vector3 N;
            N = Vector3.Cross(vp1p0, vp2p0);
            Vector3 v0, v1, e0, e1;
            v0 = Vector3.Cross(vp2p1, N);
            v1 = Vector3.Cross(vp2p0, N);
            e0 = (p2 + p1) * 0.5f;
            e1 = (p2 + p0) * 0.5f;
            float a = Vector3.Dot(v0, v1);
            float b = Vector3.Dot(v0, v0);
            float c = -Vector3.Dot(e1 - e0, v0);
            float d = Vector3.Dot(v1, v1);
            float e = -Vector3.Dot(e1 - e0, v1);
            float denom = -a * a + b * d;
            float s = (-c * d + a * e) / denom;
            outPosition = e0 + s * v0;
            outRadius = Vector3.Distance(outPosition, -p0);
        }

        static void Calc4PointBS(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3, out Vector3 outPosition, out float outRadius)
        {
            // Obtain the center point of the bounding sphere enclosing triangle p0p1p2
            Calc3PointBS(p0, p1, p2, out var c0, out var tmpR);
            Vector3 v;
            v = Vector3.Cross(p1 - p0, p2 - p0);
            v = v.normalized;
            float D = Vector3.Dot(p0, p0) - Vector3.Dot(p3, p3);
            float a = (-2.0f * Vector3.Dot(c0, p0 - p3) + D) / (2.0f * Vector3.Dot(v, p0 - p3));
            outPosition = c0 + a * v;
            outRadius = Vector3.Distance(outPosition, p0);
        }


        /// <summary>
        /// Get JointInfo
        /// </summary>
        /// <param name="jointID"> Joint ID </param>
        /// <param name="jointInfo"> Obtained JointInfo </param>
        /// <returns> Did you get JointInfo? </returns>
        public bool TryGetJointInfo(XRHandJointID jointID, out JointInfo jointInfo)
        {
            return m_jointInfoList.TryGetValue(jointID, out jointInfo);
        }

        /// <summary>
        /// Obtains the JointInfo from the camera
        /// </summary>
        /// <param name="jointID"> Joint ID </param>
        /// <param name="jointInfo"> Obtained JointInfo </param>
        /// <returns> Did you get JointInfo? </returns>
        public bool TryGetEnableJointInfo(XRHandJointID jointID, out JointInfo jointInfo)
        {
            if (TryGetJointInfo(jointID, out jointInfo))
            {
                return jointInfo.IsEnable;
            }
            return false;
        }


    }


}