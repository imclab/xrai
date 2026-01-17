using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_XBOXONE)
using RayFire.DotNet;
#endif

namespace RayFire
{
    [Serializable]
    public class RFDemolitionMesh
    {
        public enum MeshInputType
        {
            AtStart          = 3,
            AtInitialization = 6,
            AtDemolition     = 9
        }
        
        public enum ConvertType
        {
            Disabled         = 0,
            ConnectedCluster = 2,
            Connectivity     = 4
        }

        public RayfireShatter.RFEngineType engTp = RayfireShatter.RFEngineType.V1;
        
        [FormerlySerializedAs ("amount")]      public    int                  am;
        [FormerlySerializedAs ("variation")]   public    int                  var;
        [FormerlySerializedAs ("depthFade")]   public    float                dpf;
        [FormerlySerializedAs ("contactBias")] public    float                bias;
        [FormerlySerializedAs ("seed")]        public    int                  sd;
        [FormerlySerializedAs ("useShatter")]  public    bool                 use;
        [FormerlySerializedAs ("addChildren")] public    bool                 cld;
        [FormerlySerializedAs ("simType")]     public    FragSimType          sim;
        public                                           ConvertType          cnv;
        [FormerlySerializedAs ("meshInput")]      public MeshInputType        inp;
        [FormerlySerializedAs ("properties")]     public RFFragmentProperties prp;
        [FormerlySerializedAs ("runtimeCaching")] public RFRuntimeCaching     ch;
        [FormerlySerializedAs ("scrShatter")]     public RayfireShatter       sht;
        
        // Non serialized
        [NonSerialized] public int       badMesh;
        [NonSerialized] public int       shatterMode;
        [NonSerialized] public int       totalAmount;
        [NonSerialized] public int       innerSubId;
        [NonSerialized] public Mesh      mesh;
        [NonSerialized] public RFShatter rfShatter;
        [NonSerialized] public RFEngine  engine;
        
        // Hidden
        [HideInInspector] public Quaternion rotStart;
        
        static string fragmentStr = "_fr_";

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RFDemolitionMesh()
        {
            InitValues();
            LocalReset();
            prp = new RFFragmentProperties();
            ch  = new RFRuntimeCaching();
        }
        
        // Starting values
        void InitValues()
        {
            engTp       = RayfireShatter.RFEngineType.V1;
            am          = 15;
            var         = 0;
            dpf         = 0.5f;
            bias        = 0f;
            sd          = 1;
            use         = false;
            cld         = true;
            cnv         = 0;
            sim         = FragSimType.Dynamic;
            inp         = MeshInputType.AtDemolition;
            sht         = null;
            shatterMode = 1;
            innerSubId  = 0;
            rotStart    = Quaternion.identity;
            mesh        = null;
            rfShatter   = null;
        }

        // Reset
        public void LocalReset()
        {
            badMesh     = 0;
            totalAmount = 0;
        }
        
        // Pool Reset
        public void GlobalReset()
        {
            InitValues();
            LocalReset();
            
            prp.InitValues();
            ch.InitValues();
        }
        
        // Copy from
        public void CopyFrom (RFDemolitionMesh source)
        {
            engTp = source.engTp;
            am    = source.am;
            var   = source.var;
            dpf   = source.dpf;
            sd    = source.sd;
            bias  = source.bias;
            use   = false;
            cld   = source.cld;
            cnv   = source.cnv;
            sim   = source.sim;
            inp   = source.inp;
            inp   = MeshInputType.AtDemolition;

            prp.CopyFrom (source.prp);
            ch = new RFRuntimeCaching();

            LocalReset();

            shatterMode = 1;
            innerSubId  = 0;
            rotStart    = Quaternion.identity;

            mesh      = null;
            rfShatter = null;
        }

        /// /////////////////////////////////////////////////////////
        /// Demolish
        /// /////////////////////////////////////////////////////////
        
        // Demolish single mesh to fragments
        public static bool DemolishMesh(RayfireRigid scr, RayfireShatter.RFEngineType engTp)
        {
            if (engTp == RayfireShatter.RFEngineType.V2)
                return DemolishMeshV2 (scr);
            return DemolishMeshV1 (scr);
        }
        
        /// /////////////////////////////////////////////////////////
        /// V2 Demolish
        /// /////////////////////////////////////////////////////////
        
