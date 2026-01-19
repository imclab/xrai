using System.Linq;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Interface for calling events in Sequence
    /// </summary>
    public interface ISequenceEvent
    {
        /// <summary>
        /// Event called at the start of a sequence
        /// </summary>
        void BeginSequence(IConditionUpdater.UpdateInfo updateInfo);
        /// <summary>
        /// Event called when a sequence is canceled
        /// </summary>
        void CancelSequence(IConditionUpdater.UpdateInfo updateInfo);
        /// <summary>
        /// Event called at the end of a sequence
        /// </summary>
        void EndSequence(IConditionUpdater.UpdateInfo updateInfo);
    }


    /// <summary>
    /// Interface for some conditions which are only available as sequence conditions.
    /// </summary>
    public interface ISequenceOnlyCondition {}


}