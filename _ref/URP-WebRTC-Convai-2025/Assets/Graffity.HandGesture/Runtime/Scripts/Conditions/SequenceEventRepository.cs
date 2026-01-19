using System.Collections.Generic;
using System.Linq;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Class that manages the list of SequenceEvents
    /// </summary>
    public class SequenceEventRepository : ISequenceEventRepository
    {


        List<ISequenceEvent> m_sequenceEventList { get; set; } = new();
        public IReadOnlyList<ISequenceEvent> ItemList => m_sequenceEventList;


        public void Setup(IEnumerable<ISequenceEvent> sequenceConditionEventList)
        {
            m_sequenceEventList.AddRange(sequenceConditionEventList);
        }


        public void Setup(params ConditionInstanceRepository[] conditionInstanceRepositories)
        {
            foreach (var conditionInstanceRepository in conditionInstanceRepositories)
            {
                var sequenceEventList = conditionInstanceRepository.ItemList
                    .Where(value => value is ISequenceEvent)
                    .Select(value => value as ISequenceEvent)
                    .ToList();
                m_sequenceEventList.AddRange(sequenceEventList);
                foreach (var conditionInstance in conditionInstanceRepository.ItemList)
                {
                    if (!(conditionInstance is ISequenceEventRepositoryGetter))
                    {
                        continue;
                    }
                    Setup((conditionInstance as ISequenceEventRepositoryGetter).GetSequenceEventRepository().ItemList);
                }
            }
        }


        public void BeginSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            foreach (var sequenceEvent in m_sequenceEventList)
            {
                sequenceEvent.BeginSequence(updateInfo);
            }
        }

        public void CancelSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            foreach (var sequenceEvent in m_sequenceEventList)
            {
                sequenceEvent.CancelSequence(updateInfo);
            }
        }

        public void EndSequence(IConditionUpdater.UpdateInfo updateInfo)
        {
            foreach (var sequenceEvent in m_sequenceEventList)
            {
                sequenceEvent.EndSequence(updateInfo);
            }
        }


    }


}