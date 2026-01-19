using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
// Edit RFShell.cs at line 54 as well in case of change
// https://docs.unity3d.com/2021.3/Documentation/Manual/PlatformDependentCompilation.html

namespace RayFire
{
    public class RFEngine
    {
        public GameObject                       mainRoot;
        public List<Mesh>                       origMeshes;
        public List<Utils.Mesh>                 utilMeshes;
        public List<Renderer>                   renderers;
        public Dictionary<Transform, Transform> transMap;
        public Matrix4x4                        normalMat;
        public Utils.SliceData                  sliceData;
        public Utils.Mesh[][]                   utilFrags;
        public bool[][]                         edgeFlags;
        public List<bool>                       skinFlags;
        public Utils.MeshMaps[]                 origMaps;
        public Utils.MeshMaps[][]               fragMaps;
        
        public Vector3[][] centroids;
        public Matrix4x4[] centMatrices;
        public Matrix4x4[] flatMatrices;
        public Matrix4x4   biasTN;
        public Vector3     biasPos;
        public Vector3     aabbMin;
        public Vector3     aabbMax;
        public Vector3     aabbSize;
        public int         innerSubId;
        public bool        transformed;
        
        // TODO array of meshfilters

        const float biasMult = 3f;
        
        /// /////////////////////////////////
        /// Constructor
        /// /////////////////////////////////

        RFEngine(int size)
        {
            origMeshes = new List<Mesh>(size);
            utilMeshes = new List<Utils.Mesh>(size);
            renderers  = new List<Renderer>(size);
            skinFlags  = new List<bool> (size);
            transMap   = new Dictionary<Transform, Transform> (size);
        }
        
        /// /////////////////////////////////
        /// Shatter
        /// /////////////////////////////////
        
        /// <summary>
        /// RFEngine static method to fragment object with Rayfire Shatter component
        /// </summary>
        public static void FragmentShatter(RayfireShatter sh)
        {
            // Set engine data
            sh.engineData = GetEngine (sh.transform, false, sh.advanced.petrify);
            
            // Set global bounds by all renderers TODO if Custom/Hex frag type
            sh.bound = GetRendererBounds (sh.engineData.renderers);
            
            // Setup fragmentation for shatter
            SetupShatterFragmentation(sh);
            
            // Get Bias Pos
            sh.engineData.biasPos = sh.advanced.centerBias != null
                ? sh.engineData.normalMat.MultiplyPoint (sh.engineData.biasTN.GetPosition())
                : new Vector3 (0, 0, 0);
            
            // Chose Fragmentation Type and Set Parameters
            SetShatterFragType (sh, sh.engineData.sliceData, sh.engineData, sh.engineData.normalMat, sh.engineData.biasTN, sh.engineData.biasPos);
            
            // Perform slicing ops
            ProcessShatterFragmentation(sh);
            
            // Create Unity Mesh objects
            List<Transform> fragments = CreateShatterFragments(sh); // TODO add skinnedMeshesOrigScale
            
            // Create shatter batch. Should be after SetCenterMatrices method
            RFShatterBatch.CreateBatch (sh, fragments);
            
            // Sync animation TODO test with animated meshes
            if (sh.engineData.HasSkin == true)
                sh.engineData.SyncAnimation(sh.transform);
            
            // Destroy shatter on instance hierarchy
            if (sh.engineData.mainRoot != null)
                Object.DestroyImmediate(sh.engineData.mainRoot.GetComponent<RayfireShatter>());   
            
            // Nullify
            sh.engineData = null;
        }
        
         // STEP 2. Shatter only. Set properties
        static void SetupShatterFragmentation(RayfireShatter sh)
        {
            // Get separate property. Should be enabled fro decompose
            bool separate = sh.advanced.separate || sh.type == FragType.Decompose;

            // Get precap property
            bool precap = sh.advanced.inpCap;

            // Set slice type
            SliceType sliceType = sh.advanced.sliceType;
            
            // Adjusted axis scale for Splinter and Slabs frag types
            Vector3 axisScale = GetAxisScale(sh);

            // Get aabb transform
            Transform cutAABB = sh.advanced.aabbEnable == true && sh.advanced.aabbObject != null
                ? sh.advanced.aabbObject
                : null;

            // Get AABB separate state
            bool aabbSeparate = sh.advanced.aabbSeparate;
            
            // Get Bias TN
            sh.engineData.biasTN = sh.advanced.CanUseCenter == true
                ? sh.advanced.centerBias.localToWorldMatrix
                : Matrix4x4.identity;
            
            // Setup fragmentation properties
            SetupFragmentation (sh.engineData, sh.transform, separate, precap, sliceType, axisScale, cutAABB, aabbSeparate);
        }
        
        // STEP 3. Perform slicing ops. Shatter only
        static void ProcessShatterFragmentation(RayfireShatter sh) 
        {
            // Set fragmentation type
            FragType fragType  = sh.type;
            bool     combine   = sh.advanced.combine;
            int      elements  = sh.advanced.element;
            int      clsCount  = sh.clusters.Count;
            int      clsLayers = sh.clusters.layers;
            int      clsSeed   = sh.clusters.Seed;
            int      faceFlt   = sh.advanced.faceFlt;
            bool     outCap    = sh.advanced.outCap;
            bool     smooth    = sh.advanced.smooth;
            
            // Perform slicing ops
            ProcessFragmentation (sh.engineData, fragType, combine, elements, clsCount, clsLayers, clsSeed, faceFlt);
            
            // STEP 4. Post slicing ops
            PostFragmentation (sh.transform, sh.engineData, sh.material, outCap, smooth);
        }
        
        // STEP 4. Create Unity Mesh objects
        static List<Transform> CreateShatterFragments(RayfireShatter sh)
        {
            // Get hierarchy type
            FragHierarchyType hierarchy = sh.advanced.hierarchy;

            // Get original scale state
            bool origScale = sh.advanced.origScale;
            
            // Get inner filter state
            bool inner = sh.advanced.inner;
            
            // Get planar filter state 
            bool planar = sh.advanced.planar;
            
            // Reset list for skinned scale preview
            sh.skinnedMeshesOrigScale = new List<Tuple<Mesh, Matrix4x4[]>>();
            
            // Set center matrices for different hierarchy types
            CreateHierarchy (sh.engineData, sh.transform, hierarchy, origScale);
            
            // Create Unity Mesh objects
            return CreateShatterFragments(sh.engineData, sh.material.iMat, inner, planar, sh.shell); // TODO add skinnedMeshesOrigScale
        }
        
         // STEP 4.1 Create Unity Mesh objects
        static List<Transform> CreateShatterFragments(RFEngine engine, Material iMat, bool inner, bool planar, RFShell shell)
        {
            // Fragments list
            List<Transform> fragments = new List<Transform>();
            
            // Create fragments 
            for (int i = 0; i < engine.utilFrags.Length; i++)
            {
                // Tag and layer
                int    layer = engine.renderers[i].gameObject.layer;
                string tag   = engine.renderers[i].gameObject.tag;
            
                // Create local fragments root
                Transform groupRoot = engine.transMap[engine.renderers[i].transform];

                // Set tag and layer to root.
                groupRoot.gameObject.layer = layer;
                groupRoot.gameObject.tag   = tag;
                
                if (engine.skinFlags[i] == false)
                    CreateShatterMeshFragments (engine, groupRoot, i, iMat, inner, planar, shell, ref fragments, layer, tag);
                else
                    CreateShatterSkinFragments (engine, groupRoot, i, iMat, inner, planar, ref fragments, layer, tag);
            }

            return fragments;
        }
        