        // Demolish single mesh to fragments
        public static bool DemolishMeshV2(RayfireRigid scr)
        {
            // Object demolition
            if (scr.objTp != ObjectType.Mesh && scr.objTp != ObjectType.SkinnedMesh)
                return true;

            // Skip if reference
            if (scr.dmlTp == DemolitionType.ReferenceDemolition)
                return true;
            
            // Already has fragments
            if (scr.HasFragments == true)
            {
                // Set parent
                RayfireMan.SetFragmentRootParent (scr.rtC);
                
                // Set tm 
                scr.rtC.position = scr.tsf.position;
                scr.rtC.rotation = scr.tsf.rotation;

                // Activate root and fragments
                scr.rtC.gameObject.SetActive (true);

                // Set demolished state
                scr.lim.demolished = true;
                
                // Skip coroutines start if Awake prefragment and Convert
                if (scr.dmlTp == DemolitionType.AwakePrefragment && scr.mshDemol.cnv != ConvertType.Disabled)
                    return true;

                // Start all coroutines
                for (int i = 0; i < scr.fragments.Count; i++)
                    scr.fragments[i].StartAllCoroutines();
                
                return true;
            }
            
            // Has unity meshes - create fragments
            if (scr.mshDemol.HasEngineAndMeshes == true)
            {
                RFEngine.CreateRigidFragments (scr.mshDemol.engine, scr);
                scr.lim.demolished = true;
                return true;
            }

            // Still has no Unity meshes - cache Unity meshes
            if (scr.mshDemol.HasEngineAndMeshes == false)
            {
                // Start countdown
                System.Diagnostics.Stopwatch stopWatch0 = new System.Diagnostics.Stopwatch();
                stopWatch0.Start();
                
                // Start countdown
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                
                // Cache unity meshes
                RFEngine.CacheRuntimeV2 (scr);
                
                stopWatch.Stop();
                
                //Debug.Log("V2 CacheRuntime " + stopWatch.Elapsed.TotalMilliseconds + " ms.");

                // Caching in progress. Stop demolition
                if (scr.mshDemol.ch.inProgress == true)
                    return false;
                
                // Fragmentation on not supported platforms. approve and set dml to none
                if (scr.mshDemol.HasEngineAndMeshes == false)
                {
                    scr.lim.demolished = false;
                    return true;
                }
                
                // Has unity meshes - create fragments
                if (scr.mshDemol.HasEngineAndMeshes == true)
                {
                    // Start countdown
                    System.Diagnostics.Stopwatch stopWatch2 = new System.Diagnostics.Stopwatch();
                    stopWatch2.Start();
                    
                    RFEngine.CreateRigidFragments (scr.mshDemol.engine, scr);
                    scr.lim.demolished = true;

                    stopWatch2.Stop();
                    //Debug.Log("V2 CreateRigidFragments " + stopWatch2.Elapsed.TotalMilliseconds + " ms.");
                    
                    stopWatch0.Stop();
                    //Debug.Log("V2 Total Time " + stopWatch0.Elapsed.TotalMilliseconds + " ms.");
                    //Debug.Log("============================================");
                    return true;
                }
            }

            return false;
        }
        
        /// /////////////////////////////////////////////////////////
        /// V1 Demolish
        /// /////////////////////////////////////////////////////////
        
