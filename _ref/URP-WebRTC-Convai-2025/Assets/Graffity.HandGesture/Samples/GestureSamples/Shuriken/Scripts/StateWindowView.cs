using R3;
using TMPro;
using UnityEngine;


namespace Graffity.HandGesture.Sample.Shuriken
{


    public class StateWindowView : MonoBehaviour
    {


        public interface IViewModel
        {
            ReadOnlyReactiveProperty<string> GestureName { get; }
            ReadOnlyReactiveProperty<string> CurrentState { get; }
        }


        [field: SerializeField]
        TextMeshProUGUI GestureNameText { get; set; }
        [field: SerializeField]
        TextMeshProUGUI CurrentStateText { get; set; }
        [field: SerializeField]
        Animator BeginAnimator { get; set; }
        [field: SerializeField]
        Animator FinishAnimator { get; set; }


        static readonly Vector3 OFFSET = new Vector3(1f, -0.3f, 1f);


        public void Bind(IViewModel vm)
        {
            GestureNameText.text = vm.GestureName.CurrentValue;
            CurrentStateText.text = vm.CurrentState.CurrentValue;

            vm.GestureName
                .Subscribe(value => GestureNameText.text = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.CurrentState
                .Subscribe(value => CurrentStateText.text = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.CurrentState
                .Subscribe(value => BeginAnimator.Play(value == "Begin" ? "on" : "off"))
                .RegisterTo(this.destroyCancellationToken);
            vm.CurrentState
                .Subscribe(value => FinishAnimator.Play(value == "Finish" ? "on" : "off"))
                .RegisterTo(this.destroyCancellationToken);
        }


        void Update()
        {
            if (Camera.main == null) return;
            transform.position = Camera.main.transform.position + OFFSET;
            transform.LookAt(Camera.main.transform);
        }


    }


}