        // Create Mesh Frags
        static void CreateShatterMeshFragments(RFEngine engine, Transform groupRoot, int ind, Material iMat, bool inner, bool planar, RFShell shell, ref List<Transform> fragments, int layer, string tag)
        {
            // Get name for group frags
            string groupName = engine.GetGroupName (ind) + "_sh_";
            
            // Create fragments
            for (int j = 0; j < engine.utilFrags[ind].Length; j++)
            {
                // Skip inner fragments by inner filter
                if (inner == true && engine.utilFrags[ind][j].GetFragLocation() == Utils.Mesh.FragmentLocation.Inner)
                    continue;

                // Create fragment mesh
                Mesh fragUnityMesh = CreateMesh (engine.utilFrags[ind][j], engine.fragMaps[ind][j]);

                // Skip planar fragments
                if (planar == true && RFShatterAdvanced.IsCoplanar (fragUnityMesh, RFShatterAdvanced.planarThreshold) == true)
                    continue;

                // Create fragment object
                GameObject fragGo = CreateFragmentObj (groupRoot, fragUnityMesh, groupName + (j + 1), layer, tag);
                
                // Add shell
                if (shell.enable == true)
                    fragUnityMesh = RFShell.AddShell (engine.utilFrags[ind][j], fragUnityMesh, shell.bridge, shell.submesh, shell.thickness);

                // Add meshfilter
                MeshFilter mf = fragGo.AddComponent<MeshFilter>();
                mf.sharedMesh = fragUnityMesh;

                // Add renderer and materials
                MeshRenderer mr = fragGo.AddComponent<MeshRenderer>();
                mr.sharedMaterials = GetCorrectMaterials (engine.GetMaterials (ind), engine.utilFrags[ind][j], iMat);

                // Set local fragment position
                SetLocalPosition (engine, fragGo.transform, ind, j);
                
                // Collect batch data
                fragments.Add (fragGo.transform);
            }
        }
        
        // Create Skin Frags 
        static void CreateShatterSkinFragments(RFEngine engine, Transform groupRoot, int ind, Material iMat, bool inner, bool planar, ref List<Transform> fragments, int layer, string tag)
        {
            // Get name for group frags
            string groupName = engine.GetGroupName (ind) + "_sh_";
            
            // Get bones transforms
            Transform[] bones = engine.GetBonesTransform(ind);
            
            // Create fragments
            for (int j = 0; j < engine.utilFrags[ind].Length; j++)
            {
                // Skip inner fragments by inner filter
                if (inner == true && engine.utilFrags[ind][j].GetFragLocation() == Utils.Mesh.FragmentLocation.Inner)
                    continue;
                
                // Create fragment mesh
                Mesh fragUnityMesh = CreateMesh (engine.utilFrags[ind][j], engine.fragMaps[ind][j]);

                // Skip planar fragments
                if (planar == true && RFShatterAdvanced.IsCoplanar (fragUnityMesh, RFShatterAdvanced.planarThreshold) == true)
                    continue;
                
                // Create fragment object
                GameObject fragGo = CreateFragmentObj (groupRoot, fragUnityMesh, groupName + (j + 1), layer, tag);
                
                // Set Skin data 
                SetSkinData (engine, fragGo, fragUnityMesh, bones, ind, j, iMat);
                
                // TODO add absolute and relative size filters
                
                // Collect batch data
                fragments.Add (fragGo.transform);
            }
        }

        /// /////////////////////////////////
        /// Rigid
        /// /////////////////////////////////

        // Runtime rigid fragment caching
        public static void CacheRuntimeV2(RayfireRigid rg)
        {
            // Reuse existing cache
            if (rg.reset.action == RFReset.PostDemolitionType.DeactivateToReset && 
                rg.reset.mesh == RFReset.MeshResetType.ReuseFragmentMeshes)
                if (rg.mshDemol.HasEngineAndMeshes == true)
                    return;
            
            // Skin demolition requires all child renderers. mesh demolition only renderer on object
            bool oneRenderer = rg.objTp != ObjectType.SkinnedMesh;

            // Petrify
            bool petrify = rg.mshDemol.prp.ptr;
            
            // Set engine data. Input mesh stage.
            rg.mshDemol.engine = GetEngine (rg.transform, oneRenderer, petrify);

            // Set global bounds by all renderers TODO use already calculated
            rg.lim.bound = GetRendererBounds (rg.mshDemol.engine.renderers);
            
            // STEP 2. Rigid only. Set properties
            SetupRigidFragmentation(rg.mshDemol.engine, rg);
            
            // Get Bias Pos TODO consider Use SHatter case
            rg.mshDemol.engine.biasPos = rg.mshDemol.bias > 0
                ? rg.mshDemol.engine.normalMat.MultiplyPoint (rg.lim.contactVector3)
                : new Vector3 (0, 0, 0);
            
            // Chose Fragmentation Type and Set Parameters
            SetRigidFragType(rg);
            
            // Perform slicing ops
            ProcessRigidFragmentation(rg.mshDemol.engine, rg);
        }

        // STEP 2. Rigid only. Set properties
        static void SetupRigidFragmentation(RFEngine engine, RayfireRigid rg)
        {
            // Get Rigid default properties
            bool      separate  = true;
            bool      inpCap    = rg.mshDemol.prp.cap;
            SliceType sliceType = rg.mshDemol.prp.slc;
            Vector3   axisScale = Vector3.one;
            
            // Set Shatter in case of use
            if (rg.mshDemol.UseShatter == true)
            {
                separate  = rg.mshDemol.sht.advanced.separate || rg.mshDemol.sht.type == FragType.Decompose;
                inpCap    = rg.mshDemol.sht.advanced.inpCap;
                sliceType = rg.mshDemol.sht.advanced.sliceType;
                axisScale = GetAxisScale(rg.mshDemol.sht);
            }
            
            // Rigid do not use this feature even via Shatter
            Transform cutAABB      = null;
            bool      aabbSeparate = false;
            
            // Get Bias Always Identity by defaultNt,z
            engine.biasTN = Matrix4x4.identity;

            // Setup fragmentation properties
            SetupFragmentation (engine, rg.transform, separate, inpCap, sliceType, axisScale, cutAABB, aabbSeparate);
        }
        
        // STEP 3. Perform slicing ops. Shatter only
        static void ProcessRigidFragmentation(RFEngine engine, RayfireRigid rg) 
        {
            // Set fragmentation type
            FragType fragType  = FragType.Voronoi;
            bool     combine   = rg.mshDemol.prp.cmb;
            int      elements  = 3;
            int      clsCount  = 0;
            int      clsLayers = 0;
            int      clsSeed   = 0;
            int      faceFlt   = 0;
            bool     outCap    = false;
            bool     smooth    = false;

            // Set Shatter in case of use
            if (rg.mshDemol.UseShatter == true)
            {
                fragType  = rg.mshDemol.sht.type;
                combine   = rg.mshDemol.sht.advanced.combine;
                elements  = rg.mshDemol.sht.advanced.element;
                clsCount  = rg.mshDemol.sht.clusters.Count;
                clsLayers = rg.mshDemol.sht.clusters.layers;
                clsSeed   = rg.mshDemol.sht.clusters.Seed;
                faceFlt   = rg.mshDemol.sht.advanced.faceFlt;
                outCap    = rg.mshDemol.sht.advanced.outCap;
                smooth    = rg.mshDemol.sht.advanced.smooth;
            }
            
            // TODO Runtime caching
            if (rg.mshDemol.ch.tp == CachingType.Disabled)
            {
                
            }

            // Perform slicing ops
            ProcessFragmentation (engine, fragType, combine, elements, clsCount, clsLayers, clsSeed, faceFlt);
            
            // STEP 4. Post slicing ops
            PostFragmentation (rg.transform, engine, rg.materials, outCap, smooth);
        }
        
