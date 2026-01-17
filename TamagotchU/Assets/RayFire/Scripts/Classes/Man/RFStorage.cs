using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    /// <summary>
    /// Rayfire Man fragment storage class.
    /// </summary>
    public class RFStorage
    {
        public Transform       storageRoot;
        public bool            inProgress;
        float                  rate = 1f;
        public List<Transform> storageRoots;
        public List<Transform> storageFrags;

        // Constructor
        public RFStorage()
        {
            storageRoots = new List<Transform>();
            storageFrags = new List<Transform>();
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Create storage
        public void CreateStorageRoot  (Transform manTm)
        {
            // Already has storage root
            if (storageRoot != null)
                return;
            
            GameObject storageGo = new GameObject ("Storage_Fragments");
            storageRoot          = storageGo.transform;
            storageRoot.position = manTm.transform.position;
            storageRoot.parent   = manTm.transform;
        }

        // Destroy empty storage roots
        public IEnumerator StorageCor()
        {
            WaitForSeconds delay = new WaitForSeconds (rate);
            
            // Pooling loop
            inProgress = true;
            while (inProgress == true)
            {
                // Destroy root without children
                for (int i = storageRoots.Count - 1; i >= 0; i--)
                {
                    // Remove destroyed, reset
                    if (storageRoots[i] == null)
                    {
                        storageRoots.RemoveAt (i);
                        continue;
                    }

                    // 
                    if (storageRoots[i].childCount == 0)
                    {
                        Object.Destroy (storageRoots[i].gameObject);
                        storageRoots.RemoveAt (i);
                    }
                }

                // Wait next frame
                yield return delay;
            }
            inProgress = false;
        }

        // Add new root to storage
        public void RegisterRoot (Transform tm)
        {
            storageRoots.Add (tm);
        }
        
        // Add new fragment to storage
        public void RegisterFrag (Transform tm)
        {
            storageFrags.Add (tm);
        }
        
        // Destroy all storage objects
        public void DestroyAll()
        {
            for (int i = storageRoots.Count - 1; i >= 0; i--)
                if (storageRoots[i] != null)
                    Object.Destroy (storageRoots[i].gameObject);
            storageRoots.Clear();
            
            for (int i = storageFrags.Count - 1; i >= 0; i--)
                if (storageFrags[i] != null)
                    Object.Destroy (storageFrags[i].gameObject);
            storageFrags.Clear();
        }
    }
}
