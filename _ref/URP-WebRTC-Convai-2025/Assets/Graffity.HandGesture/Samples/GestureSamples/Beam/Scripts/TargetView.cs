using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Beam
{


    public class TargetView : MonoBehaviour
    {


        public interface IViewModel
        {
            ReadOnlyReactiveProperty<Vector3> Position { get; }
            void Hit(Collider collider);
        }


        [field: SerializeField]
        TriggerEvent TriggerEvent { get; set; }


        public void Bind(IViewModel vm)
        {
            transform.position = vm.Position.CurrentValue;

            vm.Position
                .Subscribe(value => transform.position = value)
                .RegisterTo(this.destroyCancellationToken);

            TriggerEvent.TriggerStay
                .Subscribe(collider => vm.Hit(collider))
                .RegisterTo(this.destroyCancellationToken);
        }


        void Update() 
        {
            if(Camera.main == null) return;
            transform.LookAt(Camera.main.transform);
        }


    }


}