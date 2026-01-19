using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    public class GestureDoubleIconView : MonoBehaviour
    {


        public interface IViewModel : IGestureIconViewModel
        {
            ReadOnlyReactiveProperty<Sprite> Icon1Image { get; }
            ReadOnlyReactiveProperty<Sprite> Icon2Image { get; }
        }


        [field: SerializeField]
        Animator Animator { get; set; } = default;
        [field: SerializeField]
        TextMeshProUGUI GestureNameText { get; set; } = default;
        [field: SerializeField]
        Image Icon1Image { get; set; } = default;
        [field: SerializeField]
        Image Icon2Image { get; set; } = default;


        public void Bind(IViewModel vm)
        {
            Animator.Play(vm.IsChangeResult.CurrentValue ? "on" : "off");
            GestureNameText.text = vm.GestureName.CurrentValue;
            Icon1Image.sprite = vm.Icon1Image.CurrentValue;
            Icon2Image.sprite = vm.Icon2Image.CurrentValue;

            vm.IsChangeResult
                .Subscribe(result => Animator.Play(result ? "on" : "off"))
                .RegisterTo(this.destroyCancellationToken);
            vm.GestureName
                .Subscribe(value => GestureNameText.text = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.Icon1Image
                .Subscribe(value => Icon1Image.sprite = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.Icon2Image
                .Subscribe(value => Icon2Image.sprite = value)
                .RegisterTo(this.destroyCancellationToken);
        }


    }


}