        // Demolish single mesh to fragments
        public static bool DemolishMeshV1 (RayfireRigid scr)
        {
            // Object demolition
            if (scr.objTp != ObjectType.Mesh && scr.objTp != ObjectType.SkinnedMesh)
                return true;
            
            // Skin demolition
            if (scr.objTp == ObjectType.SkinnedMesh && scr.skr == null)
                return false;

            // Skip if reference
            if (scr.dmlTp == DemolitionType.ReferenceDemolition)
                return true;

            // Already has fragments
            if (scr.HasFragments == true)
            {
                // Set tm 
                scr.rtC.position = scr.tsf.position;
                scr.rtC.rotation = scr.tsf.rotation;

                // Set parent
                RayfireMan.SetFragmentRootParent (scr.rtC);

                // Activate root and fragments
                scr.rtC.gameObject.SetActive (true);

                // Set demolished state
                scr.lim.demolished = true;
                
                // Skip coroutines start if Awake prefragment and Convert
                if (scr.dmlTp == DemolitionType.AwakePrefragment && scr.mshDemol.cnv != ConvertType.Disabled)
                    return true;

                // Start all coroutines
                for (int i = 0; i < scr.fragments.Count; i++)
                    scr.fragments[i].StartAllCoroutines();

                return true;
            }
            
            // Has unity meshes - create fragments
            if (scr.HasMeshes == true)
            {
                scr.fragments              = CreateFragments (scr);
                scr.lim.demolished = true;
                return true;
            }

            // Still has no Unity meshes - cache Unity meshes
            if (scr.HasMeshes == false)
            {
                // Start countdown
                System.Diagnostics.Stopwatch stopWatch0 = new System.Diagnostics.Stopwatch();
                stopWatch0.Start();
                
                // Start countdown
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                
                // Cache unity meshes
                CacheRuntimeV1 (scr);

                stopWatch.Stop();
                //Debug.Log("V1 CacheRuntime " + stopWatch.Elapsed.TotalMilliseconds + " ms.");
                
                // Caching in progress. Stop demolition
                if (scr.mshDemol.ch.inProgress == true)
                    return false;
                
                // Fragmentation on not supported platforms. approve and set dml to none
                if (scr.meshes == null)
                {
                    scr.lim.demolished = false;
                    return true;
                }
                
                // Has unity meshes - create fragments
                if (scr.HasMeshes == true)
                {
                    // Start countdown
                    System.Diagnostics.Stopwatch stopWatch2 = new System.Diagnostics.Stopwatch();
                    stopWatch2.Start();
                    
                    scr.fragments              = CreateFragments (scr);
                    scr.lim.demolished = true;
                    
                    stopWatch2.Stop();
                    //Debug.Log("V1 CreateFragments " + stopWatch2.Elapsed.TotalMilliseconds + " ms.");
                    
                    stopWatch0.Stop();
                    //Debug.Log("V1 Total Time " + stopWatch0.Elapsed.TotalMilliseconds + " ms.");
                    //Debug.Log("============================================");
                    
                    return true;
                }
            }

            return false;
        }

