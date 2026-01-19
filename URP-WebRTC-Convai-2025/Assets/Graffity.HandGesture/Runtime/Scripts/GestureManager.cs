using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Hands;


namespace Graffity.HandGesture
{


    /// <summary>
    /// Class for managing and updating all gestures
    /// </summary>
    public class GestureManager : MonoBehaviour
    {


        [field: SerializeField]
        List<Gesture> m_gestureList { get; set; } = new();

        /// <summary> List of gestures </summary>
        public IReadOnlyList<Gesture> GestureList => m_gestureList;
        /// <summary> Left hand information </summary>
        public HandInfo LeftHandInfo { get; private set; } = new(HandInfo.HandType.Left);
        /// <summary> Right hand information </summary>
        public HandInfo RightHandInfo { get; private set; } = new(HandInfo.HandType.Right);
        /// <summary> XRHandSubsystem exists and is running </summary>
        public bool IsValid { get; protected set; } = false;

        readonly List<XRHandSubsystem> subsystemsReuse = new();
        XRHandSubsystem subsystem = null;


        void Awake()
        {
            InitializeSubsystem();
            foreach (var gesture in GestureList)
            {
                gesture?.Setup();
            }
        }

        void Update()
        {
            if (subsystem != null)
            {
                if (subsystem.running) return;
                UnsubscribeSubsystem();
                IsValid = false;
                return;
            }
            if (IsValid) return;
            InitializeSubsystem();
        }

        void OnDestroy()
        {
            foreach (var gesture in GestureList)
            {
                gesture?.Dispose();
            }
        }

        void InitializeSubsystem()
        {
            SubsystemManager.GetSubsystems<XRHandSubsystem>(subsystemsReuse);

            for (var i = 0; i < subsystemsReuse.Count; ++i)
            {
                var handSubsystem = subsystemsReuse[i];
                if (handSubsystem.running)
                {
                    UnsubscribeSubsystem();
                    subsystem = handSubsystem;
                    break;
                }
            }
            if (subsystem == null) return;
            SubscribeHandSubsystem();
            IsValid = true;
        }

        void UnsubscribeSubsystem()
        {
            if (subsystem == null) return;
            subsystem.updatedHands -= this.OnUpdatedHands;
            subsystem = null;
        }

        void SubscribeHandSubsystem()
        {
            if (subsystem == null) return;
            subsystem.updatedHands += this.OnUpdatedHands;
        }

        void OnUpdatedHands(XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType
        )
        {
            // Update information on both hands
            LeftHandInfo.Update(subsystem.leftHand);
            RightHandInfo.Update(subsystem.rightHand);
            // Gesture Update
            var updateInfo = new IGestureUpdater.UpdateInfo(subsystem, updateSuccessFlags, updateType, LeftHandInfo, RightHandInfo);
            foreach (var gesture in GestureList)
            {
                gesture?.OnUpdatedHands(updateInfo);
            }
        }

        /// <summary>
        /// Get Gesture
        /// </summary>
        /// <param name="id"> Gesture ID </param>      
        /// <param name="gesture"> Obtained Gesture </param>   
        /// <returns> Did you get Gesture? </returns>   
        public bool TryGetGesture(string id, out Gesture gesture)
        {
            gesture = GestureList.FirstOrDefault(value => value.ID == id);
            return gesture != null;
        }


    }


}