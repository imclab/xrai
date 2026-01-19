using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
#if UNITY_VISIONOS
using UnityEngine.XR.VisionOS;
#endif


namespace Graffity.HandGesture.Extentions
{


    /// <summary>
    /// Extention methods for XRHand
    /// </summary>
    public static class XRHandExtention
    {


        /// <summary>
        /// XRHand has no concept of parent-child structure, so it assigns its parent-child relationship.
        /// </summary>
        static Dictionary<XRHandJointID, XRHandJointID> ParentJointID = new Dictionary<XRHandJointID, XRHandJointID>()
        {
            {XRHandJointID.Wrist,XRHandJointID.Wrist},
            {XRHandJointID.Palm,XRHandJointID.Wrist},
            {XRHandJointID.ThumbMetacarpal,XRHandJointID.Wrist},
            {XRHandJointID.ThumbProximal,XRHandJointID.ThumbMetacarpal},
            {XRHandJointID.ThumbDistal,XRHandJointID.ThumbProximal},
            {XRHandJointID.ThumbTip,XRHandJointID.ThumbDistal},
            {XRHandJointID.IndexMetacarpal,XRHandJointID.Wrist},
            {XRHandJointID.IndexProximal,XRHandJointID.IndexMetacarpal},
            {XRHandJointID.IndexIntermediate,XRHandJointID.IndexProximal},
            {XRHandJointID.IndexDistal,XRHandJointID.IndexIntermediate},
            {XRHandJointID.IndexTip,XRHandJointID.IndexDistal},
            {XRHandJointID.MiddleMetacarpal,XRHandJointID.Wrist},
            {XRHandJointID.MiddleProximal,XRHandJointID.MiddleMetacarpal},
            {XRHandJointID.MiddleIntermediate,XRHandJointID.MiddleProximal},
            {XRHandJointID.MiddleDistal,XRHandJointID.MiddleIntermediate},
            {XRHandJointID.MiddleTip,XRHandJointID.MiddleDistal},
            {XRHandJointID.RingMetacarpal,XRHandJointID.Wrist},
            {XRHandJointID.RingProximal,XRHandJointID.RingMetacarpal},
            {XRHandJointID.RingIntermediate,XRHandJointID.RingProximal},
            {XRHandJointID.RingDistal,XRHandJointID.RingIntermediate},
            {XRHandJointID.RingTip,XRHandJointID.RingDistal},
            {XRHandJointID.LittleMetacarpal,XRHandJointID.Wrist},
            {XRHandJointID.LittleProximal,XRHandJointID.LittleMetacarpal},
            {XRHandJointID.LittleIntermediate,XRHandJointID.LittleProximal},
            {XRHandJointID.LittleDistal,XRHandJointID.LittleIntermediate},
            {XRHandJointID.LittleTip,XRHandJointID.LittleDistal},
#if UNITY_VISIONOS
            {(XRHandJointID)VisionOSHandJointID.ForearmWrist,(XRHandJointID)VisionOSHandJointID.ForearmWrist},
            {(XRHandJointID)VisionOSHandJointID.ForearmArm,(XRHandJointID)VisionOSHandJointID.ForearmArm},
#endif
        };


        /// <summary>
        /// Normal vector in palm direction
        /// </summary>
        /// <param name="h"> XRHand </param>
        /// <param name="isLeftHand"> Left hand? </param>
        /// <param name="crossVec"> normal vector </param>
        public static void PalmCrossVector(this XRHand h, bool isLeftHand, out Vector3 crossVec)
        {
            crossVec = Vector3.zero;
            bool isValid = true;
            isValid &= h.TryGetPose(XRHandJointID.Wrist, out var wrist);
            isValid &= h.TryGetPose(XRHandJointID.MiddleProximal, out var middleProximal);
            isValid &= h.TryGetPose(XRHandJointID.ThumbProximal, out var thumbProximal);
            if (!isValid) return;
            if (isLeftHand)
            {
                crossVec = Vector3.Cross(
                    (thumbProximal.position - wrist.position),
                    (middleProximal.position - wrist.position)
                ).normalized;
            }
            else
            {
                crossVec = Vector3.Cross(
                    (middleProximal.position - wrist.position),
                    (thumbProximal.position - wrist.position)
                ).normalized;
            }
        }


        /// <summary>
        /// Obtains the current Pose information of the specified Joint.
        /// </summary>
        /// <param name="hand"> XRHand </param>
        /// <param name="id"> Joint ID </param>
        /// <param name="p"> Pose </param>
        /// <returns> success or failure </returns>
        public static bool TryGetPose(this XRHand hand, XRHandJointID id, out Pose p)
        {
            XRHandJoint joint;
#if UNITY_VISIONOS
            if ((int)id == (int)VisionOSHandJointID.ForearmWrist || (int)id == (int)VisionOSHandJointID.ForearmArm)
            {
                joint = hand.GetVisionOSJoint((VisionOSHandJointID)id);
            }
            else
            {
                joint = hand.GetJoint(id);
            }
#else
            joint = hand.GetJoint(id);
#endif
            return joint.TryGetPose(out p);
        }


        /// <summary>
        /// Obtains the JointID of the parent of the specified JointID
        /// </summary>
        /// <param name="hand"> XRHand </param>
        /// <param name="id"> Joint ID </param>
        /// <param name="parentID"> Parent Joint ID </param>
        /// <returns> success or failure </returns>
        public static bool TryGetParentJointID(this XRHand hand, XRHandJointID id, out XRHandJointID parentID)
        {
            return ParentJointID.TryGetValue(id, out parentID);
        }


        /// <summary>
        /// Obtains the current pose information of the parent of the specified Joint.
        /// </summary>
        /// <param name="hand"> XRHand </param>
        /// <param name="id"> Joint ID </param>
        /// <param name="p"> Pose </param>
        /// <returns> success or failure </returns>
        public static bool TryGetParentPose(this XRHand hand, XRHandJointID id, out Pose p)
        {
            var parent = ParentJointID[id];
            var joint = hand.GetJoint(parent);
            return joint.TryGetPose(out p);
        }


    }


}