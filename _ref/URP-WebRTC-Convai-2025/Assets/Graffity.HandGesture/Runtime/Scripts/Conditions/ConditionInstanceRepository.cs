using System;
using System.Collections.Generic;
using System.Linq;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Interface to manage the list of conditions of the gesture
    /// </summary>
    public interface IConditionInstanceRepository : IRepository<IConditionInstance> 
        ,IConditionUpdater 
        ,IConditionMatchDetector 
        ,IDisposable
    {
    }


    /// <summary>
    /// Class that manages the list of gesture conditions
    /// </summary>
    public class ConditionInstanceRepository : IConditionInstanceRepository
    {


        List<IConditionInstance> m_conditionInstanceList = new();
        /// <summary> List of condition classes </summary>
        public IReadOnlyList<IConditionInstance> ItemList => m_conditionInstanceList;


        public void Setup(IEnumerable<IConditionInstance> conditionInstanceList)
        {
            foreach (var conditionInstance in m_conditionInstanceList) conditionInstance.Dispose();
            m_conditionInstanceList.Clear();
            m_conditionInstanceList.AddRange(conditionInstanceList);
        }


        public void Update(IConditionUpdater.UpdateInfo updateInfo)
        {
            foreach (var conditionInstance in m_conditionInstanceList)
            {
                conditionInstance.Update(updateInfo);
            }
        }


        public bool IsAllMatched() => m_conditionInstanceList.All(value => value.GetResult());

        public bool IsAnyMatched() => m_conditionInstanceList.Any(value => value.GetResult());


        public void Dispose()
        {
            foreach (var conditionInstance in m_conditionInstanceList) conditionInstance.Dispose();
        }


    }


}