        // STEP 4. Create Unity Mesh objects
        public static List<Transform> CreateRigidFragments(RFEngine engine, RayfireRigid rg)
        {
            // Get hierarchy type. Always Flat for Rigid
            FragHierarchyType hierarchy = FragHierarchyType.Flat;
            
            // Rigid always fragments with identity scale except skinned mesh
            bool origScale = engine.HasSkin;
            
            // Set center matrices for different hierarchy types and create roots
            CreateHierarchy (engine, rg.transform, hierarchy, origScale);
            
            // Get inner material
            Material iMat = rg.materials.iMat;
            
            // Get planar filter
            bool planar = rg.physics.pc;
            
            // Get shell properties. Disabled by default.
            RFShell shell = new RFShell();
            
            // Create Unity Mesh objects
            List<Transform> fragsTms = CreateRigidFragments(engine, rg, iMat, planar, shell);
            
            // Sync animation TODO test with animated meshes
            if (rg.mshDemol.engine.HasSkin == true)
                rg.mshDemol.engine.SyncAnimation(rg.transform);
            
            // Set main fragments root after hierarchy created
            rg.rtC = rg.mshDemol.engine.mainRoot.transform;
            
            // Set root to manager
            RayfireMan.SetFragmentRootParent (rg.rtC);
            
            // Ignore neib collisions
            RFPhysic.SetIgnoreColliders (rg.physics, rg.fragments);

            return fragsTms;
        }
        
        // STEP 4. Create Unity Mesh objects
        static List<Transform> CreateRigidFragments(RFEngine engine, RayfireRigid rg, Material iMat, bool planar, RFShell shell)
        {
            // Fragments list
            List<Transform> fragments = new List<Transform>();
            
            // Create RayFire manager if not created
            RayfireMan.RayFireManInit();

            // Set fragments list
            rg.fragments = new List<RayfireRigid>();
            
            // Create fragments 
            for (int i = 0; i < engine.utilFrags.Length; i++)
            {
                // Tag and layer
                int layer = engine.renderers[i].gameObject.layer;
                if (rg.mshDemol.prp.l == false)
                    layer = rg.mshDemol.prp.lay;
                string tag = engine.renderers[i].gameObject.tag;
                if (rg.mshDemol.prp.t == false)
                    tag = rg.mshDemol.prp.tag;
            
                // Get local fragments root
                Transform groupRoot = engine.transMap[engine.renderers[i].transform];

                // Set tag and layer to root.
                groupRoot.gameObject.layer = layer;
                groupRoot.gameObject.tag   = tag;
                
                // Create fragments
                if (engine.skinFlags[i] == false)
                    CreateRigidMeshFragments (engine, rg, groupRoot, i, iMat, planar, shell, ref fragments, layer, tag);
                else // TODO fix as Mesh method
                    CreateRigidSkinFragments (engine, rg, groupRoot, i, iMat, planar, ref fragments, layer, tag);
            }
            
            // Save pivots for reset
            if (rg.reset.fragments == RFReset.FragmentsResetType.Reuse)
            {
                rg.pivots = new Vector3[rg.fragments.Count];
                for (int i = 0; i < rg.fragments.Count; i++)
                    rg.pivots[i] = rg.fragments[i].tsf.position - rg.tsf.position;
            }

            return fragments;
        }
        
        // Create Mesh Frags
        static void CreateRigidMeshFragments(RFEngine engine, RayfireRigid rg, Transform groupRoot, int ind, Material iMat, bool planar, RFShell shell, ref List<Transform> fragments, int layer, string tag)
        {
            // Get name for group frags
            string groupName = engine.GetGroupName (ind) + "_fr_";
            
            // Create fragments
            for (int j = 0; j < engine.utilFrags[ind].Length; j++)
            {
                // Create fragment mesh
                Mesh fragUnityMesh = CreateMesh (engine.utilFrags[ind][j], engine.fragMaps[ind][j]);

                // Skip planar fragments
                if (planar == true && RFShatterAdvanced.IsCoplanar (fragUnityMesh, RFShatterAdvanced.planarThreshold) == true)
                    continue;
                
                // Get object from pool or create
                RayfireRigid rfScr = RayfireMan.inst.fragments.rgInst == null
                    ? RayfireMan.inst.fragments.CreateRigidInstance()
                    : RayfireMan.inst.fragments.GetPoolObject();
                
                // Set tag and layer to fragment
                rfScr.name             = groupName + (j + 1);
                rfScr.gameObject.layer = layer;
                rfScr.gameObject.tag   = tag;
                rfScr.rtP              = groupRoot;

                fragUnityMesh.name = rfScr.name;
                rfScr.tsf.SetParent (groupRoot, false);
                
                // Copy properties from parent to fragment node
                rg.CopyPropertiesTo (rfScr);
                
                // Set custom fragment simulation type if not inherited
                RFPhysic.SetFragmentSimulationType (rfScr, rg.simTp);
                
                // Copy particles
                RFPoolingParticles.CopyParticlesRigid (rg, rfScr);
                
                // Set collider
                RFPhysic.SetFragmentCollider (rfScr, fragUnityMesh);
                
                // TODO Copy Renderer properties
                // CopyRenderer (scr, rfScr.mRnd, scr.meshes[i].bounds);
                
                // Add shell to mesh
                if (shell.enable == true)
                    fragUnityMesh = RFShell.AddShell (engine.utilFrags[ind][j], fragUnityMesh, shell.bridge, shell.submesh, shell.thickness);

                // Set mesh
                rfScr.mFlt.sharedMesh = fragUnityMesh;

                // Set materials
                rfScr.mRnd.sharedMaterials = GetCorrectMaterials (engine.GetMaterials (ind), engine.utilFrags[ind][j], iMat);

                // Set local fragment position
                SetLocalPosition (engine, rfScr.tsf, ind, j);
                
                // Turn on
                rfScr.gameObject.SetActive (true);
                
                // Set limitations properties
                RFDemolitionMesh.SetLimitationProps(rfScr, rg.lim.currentDepth);
                
                // Set mass by mass value accordingly to parent
                if (rfScr.physics.mb == MassType.MassProperty)
                    RFPhysic.SetMassByParent (rfScr.physics, rfScr.lim.bboxSize, rg.physics.ms, rg.lim.bboxSize);
                
                // Collect rigid list
                rg.fragments.Add (rfScr);
                
                // Collect batch data
                fragments.Add (rfScr.tsf);
            }
        }
        
        // Create Skin Frags 
        static void CreateRigidSkinFragments(RFEngine engine, RayfireRigid rg, Transform groupRoot, int ind, Material iMat, bool planar, ref List<Transform> fragments, int layer, string tag)
        {
            // Get name for group frags
            string groupName = engine.GetGroupName (ind) + "_fr_";
            
            // Get bones transforms
            Transform[] bones = engine.GetBonesTransform(ind);
            
            // Create fragments
            for (int j = 0; j < engine.utilFrags[ind].Length; j++)
            {
                // Create fragment mesh
                Mesh fragUnityMesh = CreateMesh (engine.utilFrags[ind][j], engine.fragMaps[ind][j]);

                // Skip planar fragments
                if (planar == true && RFShatterAdvanced.IsCoplanar (fragUnityMesh, RFShatterAdvanced.planarThreshold) == true)
                    continue;
                
                // Create fragment object
                GameObject fragGo = CreateFragmentObj (groupRoot, fragUnityMesh, groupName + (j + 1), layer, tag);
                
                // Set Skin data 
                SetSkinData (engine, fragGo, fragUnityMesh, bones, ind, j, iMat);
                
                // TODO add absolute and relative size filters
                
                // Collect batch data
                fragments.Add (fragGo.transform);
            }
        }
        
        /// /////////////////////////////////
        /// Common Steps
        /// /////////////////////////////////

        // STEP 1. Create engine. Prepare objects meshes for fragmentation.
        static RFEngine GetEngine(Transform tm, bool oneRenderer, bool petrify)
        {
            if (oneRenderer == true)
                return GetEngine (tm.GetComponent<Renderer>(), petrify);
            return GetEngine (tm, petrify);
        }
        
