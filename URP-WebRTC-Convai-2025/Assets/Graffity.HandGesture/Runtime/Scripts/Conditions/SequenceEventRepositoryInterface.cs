

namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Interface to manage a list of SequenceEvents
    /// </summary>
    public interface ISequenceEventRepository : IRepository<ISequenceEvent>, ISequenceEvent
    {
    }


    /// <summary>
    /// Interface for Getter of the class holding the SequenceEventRepository
    /// </summary>
    public interface ISequenceEventRepositoryGetter
    {
        public ISequenceEventRepository GetSequenceEventRepository();
    }


}