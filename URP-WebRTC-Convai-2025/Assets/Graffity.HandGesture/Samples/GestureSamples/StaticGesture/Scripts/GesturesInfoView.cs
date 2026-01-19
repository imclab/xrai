using System.Collections.Generic;
using TMPro;
using UnityEngine;
using R3;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    public class GesturesInfoView : MonoBehaviour
    {


        public interface IViewModel
        {
            ReadOnlyReactiveProperty<string> CurrentGesture { get; }
            IReadOnlyList<IGestureIconViewModel> GestureIconViewModelList { get; }
        }


        [field: SerializeField]
        GameObject GestureSingleIconPrefab { get; set; }
        [field: SerializeField]
        GameObject GestureDoubleIconPrefab { get; set; }
        [field: SerializeField]
        TextMeshProUGUI CurrentGestureText { get; set; } = default;
        [field: SerializeField]
        Transform GestureIconRoot { get; set; }


        public void Bind(IViewModel vm)
        {
            CurrentGestureText.text = vm.CurrentGesture.CurrentValue;
            vm.CurrentGesture
                .Subscribe(value => CurrentGestureText.text = value)
                .RegisterTo(this.destroyCancellationToken);

            // Instantiate GestureIconView and Bind
            foreach (var iconVM in vm.GestureIconViewModelList)
            {
                switch (iconVM)
                {
                    case GestureSingleIconViewModel singleIconVM:
                        var singleIconObject = Instantiate(GestureSingleIconPrefab, GestureIconRoot);
                        var singleIconV = singleIconObject.GetComponent<GestureSingleIconView>();
                        singleIconV.Bind(singleIconVM);
                        break;
                    case GestureDoubleIconViewModel doubleIconVM:
                        var doubleIconObject = Instantiate(GestureDoubleIconPrefab, GestureIconRoot);
                        var doubleIconV = doubleIconObject.GetComponent<GestureDoubleIconView>();
                        doubleIconV.Bind(doubleIconVM);
                        break;
                    default:
                    break;
                }
            }
        }


    }


}