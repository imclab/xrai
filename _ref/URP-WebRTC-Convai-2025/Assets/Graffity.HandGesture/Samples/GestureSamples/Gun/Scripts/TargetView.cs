using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.FingerGun
{


    public class TargetView : MonoBehaviour
    {


        public interface IViewModel
        {
            ReadOnlyReactiveProperty<Vector3> Position { get; }
            void Hit(Collision collision);
        }


        [field: SerializeField]
        CollisionEvent CollisionEvent { get; set; }


        public void Bind(IViewModel vm)
        {
            transform.position = vm.Position.CurrentValue;

            vm.Position
                .Subscribe(value => transform.position = value)
                .RegisterTo(this.destroyCancellationToken);

            CollisionEvent.CollisionEnter
                .Subscribe(collision => vm.Hit(collision))
                .RegisterTo(this.destroyCancellationToken);
        }


        void Update() 
        {
            if(Camera.main == null) return;
            transform.LookAt(Camera.main.transform);
        }


    }


}