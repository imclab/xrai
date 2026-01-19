using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class BulletView : MonoBehaviour
    {


        public interface IViewModel
        {
            ReadOnlyReactiveProperty<Vector3> ShotPosition { get; }
            ReadOnlyReactiveProperty<bool> IsActive { get; }
            void Hit(Collision collision);
        }


        [field: SerializeField]
        CollisionEvent CollisionEvent { get; set; }


        public void Bind(IViewModel vm)
        {
            transform.position = vm.ShotPosition.CurrentValue;

            vm.ShotPosition
                .Subscribe(value => transform.position = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.IsActive
                .Subscribe(value => gameObject.SetActive(value))
                .RegisterTo(this.destroyCancellationToken);

            CollisionEvent.CollisionEnter
                .Subscribe(collision => vm.Hit(collision))
                .RegisterTo(this.destroyCancellationToken);
        }


    }


}