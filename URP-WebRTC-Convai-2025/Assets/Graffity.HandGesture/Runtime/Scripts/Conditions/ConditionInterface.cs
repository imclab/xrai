using System.Collections.Generic;
using UnityEngine.XR.Hands;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Repository Interface
    /// </summary>
    public interface IRepository<TItem>
    {
        IReadOnlyList<TItem> ItemList { get; }
        void Setup(IEnumerable<TItem> itemList);
    }



    /// <summary>
    /// Interface for condition update methods
    /// </summary>
    public interface IConditionUpdater
    {
        /// <summary>
        /// Information required for update
        /// </summary>
        public class UpdateInfo
        {
            public XRHandSubsystem Subsystem { get; private set; }
            public XRHandSubsystem.UpdateSuccessFlags UpdateSuccessFlags { get; private set; }
            public XRHandSubsystem.UpdateType UpdateType { get; private set; }
            public HandInfo LeftHand { get; private set; }
            public HandInfo RightHand { get; private set; }
            public IGestureState GestureState { get; private set; }
            public UpdateInfo(XRHandSubsystem subsystem,
                XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
                XRHandSubsystem.UpdateType updateType,
                HandInfo leftHand,
                HandInfo rightHand,
                IGestureState gestureState)
            {
                Subsystem = subsystem;
                UpdateSuccessFlags = updateSuccessFlags;
                UpdateType = updateType;
                LeftHand = leftHand;
                RightHand = rightHand;
                GestureState = gestureState;
            }
        }
        void Update(UpdateInfo updateInfo);
    }


    /// <summary>
    /// Interface to retrieve results of gesture conditions
    /// </summary>
    public interface IConditionMatchDetector
    {
        bool IsAllMatched();
        bool IsAnyMatched();
    }


}