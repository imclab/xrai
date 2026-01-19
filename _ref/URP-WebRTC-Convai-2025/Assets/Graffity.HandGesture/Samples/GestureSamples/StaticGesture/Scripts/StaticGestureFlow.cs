using R3;
using UnityEngine;


namespace Graffity.HandGesture.Sample.StaticGesture
{


    /// <summary>
    /// StaticGesture Sample flow
    /// </summary>
    public class StaticGestureFlow : MonoBehaviour
    {


        [field: SerializeField]
        GestureManager GestureManager { get; set; }
        [field: SerializeField]
        GesturesInfoView GesturesInfoView_Left { get; set; }
        [field: SerializeField]
        GesturesInfoModel GesturesInfoModel_Left { get; set; } = new();
        [field: SerializeField]
        GesturesInfoView GesturesInfoView_Right { get; set; }
        [field: SerializeField]
        GesturesInfoModel GesturesInfoModel_Right { get; set; } = new();
        [field: SerializeField]
        GesturesInfoView GesturesInfoView_Both { get; set; }
        [field: SerializeField]
        GesturesInfoModel GesturesInfoModel_Both { get; set; } = new();


        GesturesInfoViewModel GesturesInfoViewModel_Left { get; set; }
        GesturesInfoViewModel GesturesInfoViewModel_Right { get; set; }
        GesturesInfoViewModel GesturesInfoViewModel_Both { get; set; }


        void Start()
        {
            GesturesInfoModel_Left.Initialize();
            GesturesInfoModel_Left.RegisterTo(this.destroyCancellationToken);
            GesturesInfoModel_Right.Initialize();
            GesturesInfoModel_Right.RegisterTo(this.destroyCancellationToken);
            GesturesInfoModel_Both.Initialize();
            GesturesInfoModel_Both.RegisterTo(this.destroyCancellationToken);

            GesturesInfoViewModel_Left = new();
            GesturesInfoViewModel_Left.RegisterTo(this.destroyCancellationToken);
            GesturesInfoViewModel_Left.Bind(GestureManager, GesturesInfoModel_Left);
            GesturesInfoView_Left.Bind(GesturesInfoViewModel_Left);

            GesturesInfoViewModel_Right = new();
            GesturesInfoViewModel_Right.RegisterTo(this.destroyCancellationToken);
            GesturesInfoViewModel_Right.Bind(GestureManager, GesturesInfoModel_Right);
            GesturesInfoView_Right.Bind(GesturesInfoViewModel_Right);

            GesturesInfoViewModel_Both = new();
            GesturesInfoViewModel_Both.RegisterTo(this.destroyCancellationToken);
            GesturesInfoViewModel_Both.Bind(GestureManager, GesturesInfoModel_Both);
            GesturesInfoView_Both.Bind(GesturesInfoViewModel_Both);
        }


    }


}