        // Create fragments by mesh and pivots array
        static List<RayfireRigid> CreateFragments (RayfireRigid scr)
        {
            // Fragments list
            List<RayfireRigid> scrArray = new List<RayfireRigid>(scr.meshes.Length);

            // Stop if has no any meshes
            if (scr.meshes == null)
                return scrArray;
            
            // Create RayFire manager if not created
            RayfireMan.RayFireManInit();
            
            // Create root object and parent
            RFLimitations.CreateFragmentsRoot (scr);
            
            // Name 
            string baseName  = scr.gameObject.name + fragmentStr;
            
            // Set rotation to precache rotation
            if (scr.dmlTp == DemolitionType.AwakePrecache)
                scr.rtC.transform.rotation = scr.chRot;
            
            // Create fragment objects
            MeshesToObjects (scr, scrArray, baseName);
            
            // Fix transform for precached fragments
            if (scr.dmlTp == DemolitionType.AwakePrecache)
                scr.rtC.rotation = scr.tsf.rotation;

            // Fix runtime caching rotation difference. Get rotation difference and add to root
            if (scr.dmlTp == DemolitionType.Runtime && scr.mshDemol.ch.tp != CachingType.Disabled)
            {
                Quaternion cacheRotationDif = scr.tsf.rotation * Quaternion.Inverse (scr.mshDemol.rotStart);
                scr.rtC.rotation = cacheRotationDif * scr.rtC.rotation;
            }
            
            // Reset scale for mesh fragments. IMPORTANT: skinned mesh fragments root should not be rescaled 
            if (scr.skr == null)
                scr.rtC.localScale = Vector3.one;
            
            // Set root to manager
            RayfireMan.SetFragmentRootParent (scr.rtC);
            
            // Ignore neib collisions
            RFPhysic.SetIgnoreColliders (scr.physics, scrArray);
            
            return scrArray;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Slice
        /// /////////////////////////////////////////////////////////
        
        // SLice mesh
        public static bool SliceMesh(RayfireRigid scr, RayfireShatter.RFEngineType engTp)
        {
            if (engTp == RayfireShatter.RFEngineType.V2)
                return SliceMeshV2 (scr);
            return SliceMeshV1 (scr);
        }
        
        /// /////////////////////////////////////////////////////////
        /// V2 Slice
        /// /////////////////////////////////////////////////////////

        // SLice mesh
        public static bool SliceMeshV2(RayfireRigid scr)
        {
            // Cache unity meshes
            RFEngine.CacheRuntimeV2 (scr);

            // Set force planes
            Plane forcePlane = new Plane (scr.lim.slicePlanes[1], scr.lim.slicePlanes[0]);
            
            // Clear slice planes
            scr.lim.slicePlanes.Clear();

            // Create slices
            if (scr.mshDemol.HasEngineAndMeshes == true)
                RFEngine.CreateRigidFragments (scr.mshDemol.engine, scr);
            else return false;
            
            // Set demolition 
            scr.lim.demolished = true;
            
            // Check for sliced inactive/kinematic with unyielding
            RayfireUnyielding.SetUnyieldingFragments (scr, true);
            
            // Fragments initialisation
            scr.InitMeshFragments();
            
            // Add force
            AddForce (scr, forcePlane);

            // Set children with mesh as additional fragments
            ChildrenToFragments(scr);
            
            return true;
        }
        
        /// /////////////////////////////////////////////////////////
        /// V1 Slice
        /// /////////////////////////////////////////////////////////

        // SLice mesh
        public static bool SliceMeshV1(RayfireRigid scr)
        {
            // Empty lists
            scr.DeleteCache();
            scr.DeleteFragments();
    
            // SLice
            RFFragment.SliceMeshes (ref scr.meshes, ref scr.pivots, ref scr.subIds, scr, scr.lim.slicePlanes);
            
            // TODO check if has slicePlanes
            
            // Remove plane info 
            Plane forcePlane = new Plane (scr.lim.slicePlanes[1], scr.lim.slicePlanes[0]);
            scr.lim.slicePlanes.Clear();
            
            // Stop
            if (scr.HasMeshes == false)
                return false;

            // Get fragments
            scr.fragments = CreateSlices(scr);

            // Check for sliced inactive/kinematic with unyielding
            RayfireUnyielding.SetUnyieldingFragments (scr, true);

            // TODO check for fragments
            
            // Set demolition 
            scr.lim.demolished = true;
            
            // Fragments initialisation
            scr.InitMeshFragments();
            
            // Add force
            AddForce (scr, forcePlane);

            // Set children with mesh as additional fragments
            ChildrenToFragments(scr);
            
            return true;
        }
        
        // Create slices by mesh and pivots array
        static List<RayfireRigid> CreateSlices (RayfireRigid scr)
        {
            // Fragments list
            List<RayfireRigid> scrArray = new List<RayfireRigid>(scr.meshes.Length);

            // Stop if has no any meshes
            if (scr.meshes == null)
                return scrArray;
            
            // Create RayFire manager if not created
            RayfireMan.RayFireManInit();
            
            // Create root object and parent
            RFLimitations.CreateFragmentsRoot (scr);
            
            // Name 
            string baseName  = scr.gameObject.name + "_";
            
            // Create fragment objects
            MeshesToObjects (scr, scrArray, baseName);
            
            // Reset scale for mesh fragments. IMPORTANT: skinned mesh fragments root should not be rescaled 
            if (scr.skr == null)
                scr.rtC.localScale = Vector3.one;
            
            // Set root to manager
            RayfireMan.SetFragmentRootParent (scr.rtC);
            
            // Empty lists
            scr.DeleteCache();

            return scrArray;
        }

        // Add force to slices
        static void AddForce(RayfireRigid scr, Plane forcePlane)
        {
            if (scr.lim.sliceForce != 0)
            {
                foreach (var frag in scr.fragments)
                {
                    // Skip inactive fragments
                    if (scr.lim.affectInactive == false && frag.simTp == SimType.Inactive)
                        continue;
                    
                    // Apply force
                    Vector3 closestPoint = forcePlane.ClosestPointOnPlane (frag.transform.position);
                    Vector3 normalVector = (frag.tsf.position - closestPoint).normalized;
                    frag.physics.rb.AddForce (normalVector * scr.lim.sliceForce, ForceMode.VelocityChange);

                    /* TODO force to spin fragments based on blades direction
                    normalVector = new Vector3 (-1, 0, 0);
                    frag.physics.rigidBody.AddForceAtPosition (normalVector * scr.limitations.sliceForce, closestPoint, ForceMode.VelocityChange);
                    */
                }
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Caching
        /// /////////////////////////////////////////////////////////
        
        // Start cache fragment meshes. Instant or runtime
        static void CacheRuntimeV1 (RayfireRigid scr)
        {
            // Reuse existing cache
            if (scr.reset.action == RFReset.PostDemolitionType.DeactivateToReset && 
                scr.reset.mesh == RFReset.MeshResetType.ReuseFragmentMeshes)
                if (scr.HasMeshes == true)
                    return;

            // Clear all mesh data
            scr.DeleteCache();

            // Cache meshes
            if (scr.mshDemol.ch.tp == CachingType.Disabled)
                CacheInstant(scr);
            else
                scr.CacheFramesV1();
        }
        
        // Instant caching into meshes. IMPORTANT: static methods should be in RFFragment dummy class 
        static void CacheInstant (RayfireRigid scr)
        {
            // Input mesh, setup
            if (RFFragment.InputMesh (scr) == false)
                return;

            // Create fragments
            RFFragment.CacheMeshesInst (ref scr.meshes, ref scr.pivots, ref scr.subIds, scr);
        }       
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////
        
        // Create objects for meshes
        static void MeshesToObjects(RayfireRigid scr, List<RayfireRigid> scrArray, string baseName)
        {
            // Tag and layer
            int    baseLayer = RFFragmentProperties.GetLayer(scr);
            string baseTag   = RFFragmentProperties.GetTag(scr);
            
            // Set tag and layer to root. IMPORTANT. Some overlaps uses mask by root layer
            scr.rtC.gameObject.layer = baseLayer;
            scr.rtC.gameObject.tag   = baseTag;
            
            // Get original mats
            Material[] mats = scr.skr != null
                ? scr.skr.sharedMaterials
                : scr.mRnd.sharedMaterials;
            
            // Create fragment objects
            for (int i = 0; i < scr.meshes.Length; ++i)
            {
                // Get object from pool or create
                RayfireRigid rfScr = RayfireMan.inst.fragments.rgInst == null
                    ? RayfireMan.inst.fragments.CreateRigidInstance()
                    : RayfireMan.inst.fragments.GetPoolObject();

                // Setup
                rfScr.tsf.position     = scr.tsf.position + scr.pivots[i];
                rfScr.tsf.parent       = scr.rtC;
                rfScr.name             = baseName + i;
                rfScr.gameObject.tag   = baseTag;
                rfScr.gameObject.layer = baseLayer;
                rfScr.mFlt.sharedMesh  = scr.meshes[i];
                rfScr.rtP              = scr.rtC;
                
                // Copy properties from parent to fragment node
                scr.CopyPropertiesTo (rfScr);

                // Set custom fragment simulation type if not inherited
                RFPhysic.SetFragmentSimulationType (rfScr, scr.simTp);
                
                // Copy particles
                RFPoolingParticles.CopyParticlesRigid (scr, rfScr);
                
                // Set collider
                RFPhysic.SetFragmentCollider (rfScr, scr.meshes[i]);
                
                // Copy Renderer properties
                CopyRenderer (scr, rfScr.mRnd, scr.meshes[i].bounds);
                
                // Turn on
                rfScr.gameObject.SetActive (true);

                // Set limitations properties
                SetLimitationProps(rfScr, scr.lim.currentDepth);
                
                // Set multymaterial
                RFSurface.SetMaterial (scr.subIds, mats, scr.materials, rfScr.mRnd, i, scr.meshes.Length);
                    
                // Set mass by mass value accordingly to parent
                if (rfScr.physics.mb == MassType.MassProperty)
                    RFPhysic.SetMassByParent (rfScr.physics, rfScr.lim.bboxSize, scr.physics.ms, scr.lim.bboxSize);
                
                // Add in array
                scrArray.Add (rfScr);
            }
        }
        
        // Set limitations properties
        public static void SetLimitationProps(RayfireRigid rfScr, int depth)
        {
            // Update depth level and amount
            rfScr.lim.currentDepth = depth + 1;
            rfScr.mshDemol.am              = (int)(rfScr.mshDemol.am * rfScr.mshDemol.dpf);
            if (rfScr.mshDemol.am < 3)
                rfScr.mshDemol.am = 3;
                
            // Disable outer mat for depth fragments
            if (rfScr.lim.currentDepth >= 1)
                rfScr.materials.oMat = null;
        }
        
        // Set custom fragment simulation type if not inherited
        public static void SetClusterSimulationType (RayfireRigid frag, SimType sim)
        {
            frag.simTp = sim;
            if (frag.clsDemol.sim != FragSimType.Inherit)
                frag.simTp = (SimType)frag.clsDemol.sim;
        }
                
        // Copy mesh renderer properties
        public static void CopyRenderer (RayfireRigid scr, MeshRenderer trg, Bounds bounds)
        {
            // Shadow casting
            if (RayfireMan.inst.advancedDemolitionProperties.sizeThreshold > 0 && 
                RayfireMan.inst.advancedDemolitionProperties.sizeThreshold > bounds.size.magnitude)
                trg.shadowCastingMode = ShadowCastingMode.Off;
            
            /*
            trg.receiveGI = scr.meshRenderer.receiveGI;
            trg.rayTracingMode            = scr.meshRenderer.rayTracingMode;
            trg.lightProbeUsage           = scr.meshRenderer.lightProbeUsage;
            trg.reflectionProbeUsage      = scr.meshRenderer.reflectionProbeUsage;
            trg.allowOcclusionWhenDynamic = scr.meshRenderer.allowOcclusionWhenDynamic;
            */
        }
        
        /// /////////////////////////////////////////////////////////
        /// Precache and Prefragment
        /// /////////////////////////////////////////////////////////  
        
        // Precache meshes at awake
        public static void Awake(RayfireRigid scr)
        {
            // Not mesh
            if (scr.objTp != ObjectType.Mesh)
                return;
                
            // Precache
            if (scr.dmlTp == DemolitionType.AwakePrecache)
                PreCache(scr);

            // Precache and prefragment
            if (scr.dmlTp == DemolitionType.AwakePrefragment)
            {
                PreCache(scr);
                Prefragment(scr);
            }
        }

        // PreCache meshes
        static void PreCache(RayfireRigid scr)
        {
            // Save and disable bias
            float bias = scr.mshDemol.bias;
            scr.mshDemol.bias = 0;
                
            // Cache frag meshes
            if (scr.mshDemol.engTp == RayfireShatter.RFEngineType.V1)
                CacheInstant (scr);
            else
                RFEngine.CacheRuntimeV2 (scr);

            // Restore bias
            scr.mshDemol.bias = bias;
        }
        
        // Predefine fragments
        static void Prefragment(RayfireRigid scr)
        {
            // Delete existing
            scr.DeleteFragments();

            // Create fragments from cache
            if (scr.mshDemol.engTp == RayfireShatter.RFEngineType.V1)
                scr.fragments = CreateFragments(scr);
            else
                RFEngine.CreateRigidFragments (scr.mshDemol.engine, scr);
                
            // Stop
            if (scr.HasFragments == false)
            {
                scr.dmlTp = DemolitionType.None;
                return;
            }
            
            // Set physics properties
            for (int i = 0; i < scr.fragments.Count; i++)
            {
                scr.fragments[i].SetComponentsBasic();
                scr.fragments[i].SetComponentsPhysics();
                scr.fragments[i].SetObjectType();

                // Increment demolition depth. Disable if last
                scr.fragments[i].lim.currentDepth = 1;
                if (scr.lim.depth == 1)
                    scr.fragments[i].dmlTp = DemolitionType.None;
            }
            
            // Copy Uny state to fragments in case object has Uny components
            RayfireUnyielding.SetUnyieldingFragments(scr, false);
            
            // Clusterize awake fragments
            SetupRuntimeConnectedCluster (scr, true);
            
            // Deactivate fragments root
            if (scr.rtC != null)
                scr.rtC.gameObject.SetActive (false);
            
            // Setup awake connectivity
            SetupRuntimeConnectivity(scr, true);
        }
        
        // Clusterize runtime fragments
        public static bool SetupRuntimeConnectedCluster (RayfireRigid rigid, bool awake)
        {
            // Clusterize disabled
            if (rigid.mshDemol.cnv != ConvertType.ConnectedCluster)
                return false;
            
            // Skip if Runtime init and fragments already clusterized in awake.
            if (rigid.dmlTp == DemolitionType.AwakePrefragment && rigid.lim.demolished == true)
                return false;
            
            // Not mesh demolition
            if (rigid.objTp != ObjectType.Mesh)
                return false;
            
            // Not runtime
            if (rigid.dmlTp == DemolitionType.None || 
                rigid.dmlTp == DemolitionType.ReferenceDemolition)
                return false;

            // No fragments
            if (rigid.HasFragments == false)
                return false;

            // Create Connected cluster Rigid
            RayfireRigid clsRigid = rigid.rtC.gameObject.AddComponent<RayfireRigid>();

            // Copy properties
            rigid.CopyPropertiesTo (clsRigid);
            
            // Copy particles
            RFPoolingParticles.CopyParticlesRigid (rigid, clsRigid);  

            // Destroy particles on fragments
            for (int i = rigid.fragments.Count - 1; i >= 0; i--)
            {
                // Destroy Debris/Dust for all fragments
                if (rigid.fragments[i].HasDebris)
                    for (int d = rigid.fragments[i].debrisList.Count - 1; d >= 0; d--)
                        Object.Destroy (rigid.fragments[i].debrisList[d]);
                if (rigid.fragments[i].HasDust)
                    for (int d = rigid.fragments[i].dustList.Count - 1; d >= 0; d--)
                        Object.Destroy (rigid.fragments[i].dustList[d]);
            }

            // Set properties
            clsRigid.objTp            = ObjectType.ConnectedCluster;
            clsRigid.dmlTp            = DemolitionType.Runtime;
            clsRigid.clsDemol.cluster = new RFCluster();
            
            // Init
            clsRigid.Initialize(); 

            // Set uny states and sim
            RayfireUnyielding[] unyArray = rigid.GetComponents<RayfireUnyielding>();
            if (unyArray.Length > 0)
                for (int i = 0; i < unyArray.Length; i++)
                    if (unyArray[i].enabled == true)
                        RayfireUnyielding.ClusterOverlap(unyArray[i], clsRigid);
            
            // Stop if awake connected cluster
            if (awake == true)
            {
                DestroyComponents (rigid.fragments);
                return true;
            }
            
            // Set contact point for demolition
            clsRigid.lim.contactPoint   = rigid.lim.contactPoint;
            clsRigid.lim.contactNormal  = rigid.lim.contactNormal;
            clsRigid.lim.contactVector3 = rigid.lim.contactVector3;

            // Inherit velocity
            clsRigid.physics.velocity    = rigid.physics.velocity;
            clsRigid.physics.rb.linearVelocity = rigid.physics.velocity;
            
            // Demolish cluster and get solo shards
            List<RFShard> detachShards = RFDemolitionCluster.DemolishConnectedCluster (clsRigid);
  
            // No Shards to detach
            if (detachShards == null || detachShards.Count == 0)
            {
                DestroyComponents (rigid.fragments);
                return true;
            }

            // Get has for all detached objects to keep their Rigid and rigidbody. Should be used even if no detach shards.
            HashSet<Transform> detachTms = new HashSet<Transform>();
            for (int i = 0; i < detachShards.Count; i++)
                detachTms.Add (detachShards[i].tm);
           
            // Destroy fragments rigid, rigidbody for NOT detached shards
            for (int i = rigid.fragments.Count - 1; i >= 0; i--)
            {
                // Destroy rb, rigid
                if (detachTms.Contains (rigid.fragments[i].tsf) == false)
                {
                    Object.Destroy (rigid.fragments[i].physics.rb);
                    Object.Destroy (rigid.fragments[i]);
                    rigid.fragments.RemoveAt (i);
                }
            }

            // TODO add main and child clusters to fragments list. get them in scr.fragments
            
            // Delete if cluster was completely demolished
            if (clsRigid.lim.demolished == true)
                RayfireMan.DestroyFragment (clsRigid, null);

            return true;
        }

        // Destroy fragments rigid, rigidbody
        static void DestroyComponents(List<RayfireRigid> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Object.Destroy (list[i].physics.rb);
                Object.Destroy (list[i]);
            }
        }

        // Setup Connectivity, Unyielding and MeshRoot
        public static void SetupRuntimeConnectivity(RayfireRigid scr, bool awake)
        {
            // Connectivity disabled
            if (scr.mshDemol.cnv != ConvertType.Connectivity)
                return;
            
            // Component check
            scr.act.cnt = scr.GetComponent<RayfireConnectivity>();
            
            // Debug message and return
            if (scr.act.cnt == null)
            {
                RayfireMan.Log (RFLog.rig_dbgn + scr.name + RFLog.rig_act1 + scr.mshDemol.cnv.ToString() + RFLog.rig_act2, scr.gameObject);
                scr.mshDemol.cnv = ConvertType.Disabled;
                return;
            }
            
            // Add meshroot Rigid
            RayfireRigid mRoot = scr.rtC.gameObject.GetComponent<RayfireRigid>();
            
            // Skip if Runtime init and fragments already connected in awake.
            if (mRoot != null)
                return;

            mRoot = scr.rtC.gameObject.AddComponent<RayfireRigid>();

            // Set MeshRoot properties
            scr.CopyPropertiesTo (mRoot);
            mRoot.init = RayfireRigid.InitType.AtStart;
            mRoot.objTp          = ObjectType.MeshRoot;
            mRoot.dmlTp          = DemolitionType.None;
            mRoot.simTp          = scr.simTp;
            mRoot.act.con = true;
            
                
            /*
            // TODO set mRoot fragments Rigid sim type to FragSimType
            put SetUnyieldingFragments after this method
            Debug.Log (mRoot.simTp);
            // Set sim type for root as well.
            if (mRoot.mshDemol.sim != FragSimType.Inherit)
                mRoot.simTp = (SimType)scr.mshDemol.sim;
            
            Debug.Log (mRoot.simTp);
            */
            
            // Set Connectivity activation for fragments
            for (int i = 0; i < scr.fragments.Count; i++)
                scr.fragments[i].act.con = true;

            // Add Connectivity
            RayfireConnectivity mRootConnectivity = scr.rtC.gameObject.AddComponent<RayfireConnectivity>();

            // Copy Connectivity properties
            RayfireConnectivity.CopyTo (scr.act.cnt, mRootConnectivity);

            // Activate to initialize
            scr.rtC.gameObject.SetActive (true);

            // DeActivate in awake / init at runtime
            if (awake == true)
                scr.rtC.gameObject.SetActive (false);
            else
            {
                mRoot.Initialize();
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Children ops
        /// /////////////////////////////////////////////////////////  
        
        // Set children with mesh as additional fragments
        public static void ChildrenToFragments(RayfireRigid scr)
        {
            // Not for clusters
            if (scr.IsCluster == true)
                return;

            // Disabled
            if (scr.mshDemol.cld == false)
                return;
            
            // No children
            if (scr.tsf.childCount == 0)
                return;
            
            // Iterate children TODO precache in awake and use now. Set init type to by method at awake.
            Transform child;
            int childCount = scr.tsf.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                // Get child
                child = scr.tsf.GetChild (i);

                // Skip if has no mesh
                if (child.GetComponent<MeshFilter>() == false)
                    continue;

                // Set parent to main fragments root
                child.parent = scr.rtC;
                
                // Get Already applied Rigid
                RayfireRigid childScr = child.GetComponent<RayfireRigid>();

                // Add new if has no. Copy properties
                if (childScr == null)
                {
                    childScr = child.gameObject.AddComponent<RayfireRigid>();
                    childScr.init = RayfireRigid.InitType.ByMethod;
                    scr.CopyPropertiesTo (childScr);
                    
                    // Enable use shatter
                    childScr.mshDemol.sht = child.GetComponent<RayfireShatter>();
                    if (childScr.mshDemol.sht != null)
                        childScr.mshDemol.use = true;
                }
                
                // Set custom fragment simulation type if not inherited
                RFPhysic.SetFragmentSimulationType (childScr, scr.simTp);

                // Init
                childScr.Initialize();
                
                // Update depth level and amount
                childScr.lim.currentDepth = scr.lim.currentDepth + 1;
                
                // Collect
                scr.fragments.Add (childScr);
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////

        // Get seed
        public int Seed { get {
            if (sd == 0)
                return Random.Range (0, 100);
            return sd;
        }}
        
        // Get amount with variation
        public int Amount { get {
            if (var > 0)
                return am + Random.Range (0, am * var / 100);
            return am;
        }}
        
        // Get use shatter state
        public bool UseShatter { get
        {
            return use == true && sht != null;
        }}
        
        // Get use shatter state
        public bool HasEngineAndMeshes { get
        {
            return engine != null && engine.HasUtilFragMeshes == true;
        }}
        
    }
}