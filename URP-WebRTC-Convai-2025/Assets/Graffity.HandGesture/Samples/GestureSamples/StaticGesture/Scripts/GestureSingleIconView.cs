using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    public class GestureSingleIconView : MonoBehaviour
    {


        public interface IViewModel : IGestureIconViewModel
        {
            ReadOnlyReactiveProperty<Sprite> IconImage { get; }
        }


        [field: SerializeField]
        Animator Animator { get; set; } = default;
        [field: SerializeField]
        TextMeshProUGUI GestureNameText { get; set; } = default;
        [field: SerializeField]
        Image IconImage { get; set; } = default;


        public void Bind(IViewModel vm)
        {
            Animator.Play(vm.IsChangeResult.CurrentValue ? "on" : "off");
            GestureNameText.text = vm.GestureName.CurrentValue;
            IconImage.sprite = vm.IconImage.CurrentValue;

            vm.IsChangeResult
                .Subscribe(result => Animator.Play(result ? "on" : "off"))
                .RegisterTo(this.destroyCancellationToken);
            vm.GestureName
                .Subscribe(value => GestureNameText.text = value)
                .RegisterTo(this.destroyCancellationToken);
            vm.IconImage
                .Subscribe(value => IconImage.sprite = value)
                .RegisterTo(this.destroyCancellationToken);
        }


    }


}