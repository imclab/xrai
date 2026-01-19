using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Beam
{


    /// <summary>
    /// Beam Sample Flow
    /// </summary>
    public class SampleBeamFlow : MonoBehaviour
    {


        [field: SerializeField]
        GestureManager GestureManager { get; set; }
        [field: SerializeField]
        BeamView BeamView { get; set; }
        [field: SerializeField]
        TargetView TargetView { get; set; }
        [field: SerializeField]
        StateWindowView StateWindowView { get; set; }

        SampleBeamModel Model { get; set; }
        BeamViewModel BeamViewModel { get; set; }
        TargetViewModel TargetViewModel { get; set; }
        StateWindowViewModel StateWindowViewModel { get; set; }


        void Start()
        {
            Model = new SampleBeamModel();
            Model.RegisterTo(this.destroyCancellationToken);

            BeamViewModel = new BeamViewModel();
            BeamViewModel.RegisterTo(this.destroyCancellationToken);
            BeamViewModel.Bind(Model);
            TargetViewModel = new TargetViewModel();
            TargetViewModel.RegisterTo(this.destroyCancellationToken);
            TargetViewModel.Bind(Model);

            BeamView.Bind(BeamViewModel);
            TargetView.Bind(TargetViewModel);

            // input
            if (GestureManager.TryGetGesture("Beam", out var gesture))
            {
                gesture.OnChangeState()
                    .Subscribe(value =>
                    {
                        Model.SetChargeFlag(value.newState == "Charge");
                        Model.SetShotFlag(value.newState == "Shot");
                    })
                    .RegisterTo(this.destroyCancellationToken);

                StateWindowViewModel = new StateWindowViewModel();
                StateWindowViewModel.RegisterTo(this.destroyCancellationToken);
                StateWindowViewModel.Bind(gesture);
                StateWindowView.Bind(StateWindowViewModel);
            }
        }


        void Update()
        {
            Model.Update(GestureManager);
        }


    }


}