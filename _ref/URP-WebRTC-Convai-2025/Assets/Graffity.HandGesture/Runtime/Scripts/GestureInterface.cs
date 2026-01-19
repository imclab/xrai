using R3;
using UnityEngine.XR.Hands;


namespace Graffity.HandGesture
{


    /// <summary>
    /// Interface for updating gestures
    /// </summary>
    public interface IGestureUpdater
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
            public UpdateInfo(XRHandSubsystem subsystem,
                XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
                XRHandSubsystem.UpdateType updateType,
                HandInfo leftHand,
                HandInfo rightHand)
            {
                Subsystem = subsystem;
                UpdateSuccessFlags = updateSuccessFlags;
                UpdateType = updateType;
                LeftHand = leftHand;
                RightHand = rightHand;
            }
        }
        /// <summary>
        /// Hand tracking update event.
        /// This method must be called as a callback event of XRHandSubsystem.updatedHands
        /// </summary>
        /// <param name="updateInfo"> Information required for update </param>
        void OnUpdatedHands(UpdateInfo updateInfo);
    }


    /// <summary>
    /// Interface to retrieve whether the gesture conditions are met
    /// </summary>
    public interface IGestureResult
    {
        /// <summary> Are all conditions met? </summary>
        bool Result { get; }
        /// <summary> Event when Result is changed </summary>
        Observable<(bool result, IGestureUpdater.UpdateInfo updateInfo)> OnChangeResult();
        /// <summary> Event when Result is true </summary>
        Observable<IGestureUpdater.UpdateInfo> OnTrueResult();
        /// <summary> Event when Result is false </summary>
        Observable<IGestureUpdater.UpdateInfo> OnFalseResult();
    }


    /// <summary>
    /// Interface for managing gesture state
    /// </summary>
    public interface IGestureState
    {
        /// <summary> State Name </summary>
        string StateName { get; }
        /// <summary> State hash code </summary>
        int StateHashCode { get; }
        /// <summary>
        /// Update state
        /// </summary>
        /// <param name="newState"> New State Name </param>
        void UpdateState(string newState);
        /// <summary> Event when state changes </summary>
        Observable<(string newState, IGestureUpdater.UpdateInfo updateInfo)> OnChangeState();
    }


}