        // Create engine with all children renderers
        static RFEngine GetEngine (Transform tm, bool petrify)
        {
            // Get all renderers
            Renderer[] allRenderers = tm.GetComponentsInChildren<Renderer>();
            if (allRenderers == null)
                return null;
            
            // Create engine
            RFEngine engine = new RFEngine (allRenderers.Length);
            
            // Collect mesh data for renderers 
            for (int i = 0; i < allRenderers.Length; ++i)
                engine.AddRendererMesh (allRenderers[i], petrify);

            return engine;
        }
        
        // Create engine with single renderer
        static RFEngine GetEngine (Renderer rend, bool petrify)
        {
            if (rend == null)
                return null;
            
            RFEngine engine = new RFEngine (1);
            engine.AddRendererMesh (rend, petrify);
            return engine;
        }
        
        // Add renderer to engine and collect meshes
        void AddRendererMesh(Renderer renderer, bool petrify)
        {
            // Get unity mesh
            Mesh unityMesh = null;
            bool skinState = false;
            if (renderer.GetType() == typeof(MeshRenderer))
                unityMesh = renderer.gameObject.GetComponent<MeshFilter>().sharedMesh;
            else
            {
                if (petrify == false)
                {
                    skinState = true;
                    unityMesh = ((SkinnedMeshRenderer)renderer).sharedMesh;
                }
                else
                {
                    unityMesh = new Mesh();
                    ((SkinnedMeshRenderer)renderer).BakeMesh (unityMesh);
                }
            }

            // Skip mesh
            if (unityMesh == null || unityMesh.vertexCount <= 2)
            {
                Debug.Log (renderer.name + " has no mesh");
                return;
            }
            
            // Collect
            origMeshes.Add (unityMesh);
            utilMeshes.Add (new Utils.Mesh (unityMesh));
            renderers.Add (renderer);
            skinFlags.Add (skinState);
        }
        
        // STEP 2. Setup meshes by their actual tm, set properties, fragment, compute maps
        static void SetupFragmentation(RFEngine engine, Transform tm, bool separate, bool inpCap, SliceType sliceType, Vector3 axisScale, Transform cutAABB, bool aabbSeparate)
        {
            // Bake World Transform & get RFMeshes Array
            engine.BakedWorldTransform();
            
            // Normalize meshes Verts to range 0.0f - 1.0f
            engine.normalMat = Utils.Mesh.GetNormMat (engine.utilFrags, Matrix4x4.TRS (tm.position, tm.rotation * engine.biasTN.rotation, new Vector3 (1, 1, 1)).inverse);

            // Get min and max
            Utils.Mesh.Transform (engine.utilFrags, engine.normalMat, out engine.aabbMin, out engine.aabbMax);

            // Separate not connected elements
            if (separate == true)
                engine.utilFrags = Utils.Mesh.Separate (engine.utilFrags, true);

            // PreCap holes on every element and set not capped open edges array
            engine.edgeFlags = new bool[engine.utilFrags.Length][];
            Utils.Mesh.CheckOpenEdges (engine.utilFrags, engine.edgeFlags, inpCap);

            // Surface for cut
            SetSliceType (sliceType, engine.utilFrags, engine.edgeFlags);
            
            // Get cutAabb matrix
            Matrix4x4 cutAABBMat = cutAABB != null
                ? engine.normalMat * cutAABB.localToWorldMatrix
                : Matrix4x4.zero;

            // Prepare Slice Data Parameters
            Vector3 aabbCentroid = (engine.aabbMin + engine.aabbMax) * 0.5f;
            engine.aabbSize = engine.aabbMax - engine.aabbMin;

            // Create Slice Data  
            engine.sliceData = new Utils.SliceData();

            // Set fragmentation bounding box
            engine.sliceData.SetAABB (engine.utilFrags, axisScale, aabbCentroid, engine.aabbSize, cutAABBMat, aabbSeparate);
        }

        // STEP 3. Perform slicing ops
        static void ProcessFragmentation(RFEngine engine,FragType fragType, bool combine, int element, int clsCount, int clsLayers, int clsSeed, int faceFlt) 
        {
            // Custom cell fragmentation state. 100% by default. Can be used for partial per frame fragmentation. 
            for (int i = 0; i < engine.sliceData.GetNumCells(); i++)
                engine.sliceData.Enable (i, true);

            // Fragment for all types except Decompose type
            if (fragType != FragType.Decompose)
                engine.utilFrags = Utils.Mesh.Slice (engine.sliceData, combine, engine.edgeFlags, element * 0.01f, faceFlt, clsCount, clsLayers, clsSeed);
        }
        
        // STEP 4. Post slicing ops
        static void PostFragmentation(Transform mainRoot, RFEngine engine, RFSurface material, bool outCap, bool smooth) 
        {
            // OutCap holes on every fragment and set not capped open edges array
            if (outCap == true)
            {
                engine.edgeFlags = new bool[engine.utilFrags.Length][];
                Utils.Mesh.CheckOpenEdges (engine.utilFrags, engine.edgeFlags, true);
            }

            // Get inner sub id TODO add support for other renderers, not only first renderer materials
            engine.innerSubId = GetInnerSubId(material.iMat, engine.GetMaterials(0));
            
            // Build SubMeshes
            Utils.Mesh.BuildSubMeshes(engine.utilFrags, engine.origMeshes, engine.innerSubId);

            // Undo Normalize
            Utils.Mesh.Transform(engine.utilFrags, engine.normalMat.inverse);
            
            // Get original mesh maps
            SetOriginalMaps(engine);
            
            // Compute UV. IMPORTANT! before UnBakeWorldTransform
            ComputeInnerUV(engine, mainRoot.localToWorldMatrix, material.MappingScale, material.UvRegionMin, material.UvRegionMax);
            
            // Undo World Transform
            engine.UnBakeWorldTransform();
            
            // Restore Maps
            ComputeMaps (engine, material.cC, smooth);
            
            // Centerize
            engine.centroids = Utils.Mesh.Centerize(engine.utilFrags);
        }

        /// /////////////////////////////////
        /// Fragmentation types
        /// /////////////////////////////////
        
