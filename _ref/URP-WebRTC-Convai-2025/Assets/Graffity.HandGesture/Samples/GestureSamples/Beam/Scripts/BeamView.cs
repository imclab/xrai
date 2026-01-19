using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Beam
{


    public class BeamView : MonoBehaviour
    {


        public interface IViewModel
        {
            ReadOnlyReactiveProperty<Vector3> Position { get; }
            ReadOnlyReactiveProperty<bool> IsCharge { get; }
            ReadOnlyReactiveProperty<float> ChargeScale { get; }
            ReadOnlyReactiveProperty<bool> IsShot { get; }
            ReadOnlyReactiveProperty<Quaternion> ShotRotation { get; }
        }


        [field: SerializeField]
        GameObject ChargeObject { get; set; }
        [field: SerializeField]
        GameObject ShotRoot { get; set; }
        [field: SerializeField]
        Animator BeamAnimator { get; set; }


        public void Bind(IViewModel vm)
        {
            transform.position = vm.Position.CurrentValue;
            ChargeObject.SetActive(vm.IsCharge.CurrentValue || vm.IsShot.CurrentValue);
            ChargeObject.transform.localScale = new Vector3(vm.ChargeScale.CurrentValue, vm.ChargeScale.CurrentValue, vm.ChargeScale.CurrentValue);
            ShotRoot.transform.rotation = vm.ShotRotation.CurrentValue;

            vm.Position
                .Subscribe(value => transform.position = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.IsCharge
                .Subscribe(value => ChargeObject.SetActive(value || vm.IsShot.CurrentValue))
                .RegisterTo(this.destroyCancellationToken);
            vm.ChargeScale
                .Subscribe(value => ChargeObject.transform.localScale = new Vector3(value, value, value))
                .RegisterTo(this.destroyCancellationToken);
            vm.IsShot
                .Subscribe(value =>
                {
                    ChargeObject.SetActive(vm.IsCharge.CurrentValue || value);
                    BeamAnimator.Play(value ? "beam_in" : "beam_out");
                })
                .RegisterTo(this.destroyCancellationToken);
            vm.ShotRotation
                .Subscribe(value => ShotRoot.transform.rotation = value)
                .RegisterTo(this.destroyCancellationToken);
        }


    }


}