using System;
using UnityEngine;


namespace Graffity.HandGesture.Attributes
{


	/// <summary>
	/// Attributes that can be set from dropdown for classes inheriting from IConditionAsset
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class ConditionSelectorAttribute : PropertyAttribute
	{


		public enum ConditionType
		{
			Default,
			Sequence
		}


		public ConditionType Type { get; private set; }


		public ConditionSelectorAttribute(ConditionType type = ConditionType.Default)
		{
			Type = type;
		}


	}


}