        // Set Fragmentation Type by Shatter
        static void SetShatterFragType(RayfireShatter sh, Utils.SliceData sd, RFEngine engine, Matrix4x4 normalMat, Matrix4x4 biasTN, Vector3 biasPos)
        {
            switch (sh.type)
            {
                case FragType.Voronoi:
                {
                    sd.GenRandomPoints(sh.voronoi.Amount, sh.advanced.Seed);
                    if (sh.advanced.CanUseCenter == true)
                        sd.ApplyCenterBias(biasPos, sh.voronoi.centerBias * biasMult);
                    sd.BuildCells();
                    break;
                }
                case FragType.Splinters:
                {
                    sd.GenRandomPoints(sh.splinters.Amount, sh.advanced.Seed);
                    if (sh.advanced.CanUseCenter == true)
                        sd.ApplyCenterBias(biasPos, sh.splinters.centerBias * biasMult);
                    sd.BuildCells();
                    break;
                }
                case FragType.Slabs:
                {
                    sd.GenRandomPoints(sh.slabs.Amount, sh.advanced.Seed);
                    if (sh.advanced.CanUseCenter == true)
                        sd.ApplyCenterBias(biasPos, sh.slabs.centerBias * biasMult);
                    sd.BuildCells();
                    break;
                }
                case FragType.Radial:
                {
                    Vector3 aabbAbsSize      = normalMat.inverse *  engine.aabbMax;
                    float   radiusExtraScale = Mathf.Max(aabbAbsSize.x, aabbAbsSize.y, aabbAbsSize.z);
                    sd.GenRadialPoints(
                        sh.advanced.Seed, 
                        sh.radial.rays, 
                        sh.radial.rings, 
                        sh.radial.radius / (radiusExtraScale * sh.radial.rings * 2f),
                        sh.radial.divergence * 2.0f, 
                        sh.radial.twist / 90.0f, 
                        (int)sh.radial.centerAxis, 
                        biasPos, 
                        sh.radial.focus * 0.01f, 
                        sh.radial.randomRings * 0.01f,
                        sh.radial.randomRays * 0.01f);
                    sd.BuildCells();
                    break;
                }
                case FragType.Hexagon:
                {
                    List<Vector3> customPnt        = RFHexagon.GetHexPointCLoudV2 (sh.hexagon, sh.CenterPos, sh.CenterDir, sh.bound);
                    List<Vector3> customPointsList = new List<Vector3>();
                    for(int i = 0; i < customPnt.Count; i++)
                        customPointsList.Add(normalMat.MultiplyPoint(biasTN * customPnt[i]));
                    sd.SetCustomPoints(customPointsList);
                    float centerBias = 0;
                    sd.ApplyCenterBias(biasPos, centerBias);
                    sd.BuildCells();
                    break;
                }
                case FragType.Custom:
                {
                    List<Vector3> customPnt        = RFCustom.GetCustomPointCLoud (sh.custom, sh.transform, sh.advanced.Seed, sh.bound);
                    List<Vector3> customPointsList = new List<Vector3>();
                    for(int i = 0; i < customPnt.Count; i++)
                        customPointsList.Add(normalMat.MultiplyPoint(biasTN * customPnt[i]));
                    sd.SetCustomPoints(customPointsList);
                    float centerBias = 0;
                    sd.ApplyCenterBias(biasPos, centerBias);
                    sd.BuildCells();
                    break;
                }
                case FragType.Slices:
                {
                    // Set slice data by transforms. Shatter usage. 
                    sd.AddPlanes(sh.slice.sliceList.ToArray(), new Vector3(0, 1, 0), normalMat);
                    break;
                }
                case FragType.Bricks:
                {
                    sd.GenBricks(
                        sh.bricks.Size,
                        sh.bricks.Num, 
                        sh.bricks.SizeVariation, 
                        sh.bricks.SizeOffset, 
                        sh.bricks.SplitState,
                        sh.bricks.SplitPro);
                    break;
                }
                case FragType.Voxels:
                {
                    sd.GenBricks(
                        sh.voxels.Size,
                        sh.bricks.SplitState, // has 0 vector int
                        Vector3.zero, 
                        Vector3.zero, 
                        sh.voxels.SplitState,
                        Vector3.zero);
                    break;
                }
                case FragType.Tets:
                {
                    // TODO
                    Debug.Log ("Tetrahedron fragmentation is not supported yet by V2 Beta engine");
                    break; 
                }
            }
        }
        
        // Set Fragmentation Type by Rigid
        static void SetRigidFragType(RayfireRigid rg)
        {
            // Slice by slice planes
            if (rg.lim.HasSlicePlanes == true)
            {
                SetSliceType (rg, rg.mshDemol.engine.sliceData, rg.mshDemol.engine.normalMat);
            }

            // Set Rigid Demolition frag properties by Shatter 
            else if (rg.mshDemol.UseShatter == true)
            {
                // Disable Center
                rg.mshDemol.sht.advanced.centerSet  = false;
                
                // Set shatter properties
                SetShatterFragType (rg.mshDemol.sht, rg.mshDemol.engine.sliceData, rg.mshDemol.engine, rg.mshDemol.engine.normalMat, rg.mshDemol.engine.biasTN, rg.mshDemol.engine.biasPos);
            }

            // Set default Rigid fragmentation properties
            else
            {
                rg.mshDemol.engine.sliceData.GenRandomPoints (rg.mshDemol.Amount, rg.mshDemol.Seed);
                rg.mshDemol.engine.sliceData.ApplyCenterBias (rg.mshDemol.engine.biasPos, rg.mshDemol.bias * biasMult);
                rg.mshDemol.engine.sliceData.BuildCells();
            }
        }

        // Set Slice type
        static void SetSliceType(RayfireRigid rg, Utils.SliceData sd, Matrix4x4 normalMat)
        {
            // Set slice data by vector arrays.
            Vector3[] pos  = new Vector3[rg.lim.slicePlanes.Count / 2];
            Vector3[] norm = new Vector3[rg.lim.slicePlanes.Count / 2];
            for (int i = 0; i < rg.lim.slicePlanes.Count / 2; i++)
            {
                pos[i]  = rg.lim.slicePlanes[i*2];
                norm[i] = rg.lim.slicePlanes[i*2+1];
            }
            sd.AddPlanes(pos, norm, normalMat);
        }
        
        /// /////////////////////////////////
        /// Other
        /// /////////////////////////////////

        // Surface for cut
        static void SetSliceType(SliceType slice, Utils.Mesh[][] fragMeshes, bool[][] openEdgeFlags)
        {
            if (slice != SliceType.Hybrid)
                for (int i = 0; i < fragMeshes.Length; i++)
                    for (int j = 0; j < fragMeshes[i].Length; j++)
                        openEdgeFlags[i][j] = slice == SliceType.ForcedCut;
        }

        // Adjusted axis scale for Splinter and Slabs frag types
        static Vector3 GetAxisScale(RayfireShatter sh)
        {
            Vector3 axisScale = Vector3.one;
            if (sh.type == FragType.Splinters)
            {
                float stretch = Mathf.Min (1.0f, Mathf.Max (0.005f, Mathf.Pow (1.0f - sh.splinters.strength, 1.5f)));
                if (sh.splinters.axis == AxisType.XRed)
                    axisScale.x = stretch;
                else if (sh.splinters.axis == AxisType.YGreen)
                    axisScale.y = stretch;
                else if (sh.splinters.axis == AxisType.ZBlue)
                    axisScale.z = stretch;
            }
            else if (sh.type == FragType.Slabs)
            {
                float stretch = Mathf.Min (1.0f, Mathf.Max (0.005f, Mathf.Pow (1.0f - sh.slabs.strength, 1.5f)));
                if (sh.slabs.axis == AxisType.XRed)
                    axisScale.y = axisScale.z = stretch;
                else if (sh.slabs.axis == AxisType.YGreen)
                    axisScale.x = axisScale.z = stretch;
                else if (sh.slabs.axis == AxisType.ZBlue)
                    axisScale.x = axisScale.y = stretch;
            }
            return axisScale;
        }
        
        // Create fragment mesh
        static Mesh CreateMesh(Utils.Mesh utilsMesh, Utils.MeshMaps map)
        {
            Mesh fragMesh = new Mesh();
            fragMesh.indexFormat = utilsMesh.GetNumTri() * 3 < ushort.MaxValue 
                ? UnityEngine.Rendering.IndexFormat.UInt16 
                : UnityEngine.Rendering.IndexFormat.UInt32;
            fragMesh.subMeshCount = utilsMesh.GetNumSubMeshes();
            fragMesh.vertices     = utilsMesh.GetVerts();
                    
            // Set triangles
            for (int subMesh = 0; subMesh < utilsMesh.GetNumSubMeshes(); subMesh++)
                fragMesh.SetTriangles(utilsMesh.GetSubTris(subMesh), subMesh);
                    
            // Set UV data
            map.GetMaps(fragMesh, UVType.bytes_8);
            return fragMesh;
        }

        // Create fragment object
        static GameObject CreateFragmentObj(Transform groupRoot, Mesh fragUnityMesh, string name, int layer, string tag)
        {
            // Create fragment object
            GameObject fragGo = new GameObject (name);
            fragUnityMesh.name = fragGo.name;
            fragGo.layer       = layer;
            fragGo.tag         = tag;
            fragGo.transform.SetParent (groupRoot, false);

            return fragGo;
        }
        
        // Get inner sub id in case of inner material usage
        static int GetInnerSubId(Material innerMaterial, Material[] materials)
        {
            // Inner surface should have custom inner material
            if (innerMaterial != null)
            {
                // Object already has Inner material applied to one of the submesh
                if (materials.Contains (innerMaterial) == true)
                    for (int i = 0; i < materials.Length; i++)
                        if (innerMaterial == materials[i])
                            return i;
                
                // Object has no inner material applied
                return -1;
            }
            
            // Apply first material to inner surface
            return 0;
        }

