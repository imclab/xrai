using System.Collections.Generic;
using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.FingerGun
{


    /// <summary>
    /// FingerGun Sample Flow
    /// </summary>
    public class SampleFingerGunFlow : MonoBehaviour
    {


        [field: SerializeField]
        GestureManager GestureManager { get; set; }
        [field: SerializeField]
        Transform BulletRoot { get; set; }
        [field: SerializeField]
        GameObject BulletViewPrefab { get; set; }
        [field: SerializeField]
        Transform TargetRoot { get; set; }
        [field: SerializeField]
        GameObject TargetViewPrefab { get; set; }
        [field: SerializeField]
        StateWindowView StateWindowView_L { get; set; }
        [field: SerializeField]
        StateWindowView StateWindowView_R { get; set; }

        SampleFingerGunModel Model { get; set; }
        List<BulletViewModel> BulletViewModelList { get; set; } = new();
        List<TargetViewModel> TargetViewModelList { get; set; } = new();
        StateWindowViewModel StateWindowViewModel_L { get; set; }
        StateWindowViewModel StateWindowViewModel_R { get; set; }


        void Start()
        {
            // Instantiate SampleFingerGunModel
            Model = new SampleFingerGunModel();
            Model.RegisterTo(this.destroyCancellationToken);

            // BulletViewModel and BulletView Instantiate
            foreach (var bulletModel in Model.BulletModelList)
            {
                var bulletViewModel = new BulletViewModel();
                bulletViewModel.RegisterTo(this.destroyCancellationToken);
                bulletViewModel.Bind(bulletModel);
                BulletViewModelList.Add(bulletViewModel);
                var bulletObject = Instantiate(BulletViewPrefab, BulletRoot);
                if (bulletObject.TryGetComponent<BulletView>(out var bulletView))
                {
                    bulletView.Bind(bulletViewModel);
                }
            }

            // TargetViewModel and TargetView Instantiate
            foreach (var targetModel in Model.TargetModelList)
            {
                var targetViewModel = new TargetViewModel();
                targetViewModel.RegisterTo(this.destroyCancellationToken);
                targetViewModel.Bind(targetModel);
                TargetViewModelList.Add(targetViewModel);
                var targetObject = Instantiate(TargetViewPrefab, TargetRoot);
                if (targetObject.TryGetComponent<TargetView>(out var targetView))
                {
                    targetView.Bind(targetViewModel);
                }
            }

            // input
            if (GestureManager.TryGetGesture("Left_FingerGun", out var l_gesture))
            {
                l_gesture.OnChangeResult()
                    .Where(value => value.result)
                    .Subscribe(_ => Model.Shot(GestureManager.LeftHandInfo))
                    .RegisterTo(this.destroyCancellationToken);
                StateWindowViewModel_L = new StateWindowViewModel(true);
                StateWindowViewModel_L.RegisterTo(this.destroyCancellationToken);
                StateWindowViewModel_L.Bind(l_gesture);
                StateWindowView_L.Bind(StateWindowViewModel_L);
            }
            if (GestureManager.TryGetGesture("Right_FingerGun", out var r_gesture))
            {
                r_gesture.OnChangeResult()
                    .Where(value => value.result)
                    .Subscribe(_ => Model.Shot(GestureManager.RightHandInfo))
                    .RegisterTo(this.destroyCancellationToken);
                StateWindowViewModel_R = new StateWindowViewModel(false);
                StateWindowViewModel_R.RegisterTo(this.destroyCancellationToken);
                StateWindowViewModel_R.Bind(r_gesture);
                StateWindowView_R.Bind(StateWindowViewModel_R);
            }
        }


        void Update()
        {
            Model.Update();
        }


    }


}