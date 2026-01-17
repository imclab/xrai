using System;
using UnityEngine.Serialization;

namespace RayFire
{
	[Serializable]
	public class RFFragmentProperties
	{
		[FormerlySerializedAs ("removeCollinear")] public bool           rem; 
		[FormerlySerializedAs ("decompose")]       public bool           dec; // Decompose
		public                                            SliceType      slc;
		public                                            bool           cmb; // Combine
		public                                            bool           cap; // Input Cap
		public                                            bool           ptr; // Petrify
		[FormerlySerializedAs ("colliderType")] public    RFColliderType col;
		[FormerlySerializedAs ("sizeFilter")]   public    float          szF;
		public                                            bool           l; // Inherit layer
		[FormerlySerializedAs ("layer")] public           int            lay;
		public                                            bool           t; // Inherit tag
		public                                            string         tag;

		/// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
		
		// Constructor
		public RFFragmentProperties()
		{
			InitValues();
		}
		
		// Starting values
		public void InitValues()
		{
			rem = false;
			dec = true;
			slc = SliceType.Hybrid;
			cmb = true;
			cap = true;
			ptr = false;
			col = RFColliderType.Mesh;
			szF = 0;
			l   = true;
			lay = 0;
			t   = true;
			tag = string.Empty;
		}

		// Copy from
		public void CopyFrom (RFFragmentProperties props)
		{
			rem = props.rem;
			dec = props.dec;
			slc = props.slc;
			cmb = props.cmb;
			cap = props.cap;
			ptr = props.ptr;
			col = props.col;
			szF = props.szF;
			l   = props.l;
			lay = props.lay;
			t   = props.t;
			tag = props.tag;
		}
		
		/// /////////////////////////////////////////////////////////
		/// Layer & Tag
		/// /////////////////////////////////////////////////////////
        
		// Get layer for fragments
		public static int GetLayer (RayfireRigid scr)
		{
			// Inherit layer
			if (scr.mshDemol.prp.l == true)
				return scr.gameObject.layer;

			// Get custom layer
			return scr.mshDemol.prp.lay;
		}
        
		// Set layer for fragments
		public static void SetLayer (RayfireRigid scr)
		{
			if (scr.mshDemol.prp.l == false)
			{
				int baseLayer = GetLayer(scr);
				for (int i = 0; i < scr.fragments.Count; i++)
					scr.fragments[i].gameObject.layer = baseLayer;
				
				if (scr.objTp == ObjectType.ConnectedCluster)
				{
					for (int i = 0; i < scr.clsDemol.cluster.shards.Count; i++)
						scr.clsDemol.cluster.shards[i].tm.gameObject.layer = baseLayer;
				}
			}
		}
		
		// Get tag for fragments
		public static string GetTag (RayfireRigid scr)
		{
			// Inherit tag
			if (scr.mshDemol.prp.t == true)
				return scr.gameObject.tag;
            
			// Set tag. Not defined -> Untagged
			if (scr.mshDemol.prp.tag.Length == 0)
				return "Untagged";
			
			// Set tag.
			return scr.mshDemol.prp.tag;
		}
		
		// Set tag for fragments
		public static void SetTag (RayfireRigid scr)
		{
			if (scr.mshDemol.prp.t == false)
			{
				string baseTag = GetTag(scr);
				for (int i = 0; i < scr.fragments.Count; i++)
					scr.fragments[i].gameObject.tag = baseTag;
				
				if (scr.objTp == ObjectType.ConnectedCluster)
				{
					for (int i = 0; i < scr.clsDemol.cluster.shards.Count; i++)
						scr.clsDemol.cluster.shards[i].tm.gameObject.tag = baseTag;
				}
			}
		}

		
	}
}