        // Set materials to fragments
        static Material[] GetCorrectMaterials(Material[] materials, Utils.Mesh rfMesh, Material innerMaterial)
        {
            List<Material> correctMaterials = new List<Material>();
            for (int ii = 0; ii < rfMesh.GetNumSubMeshes(); ii++)
            {
                int origSubID = rfMesh.GetSubMeshID(ii);
                if (origSubID >= 0) // if == -1 then its a inner faces new submesh
                    correctMaterials.Add(materials[origSubID]);
                if (origSubID == -1) // if == -1 then its a inner faces new submesh
                    correctMaterials.Add(innerMaterial);
            }
            return correctMaterials.ToArray();
        }

        // Set local fragment position
        static void SetLocalPosition(RFEngine engine, Transform tm, int ind, int j)
        {
            // Set local position based on hierarchy type
            tm.localPosition = engine.centMatrices == null
                ? engine.centroids[ind][j]
                : engine.centMatrices[ind].MultiplyPoint (engine.centroids[ind][j]);

            // Flat hierarchy 
            if (engine.flatMatrices != null)
            {
                tm.localPosition =  engine.flatMatrices[ind].rotation * tm.localPosition;
                tm.localPosition += engine.flatMatrices[ind].GetPosition();
                tm.localRotation =  engine.flatMatrices[ind].rotation;
            }
        }
        
        // Get materials
        Material[] GetMaterials(int i)
        {
            return renderers[i].sharedMaterials;
        }

        // Get local root name
        string GetGroupName(int i)
        {
            return renderers[i].name;
        }
        
