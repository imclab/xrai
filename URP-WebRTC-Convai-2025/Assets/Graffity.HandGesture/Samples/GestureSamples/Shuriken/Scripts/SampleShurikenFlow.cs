using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Shuriken
{


    /// <summary>
    /// Shuriken Sample Flow
    /// </summary>
    public class SampleShurikenFlow : MonoBehaviour
    {


        [field: SerializeField]
        GestureManager GestureManager { get; set; }
        [field: SerializeField]
        ShurikenView ShurikenView { get; set; }
        [field: SerializeField]
        TargetView TargetView { get; set; }
        [field: SerializeField]
        StateWindowView StateWindowView { get; set; }

        SampleShurikenModel Model { get; set; }
        ShurikenViewModel ShurikenViewModel { get; set; }
        TargetViewModel TargetViewModel { get; set; }
        StateWindowViewModel StateWindowViewModel { get; set; }


        void Start()
        {
            Model = new SampleShurikenModel();
            Model.RegisterTo(this.destroyCancellationToken);

            ShurikenViewModel = new ShurikenViewModel();
            ShurikenViewModel.RegisterTo(this.destroyCancellationToken);
            ShurikenViewModel.Bind(Model);
            TargetViewModel = new TargetViewModel();
            TargetViewModel.RegisterTo(this.destroyCancellationToken);
            TargetViewModel.Bind(Model);

            ShurikenView.Bind(ShurikenViewModel);
            TargetView.Bind(TargetViewModel);

            // input
            if (GestureManager.TryGetGesture("Shuriken", out var gesture))
            {
                gesture.OnChangeResult()
                    .Where(value => value.result)
                    .Subscribe(_ => Model.Shot(GestureManager))
                    .RegisterTo(this.destroyCancellationToken);

                StateWindowViewModel = new StateWindowViewModel();
                StateWindowViewModel.RegisterTo(this.destroyCancellationToken);
                StateWindowViewModel.Bind(gesture);
                StateWindowView.Bind(StateWindowViewModel);
            }
        }


        void Update()
        {
            Model.Update();
        }


    }


}