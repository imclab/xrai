using UnityEngine;
using UnityEngine.XR.Hands;
using Graffity.HandGesture.Extentions;


namespace Graffity.HandGesture
{


    /// <summary>
    /// Class that converts and manages library joint information
    /// </summary>
    public sealed class JointInfo
    {


        /// <summary> XRHandJointID </summary>
        public XRHandJointID JointID { get; private set; }
        /// <summary> Current Pose </summary>
        public Pose CurrentPose { get; private set; }
        /// <summary> Is this joint on camera? </summary>
        public bool IsEnable { get; private set; }
        /// <summary> Parent Information </summary>
        public JointInfo ParentInfo { get; private set; } = null;
        /// <summary> Local Rotation </summary>
        public Quaternion LocalRotation { get; private set; }
        /// <summary> Local Rotation angle (-180~180)[deg.] </summary>
        public Vector3 LocalRotationNormalizedEulerAngles { get; private set; }
        /// <summary> World Rotation angle (-180~180)[deg.] <summary>
        public Vector3 WorldRotationNormalizedEulerAngles { get; private set; }


        public JointInfo(XRHandJointID jointID)
        {
            JointID = jointID;
        }


        /// <summary>
        /// Update based on current pose
        /// </summary>
        /// <param name="hand"></param>
        public void Update(HandInfo hand)
        {
            IsEnable = hand.Hand.TryGetPose(JointID, out var pose);
            if (!IsEnable) return;
            CurrentPose = pose;
            // If not on camera, nothing is updated.
            if (!IsEnable) return;
            // Set the parent
            if (ParentInfo == null)
            {
                if (!hand.Hand.TryGetParentJointID(JointID, out var parentID)) return;
                if (!hand.TryGetJointInfo(parentID, out var parentInfo)) return;
                ParentInfo = parentInfo;
            }
            // Update local rotation
            // Not performed without parental update
            if (ParentInfo.IsEnable)
            {
                LocalRotation = Quaternion.Inverse(CurrentPose.rotation) * ParentInfo.CurrentPose.rotation;
                var eulerAngles = LocalRotation.eulerAngles;
                LocalRotationNormalizedEulerAngles = CalculateDegreeAngles(eulerAngles);
            }
            // Update world rotation
            var worldEulerAngles = CurrentPose.rotation.eulerAngles;
            WorldRotationNormalizedEulerAngles = CalculateDegreeAngles(worldEulerAngles);
        }


        private Vector3 CalculateDegreeAngles(Vector3 eulerAngles)
        {
            return new Vector3(Mathf.Repeat(eulerAngles.x + 180, 360) - 180,
                Mathf.Repeat(eulerAngles.y + 180, 360) - 180,
                Mathf.Repeat(eulerAngles.z + 180, 360) - 180);
        }


    }


}