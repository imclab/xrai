using System;
using System.Collections.Generic;
using R3;
using System.Linq;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class SampleFingerGunModel : IDisposable
    {


        public IReadOnlyList<BulletModel> BulletModelList => m_bulletModelList;
        public IReadOnlyList<TargetModel> TargetModelList => m_targetModelList;

        List<BulletModel> m_bulletModelList = new();
        List<TargetModel> m_targetModelList = new();
        readonly CompositeDisposable m_disposable = new();

        const int BULLET_MAX = 30;
        const int TARGET_MAX = 5;


        public SampleFingerGunModel()
        {
            for (int i = 0; i < BULLET_MAX; i++)
            {
                var bulletModel = new BulletModel();
                m_bulletModelList.Add(bulletModel);
            }
            for (int i = 0; i < TARGET_MAX; i++)
            {
                var targetModel = new TargetModel();
                m_targetModelList.Add(targetModel);
            }
        }


        public void Update()
        {
            foreach (var bulletModel in m_bulletModelList)
            {
                bulletModel.Update();
            }
        }


        public void Dispose()
        {
            if (!m_disposable.IsDisposed) m_disposable.Dispose();
        }


        public void Shot(HandInfo hand)
        {
            if (hand.TryGetJointInfo(UnityEngine.XR.Hands.XRHandJointID.IndexTip, out var joint_it))
            {
                var bulletModel = m_bulletModelList.FirstOrDefault(value => !value.IsActive.CurrentValue);
                if (bulletModel == null)
                {
                    bulletModel = m_bulletModelList.FirstOrDefault();
                }
                m_bulletModelList.Remove(bulletModel);
                bulletModel.Shot(joint_it.CurrentPose.position, joint_it.CurrentPose.forward);
                m_bulletModelList.Add(bulletModel);
            }
        }


    }


}