using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Shuriken
{


    public class ShurikenView : MonoBehaviour
    {


        public interface IViewModel
        {
            ReadOnlyReactiveProperty<bool> IsShot { get; }
            ReadOnlyReactiveProperty<Vector3> ShotPosition { get; }
            ReadOnlyReactiveProperty<Quaternion> ShotRotation { get; }
            void Hit(Collision collision);
        }


        [field: SerializeField]
        CollisionEvent CollisionEvent { get; set; }


        public void Bind(IViewModel vm)
        {
            gameObject.SetActive(vm.IsShot.CurrentValue);
            transform.position = vm.ShotPosition.CurrentValue;
            transform.rotation = vm.ShotRotation.CurrentValue;

            vm.IsShot
                .Subscribe(value => gameObject.SetActive(value))
                .RegisterTo(this.destroyCancellationToken);
            vm.ShotPosition
                .Subscribe(value => transform.position = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.ShotRotation
                .Subscribe(value => transform.rotation = value)
                .RegisterTo(this.destroyCancellationToken);

            CollisionEvent.CollisionEnter
                .Subscribe(collision => vm.Hit(collision))
                .RegisterTo(this.destroyCancellationToken);
        }


        void Update()
        {
            transform.Rotate(0f, 10f, 0f);
        }


    }


}