        // Get bounds by renderers
        static Bounds GetRendererBounds(List<Renderer> list)
        {
            // Only one bound
            if (list.Count == 1)
                return list[0].bounds;
            
            // New bound
            Bounds bound = new Bounds();
            
            // Basic bounds min and max values
            float minX = list[0].bounds.min.x;
            float minY = list[0].bounds.min.y;
            float minZ = list[0].bounds.min.z;
            float maxX = list[0].bounds.max.x;
            float maxY = list[0].bounds.max.y;
            float maxZ = list[0].bounds.max.z;
            
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].bounds.min.x < minX) minX = list[i].bounds.min.x;
                if (list[i].bounds.min.y < minY) minY = list[i].bounds.min.y;
                if (list[i].bounds.min.z < minZ) minZ = list[i].bounds.min.z;
                if (list[i].bounds.max.x > maxX) maxX = list[i].bounds.max.x;
                if (list[i].bounds.max.y > maxY) maxY = list[i].bounds.max.y;
                if (list[i].bounds.max.z > maxZ) maxZ = list[i].bounds.max.z;
            }
            
            // Get center
            bound.center = new Vector3((maxX - minX) / 2f, (maxY - minY) / 2f, (maxZ - minZ) / 2f);

            // Get min and max vectors
            bound.min = new Vector3(minX, minY, minZ);
            bound.max = new Vector3(maxX, maxY, maxZ);

            return bound;
        }
        
        /// /////////////////////////////////
        /// Matrix ops
        /// /////////////////////////////////
        
        // Bake
        void BakedWorldTransform()
        {
            utilFrags = new Utils.Mesh[origMeshes.Count][];
            for (int i = 0; i < origMeshes.Count; ++i)
            {
                utilFrags[i]    = new Utils.Mesh[1];
                utilFrags[i][0] = new Utils.Mesh (origMeshes[i]);
                if (skinFlags[i] == false)
                    utilFrags[i][0].Transform (renderers[i].transform.localToWorldMatrix);
                else
                    utilFrags[i][0].TransformByBones (origMeshes[i].bindposes, ((SkinnedMeshRenderer)renderers[i]).bones, false);
            }
        }

        // UnBake
        void UnBakeWorldTransform()
        {
            for (int i = 0; i < utilFrags.Length; ++i)
            {
                for (int j = 0; j < utilFrags[i].Length; ++j)
                {
                    if (skinFlags[i] == false)
                        utilFrags[i][j].Transform (renderers[i].transform.localToWorldMatrix.inverse);
                    else
                        utilFrags[i][j].TransformByBones (origMeshes[i].bindposes, ((SkinnedMeshRenderer)renderers[i]).bones, true);
                }
            }
        }
        
        /// /////////////////////////////////
        /// Shatter Interactive ops
        /// /////////////////////////////////
        
        // Fragment all meshes into own mesh
        public static void InteractiveStart(RayfireShatter sh)
        {
            // Set engine data. Forced multiple renderers and petrify
            sh.engineData = GetEngine (sh.transform, false, true);
            
            // Set global bounds by all renderers 
            sh.bound = GetRendererBounds (sh.engineData.renderers);
            
            // Setup fragmentation for shatter
            SetupShatterFragmentation(sh);
            
            // Get Bias Pos
            sh.engineData.biasPos = sh.advanced.centerBias != null
                ? sh.engineData.normalMat.MultiplyPoint (sh.engineData.biasTN.GetPosition())
                : new Vector3 (0, 0, 0);
            
            // Chose Fragmentation Type and Set Parameters
            SetShatterFragType (sh, sh.engineData.sliceData, sh.engineData, sh.engineData.normalMat, sh.engineData.biasTN, sh.engineData.biasPos);

            // Perform slicing ops
            ProcessShatterFragmentation(sh);

            // TODO Stop if no meshes
            if (sh.engineData.utilFrags == null || sh.engineData.utilFrags.Length == 0)
            {
                return;
            }

            // Create hierarchy for interactive fragment meshes
            CreateInteractiveHierarchy (sh.engineData, sh.transform, sh.advanced.origScale);
            
            // Create interactive object if there is no any
            sh.InteractiveCreate();
            
            
            
            // Transform frag meshes by scale matrix
            Utils.Mesh.Transform(sh.engineData.utilFrags, sh.engineData.centMatrices);
            
            // TODO convert utils meshes to unity meshes, reposition verts relative to root

            // TODO Weld into one mesh and set to interactive object
            // scr.intMf.sharedMesh = RFShatter.WeldMeshes (ref scr.meshes, ref scr.pivots, ref ids, scr.PreviewScale());
            
            
            // Disable own Renderer TODO affect all rendrers
            sh.OriginalRenderer(false);
        }
        
        // Property changed
        public static void InteractiveChange(RayfireShatter scr)
        {
           
        }
        
        // Set center matrices for different hierarchy types
        static void CreateInteractiveHierarchy(RFEngine engine, Transform tm, bool origScale)
        {
            engine.centMatrices = engine.FlatHierarchy(tm, origScale);
            engine.flatMatrices = GetFlatMatrices (engine.renderers, engine.mainRoot.transform);
            
            // Root name
            engine.mainRoot.name += "_frags";
        }
        
        /// /////////////////////////////////////////////////////////
        /// Hierarchy
        /// /////////////////////////////////////////////////////////

        // Set center matrices for different hierarchy types
        static void CreateHierarchy(RFEngine engine, Transform tm, FragHierarchyType hierarchy, bool origScale)
        {
            // Check if has skinned meshes and use Instance hierarchy if has
            if (engine.HasSkin == true)
            {
                origScale = true;
                hierarchy = FragHierarchyType.Instance;
            }

            // Just one object, no need to copy
            if (hierarchy == FragHierarchyType.Copy)
                if (engine.renderers.Count == 1)
                    hierarchy = FragHierarchyType.Flat;
            
            // Set centers based on hierarchy type
            switch (hierarchy)
            {
                case FragHierarchyType.Instance:
                {
                    engine.centMatrices = engine.InstHierarchy(tm, origScale);
                    engine.flatMatrices = null;
                    break;
                }
                case FragHierarchyType.Copy:
                {
                    engine.centMatrices = engine.CopyHierarchy(tm, origScale);
                    engine.flatMatrices = null;
                    break;
                }
                case FragHierarchyType.Flat:
                {
                    engine.centMatrices = engine.FlatHierarchy(tm, origScale);
                    engine.flatMatrices = GetFlatMatrices (engine.renderers, engine.mainRoot.transform);
                    break;
                }
            }
            
            // Transform frag meshes by scale matrix accordingly to hierarchy type. Set flag to avoid transformation at next fragment creation.
            if (engine.transformed == false)
            {
                engine.transformed = true;
                if (engine.centMatrices != null)
                    Utils.Mesh.Transform (engine.utilFrags, engine.centMatrices);
            }
            
            // Root name
            engine.mainRoot.name += "_frags";
            
            // Set tag and layer to root.
            engine.mainRoot.layer = tm.gameObject.layer;
            engine.mainRoot.tag   = tm.gameObject.tag;
        }
        
        // instantiate root structure. Only option for skinned mesh fragmentation
        Matrix4x4[] InstHierarchy(Transform tm, bool origScale)
        {
            mainRoot = Object.Instantiate(tm.gameObject);
            Transform[] oldTms = tm.GetComponentsInChildren<Transform>();
            Transform[] newTms = mainRoot.GetComponentsInChildren<Transform>();

            transMap.Clear();

            // Destroy Renderer and Meshfilters to use as roots
            for (int i = 0; i < oldTms.Length; i++)
            {
                Renderer rnd = newTms[i].gameObject.GetComponent<Renderer>();
                if (rnd != null)
                    Object.DestroyImmediate(rnd);

                MeshFilter mf = newTms[i].gameObject.GetComponent<MeshFilter>();
                if (mf != null)
                    Object.DestroyImmediate(mf);

                transMap.Add(oldTms[i], newTms[i]);
                
                // Set identity scale
                if (origScale == false)
                {
                    newTms[i].localScale = new Vector3(1, 1, 1);
                    newTms[i].position = oldTms[i].position;
                    newTms[i].rotation = oldTms[i].rotation;
                }
            }

            // Fix name
            mainRoot.name = mainRoot.name.Remove (mainRoot.name.Length - 7, 7);

            // Skip if roots keep original scale
            if (origScale == true)
                return null;
            
            // Save scale matrices because of roots identity scale
            return GetCenterMatrices(renderers);
        }
          
        // Create full copy of root structure
        Matrix4x4[] CopyHierarchy(Transform tm, bool origScale)
        {
            transMap.Clear();

            // Create the same root structure
            for (int i = 0; i < renderers.Count; i++)
                GetChain(tm, renderers[i].transform, ref transMap, ref mainRoot);

            // Skip if roots keep original scale
            if (origScale == true)
                return null;

            // Set frag roots scale to [1 1 1] 
            Transform[] oldTms = tm.GetComponentsInChildren<Transform>();
            for (int i = 0; i < oldTms.Length; i++)
            {
                if(transMap.ContainsKey(oldTms[i]))
                {
                    Transform newTm = transMap[oldTms[i]].transform;
                    newTm.localScale = new Vector3(1, 1, 1);
                    newTm.position   = oldTms[i].position;
                    newTm.rotation   = oldTms[i].rotation;
                }              
            }

            // Save scale matrices because of roots identity scale
            return GetCenterMatrices(renderers);
        }
       
        // Create flat hierarchy only with one main root
        Matrix4x4[] FlatHierarchy(Transform tm, bool origScale)
        {
            // Create root
            mainRoot                    = new GameObject();
            mainRoot.name               = tm.name;
            mainRoot.transform.position = tm.position;
            mainRoot.transform.rotation = tm.rotation;
            mainRoot.transform.localScale = origScale == false 
                ? new Vector3(1, 1, 1)
                : tm.lossyScale;
            
            // Collect oldNew map
            transMap.Clear();
            for (int i = 0; i < renderers.Count; i++)
                transMap.Add(renderers[i].transform, mainRoot.transform);
            
            // Get array of global scale matrices
            return origScale == false 
                ? GetCenterMatrices(renderers) 
                : GetCenterMatricesFlat(renderers, tm);
        }
        
        // Get array of global scale matrices
        static Matrix4x4[] GetCenterMatrices(List<Renderer> renderers)
        {
            Matrix4x4[] centMatrices = new Matrix4x4[renderers.Count];
            for (int i = 0; i < centMatrices.Length; i++)
                centMatrices[i] = Matrix4x4.Scale(renderers[i].gameObject.transform.lossyScale);
            return centMatrices;
        }
        
        // Get array of global scale matrices for flat hierarchy with 1 1 1 scale for frags
        static Matrix4x4[] GetCenterMatricesFlat(List<Renderer> renderers, Transform tm)
        {
            Matrix4x4[] centMatrices = new Matrix4x4[renderers.Count];
            for (int i = 0; i < centMatrices.Length; i++)
                centMatrices[i] = Matrix4x4.Scale((tm.localToWorldMatrix.inverse * renderers[i].gameObject.transform.localToWorldMatrix).lossyScale);
            return centMatrices;
        }
        
        // Get array of flat matrices
        static Matrix4x4[] GetFlatMatrices(List<Renderer> renderers, Transform tm)
        {
            Matrix4x4[] flatMatrices = new Matrix4x4[renderers.Count];
            for (int i = 0; i < flatMatrices.Length; i++)
                flatMatrices[i] = tm.localToWorldMatrix.inverse * renderers[i].transform.localToWorldMatrix;
            return flatMatrices;
        }
        
        // Create root structure copy
        static void GetChain(Transform mainTm, Transform rendTm, ref Dictionary<Transform, Transform> transMap, ref GameObject rootGO)
        {
            GameObject gameObject1 = null;
            GameObject gameObject2;
            
            // Repeat for all fragmented meshes transform
            while (true)
            {
                // Check if mesh tm in dictionary. 
                if (transMap.ContainsKey(rendTm) == true)
                {
                    // Set corresponding pair in original structure. 
                    gameObject2 = transMap[rendTm].gameObject;
                }
                
                // Mesh tm is not yet in dictionary
                else
                {
                    // Create corresponding object for structure copy
                    gameObject2 = new GameObject(rendTm.name);
                    
                    // Set position, rotation, scale
                    gameObject2.transform.localPosition = rendTm.localPosition;
                    gameObject2.transform.localRotation = rendTm.localRotation;
                    gameObject2.transform.localScale    = rendTm.localScale;
                    
                    // Collect to dictionary
                    transMap.Add(rendTm, gameObject2.transform);
                }
                
                // Set defined object as child for corresponding object in structure copy
                if (gameObject1 != null)
                    gameObject1.transform.SetParent(gameObject2.transform, false);
                
                // If mesh tm is not main root tm set rendTm to parent for next cycle
                if (rendTm != mainTm)
                {
                    gameObject1 = gameObject2;
                    rendTm      = rendTm.parent;
                }
                
                // Break if mesh time finally main root tm.
                else
                    break;
            }
            rootGO = gameObject2;
        }

        /// /////////////////////////////////
        /// Maps ops
        /// /////////////////////////////////
        
        static void SetOriginalMaps(RFEngine engine)
        {
            engine.origMaps = new Utils.MeshMaps[engine.origMeshes.Count];
            for (int i = 0; i < engine.origMeshes.Count; ++i)
            {
                engine.origMaps[i] = new Utils.MeshMaps();
                engine.origMaps[i].SetNormals(engine.origMeshes[i].normals);
                engine.origMaps[i].SetTexCoords (engine.origMeshes[i]);
                engine.origMaps[i].SetVertexColors(engine.origMeshes[i].colors);
                engine.origMaps[i].SetTangents(engine.origMeshes[i].tangents);
            }
        }

        static void ComputeInnerUV(RFEngine engine, Matrix4x4 rootMat, float uvScale, Vector2 uvAreaBegin, Vector2 uvAreaEnd)
        {
            Vector3 aabbMin = new Vector3();
            Vector3 aabbMax = new Vector3();
            Utils.Mesh.GetAABB(engine.utilFrags, out aabbMin, out aabbMax, rootMat.inverse);
            engine.fragMaps = new Utils.MeshMaps[engine.utilFrags.Length][];
            for (int i = 0; i < engine.utilFrags.Length; ++i)
            {
                engine.fragMaps[i] = new Utils.MeshMaps[engine.utilFrags[i].Length];
                for (int t = 0; t < engine.utilFrags[i].Length; ++t)
                {
                    engine.fragMaps[i][t] = new Utils.MeshMaps();
                    engine.fragMaps[i][t].ComputeInnerUV(engine.utilFrags[i][t], engine.origMaps[i], rootMat, aabbMin, aabbMax, uvScale, uvAreaBegin, uvAreaEnd);
                }
            }
        }
        
        static void ComputeMaps(RFEngine engine, Color innerColor, bool smoothInner) 
        {
            for (int i = 0; i < engine.utilFrags.Length; ++i)
                for (int t = 0; t < engine.utilFrags[i].Length; ++t)
                {
                    engine.fragMaps[i][t].BuildBary(engine.utilMeshes[i], engine.utilFrags[i][t]);

                    if (engine.skinFlags[i] == false)
                        engine.fragMaps[i][t].ComputeNormals(engine.origMaps[i], engine.utilMeshes[i], engine.utilFrags[i][t], (Matrix4x4[])null, (Transform[])null, smoothInner);
                    else
                        engine.fragMaps[i][t].ComputeNormals(engine.origMaps[i], engine.utilMeshes[i], engine.utilFrags[i][t], engine.origMeshes[i].bindposes, ((SkinnedMeshRenderer)engine.renderers[i]).bones, smoothInner);

                    engine.fragMaps[i][t].RestoreOrigUV(engine.origMaps[i], engine.utilMeshes[i], engine.utilFrags[i][t]);
                    engine.fragMaps[i][t].ComputeVertexColors(engine.origMaps[i], engine.utilMeshes[i], engine.utilFrags[i][t], innerColor);
                    engine.fragMaps[i][t].ComputeTangents(engine.origMaps[i], engine.utilMeshes[i], engine.utilFrags[i][t]);
                }
        }

        /// /////////////////////////////////
        /// Skinned ops
        /// /////////////////////////////////

        // Get bones transforms
        Transform[] GetBonesTransform(int ind)
        {
            if (skinFlags[ind] == false)
                return null;

            // Get instantiated or original bones
            Transform[] bones    = ((SkinnedMeshRenderer)renderers[ind]).bones;
            Transform[] bonesTns = new Transform[bones.Length];
            for (int i = 0; i < bones.Length; ++i)
            {
                bool containsKey = transMap.ContainsKey (bones[i]);
                if (containsKey == true)
                    bonesTns[i] = transMap[bones[i]];
                else
                {
                    // Add to be used in GetRootBone()
                    transMap.Add(bones[i], bones[i]);
                    bonesTns[i] = bones[i];
                }
            }
            return bonesTns;
        }
        
        // Set Skin data 
        static void SetSkinData (RFEngine engine, GameObject fragGo, Mesh fragUnityMesh, Transform[] bones, int ind1, int ind2, Material iMat)
        {
            // Set bone data
            fragUnityMesh.boneWeights = engine.utilFrags[ind1][ind2].GetSkinData();
                
            Matrix4x4   centroidMat       = Matrix4x4.Translate(engine.centroids[ind1][ind2]);
            Matrix4x4[] bindPoses         = new Matrix4x4[engine.origMeshes[ind1].bindposes.Length];
            Matrix4x4[] bindPosesForScale = new Matrix4x4[engine.origMeshes[ind1].bindposes.Length];
            for (int ii = 0; ii < bindPoses.Length; ii++)
            {
                bindPoses[ii]         = engine.origMeshes[ind1].bindposes[ii] * centroidMat;
                bindPosesForScale[ii] = bindPoses[ii];
            }
            fragUnityMesh.bindposes = bindPoses;
                
            // TODO sh.skinnedMeshesOrigScale.Add(Tuple.Create(fragMesh, bindPosesForScale));
                
            SkinnedMeshRenderer smr = fragGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMaterials = GetCorrectMaterials(engine.GetMaterials(ind1), engine.utilFrags[ind1][ind2], iMat);
            smr.bones           = bones;
            smr.rootBone        = engine.GetRootBone(ind1);
            smr.sharedMesh      = fragUnityMesh;
        }
        
        // Get bones root
        Transform GetRootBone(int i)
        {
            if (skinFlags[i] == false)
                return null;

            // Get instantiated or original bone transform
            return transMap[((SkinnedMeshRenderer)renderers[i]).rootBone];
        }
        
        // Copy Animation and Animator components TODO improve, optimze
        void SyncAnimation(Transform tm)
        {
            Animation[] animationArrayOld = tm.GetComponentsInChildren<Animation>();
            for (int i = 0; i < animationArrayOld.Length; ++i)
            {
                bool containsKey = transMap.ContainsKey (animationArrayOld[i].transform);
                if (containsKey == true)
                {
                    Animation animationOld = animationArrayOld[i];
                    Object.DestroyImmediate (transMap[animationOld.transform].GetComponent<Animation>());
                    Animation animationNew = transMap[animationArrayOld[i].transform].gameObject.AddComponent<Animation>();
                    animationNew.clip = animationOld.clip;
                    List<float> floatList = new List<float>();
                    foreach (AnimationState animationState in animationOld)
                    {
                        floatList.Add (animationState.time);
                        animationNew.AddClip (animationState.clip, animationState.name);
                    }
                    int ind = 0;
                    foreach (AnimationState animationState in animationNew)
                    {
                        animationState.time = floatList[ind];
                        ++ind;
                    }
                    animationNew.playAutomatically = animationOld.playAutomatically;
                    animationNew.animatePhysics    = animationOld.animatePhysics;
                    animationNew.cullingType       = animationOld.cullingType;
                    animationNew.Play();
                }
            }
            Animator[] animatorArrayOld = tm.GetComponentsInChildren<Animator>();
            for (int i = 0; i < animatorArrayOld.Length; ++i)
            {
                bool containsKey = transMap.ContainsKey (animatorArrayOld[i].transform);
                if (containsKey == true)
                {
                    Animator animatorOld  = animatorArrayOld[i];
                    Animator animatorNew = transMap[animatorArrayOld[i].transform].GetComponent<Animator>();
                    for (int t = 0; t < animatorOld.layerCount; ++t)
                    {
                        AnimatorStateInfo animatorStateInfo = animatorOld.GetCurrentAnimatorStateInfo (t);
                        animatorNew.Play (animatorStateInfo.fullPathHash, t, animatorStateInfo.normalizedTime);
                    }
                }
            }
        }

        /// /////////////////////////////////
        /// Getters
        /// /////////////////////////////////
        
        // Check if has skinned meshes
        bool HasSkin { get {
            if (skinFlags != null)
                for (int i = 0; i < skinFlags.Count; i++)
                    if (skinFlags[i] == true)
                        return true;
            return false; }}
        
        
        // Check if has skinned meshes
        public bool HasUtilFragMeshes { get {
            if (utilFrags != null && utilFrags.Length > 0)
                return true;
            return false; }}
        
    }
}

// RFEngine dummy class for not supported platforms
#else
namespace RayFire
{
    public class RFEngine
    {
        public GameObject                       mainRoot;
        public List<Renderer>                     renderers;

        public static void FragmentShatter (RayfireShatter sh) {}
        public static void FragmentRigid (RayfireRigid rg) {}
        public static List<Transform> CreateRigidFragments (RFEngine engine, RayfireRigid rg)
        {
            return null;
        }
        public static void CacheRuntimeV2(RayfireRigid rg) {}
        public static void InteractiveStart(RayfireShatter scr) {}
        public static void InteractiveChange(RayfireShatter scr) {}
        public bool HasUtilFragMeshes 
        { get {
            return false; 
        }}
    }
}

#endif