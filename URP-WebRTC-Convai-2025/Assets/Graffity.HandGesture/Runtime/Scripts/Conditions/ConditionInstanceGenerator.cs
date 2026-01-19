using System.Collections.Generic;
using System.Linq;


namespace Graffity.HandGesture.Conditions
{


    /// <summary>
    /// Class for converting to ConditionInstance
    /// </summary>
    static public partial class ConditionInstanceGenerator
    {


        static public IEnumerable<IConditionInstance> Generate(IEnumerable<IConditionInstanceCreator> craetorList)
        {
            return craetorList.Select(value => value.CreateInstance());
        }


        static public IEnumerable<IConditionInstance> Generate(GestureAsset asset)
        {
            return Generate(asset.ConditionAssetList);
        }


    }


}