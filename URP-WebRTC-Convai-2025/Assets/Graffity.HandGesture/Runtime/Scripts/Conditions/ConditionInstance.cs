using System;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Interface to create IConditionInstance
    /// </summary>
    public interface IConditionInstanceCreator
    {
        /// <summary>
        /// Create an instance that uses this asset to make updates
        /// </summary>
        IConditionInstance CreateInstance();
    }


    /// <summary>
    /// Interface to determine gesture conditions
    /// </summary>
    public interface IConditionInstance : IConditionUpdater ,IDisposable
    {
        /// <summary>
        /// Called in initialization at creation in IConditionInstanceCraetor.CreateInstance()
        /// </summary>
        /// <param name="asset"> Settings used for this instance </param>
        void Initialize(IConditionAsset asset);
        /// <summary>
        /// Get Results
        /// </summary>
        bool GetResult();
    }


    /// <summary>
    /// Prescribed class for determining gesture conditions
    /// </summary>
    public abstract class ConditionInstance<TAsset> : IConditionInstance where TAsset : IConditionAsset
    {


        public TAsset Asset { get; private set; } = default;


        public void Initialize(IConditionAsset asset)
        {
            Asset = (TAsset)asset;
            Setup();
        }
        protected virtual void Setup() { }
        public abstract void Update(IConditionUpdater.UpdateInfo updateInfo);
        public abstract bool GetResult();
        public virtual void Dispose() { }


    }


}