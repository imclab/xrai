using UnityEngine;

namespace RayFire
{
    /// <summary>
    /// Rayfire fade batch class.
    /// </summary>
    public class RFFadeBatch
    {
        readonly RayfireRigid     rigid;
        //readonly RayfireRigidRoot root;
        readonly Transform        tm;
        readonly FadeType         fadeType;
        readonly float            lifeTime;
        int                       state; // 1 - sim start, 2 - sim active, 3 - living,  4 - Fading, 5 - Faded
        Vector3                   oldPos;
        float                     lifeNow;
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
        
        // Constructor
        public RFFadeBatch(RayfireRigid scr)
        {
            rigid    = scr;
            tm       = scr.tsf;
            fadeType = scr.fading.fadeType;
            lifeTime = scr.fading.lifeTime;
            if (scr.fading.lifeVariation > 0)
                lifeTime += Random.Range (0, scr.fading.lifeVariation);
            oldPos   = tm.position;
            lifeNow  = 0;
            state    = 1;
            if (scr.fading.lifeType != RFFadeLifeType.BySimulationAndLifeTime)
                state = 3;
        }

        // Constructor
        public RFFadeBatch(RayfireRigidRoot scr, RFShard shard)
        {
            //root     = scr;
            tm       = shard.tm;
            fadeType = scr.fading.fadeType;
            lifeTime = scr.fading.lifeTime;
            if (scr.fading.lifeVariation > 0)
                lifeTime += Random.Range (0, scr.fading.lifeVariation);
            oldPos   = tm.position;
            lifeNow  = 0;
            state    = 1;
            if (scr.fading.lifeType != RFFadeLifeType.BySimulationAndLifeTime)
                state = 3;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Common Methods
        /// /////////////////////////////////////////////////////////
        
        // Check active sim  
        public static bool SimCheck(RFFadeBatch batch)
        {
            // Sim check disabled
            if (batch.state > 2)
                return false;
            
            // Null check
            if (batch.tm == null)
            {
                batch.state = 5;
                return false;
            }

            // Continue if first check. Set state to active sim
            if (batch.state == 1)
            {
                batch.state  = 2;
                batch.oldPos = batch.tm.position;
                return true;
            }

            // Continue if passed distance more than threshold
            if (Vector3.Distance (batch.tm.position, batch.oldPos) > RayfireMan.fadeDistThreshold)
            {
                batch.oldPos = batch.tm.position;
                return true;
            }

            // Set to living state, disable sim state
            batch.state = 3;
            return false;
        }
        
        // Check living 
        public static bool LiveCheck(RFFadeBatch batch)
        {
            if (batch.state == 3)
            {
                // Living in progress
                if (batch.lifeTime > batch.lifeNow)
                {
                    batch.lifeNow += RayfireMan.fadeTimeRate;
                    return true;
                }
                     
                // Living finished
                batch.state = 4;
            }
            
            return false;
        }
        
        // Start fading
        public static void StartFade(RFFadeBatch batch)
        {
            // Faded
            if (batch.state == 5)
                return;
            
            // Null check
            if (batch.tm == null)
                return;
            
            StartFadeRigid (batch);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Start Fade
        /// /////////////////////////////////////////////////////////

        // Start fading
        static void StartFadeRigid(RFFadeBatch batch)
        {
            // Stop fading
            if (batch.rigid.fading.stop == true)
            {
                batch.rigid.fading.LocalReset();
                return;
            } 
            
            // Set fading
            batch.rigid.fading.state = 2;

            // Event
            batch.rigid.fading.fadingEvent.InvokeLocalEvent (batch.rigid.transform);
            RFFadingEvent.InvokeGlobalEvent (batch.rigid.transform);
            
            // Check fading
            if (batch.fadeType == FadeType.SimExclude)
                SimExclude(batch.rigid);
            else if (batch.fadeType == FadeType.FallDown)
                batch.rigid.StartCoroutine (RFFade.FallDownCor (batch.rigid));
            else if (batch.fadeType == FadeType.ScaleDown)
                batch.rigid.StartCoroutine (RFFade.ScaleDownCor (batch.rigid));
            else if (batch.fadeType == FadeType.MoveDown)
                batch.rigid.StartCoroutine (RFFade.MoveDownCor (batch.rigid));
            else if (batch.fadeType == FadeType.Destroy)
                Destroy(batch.rigid);
            else if (batch.fadeType == FadeType.SetStatic)
                SetStatic(batch.rigid);
            else if (batch.fadeType == FadeType.SetKinematic)
                SetKinematik(batch.rigid);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Rigid Instant fades
        /// /////////////////////////////////////////////////////////
        
        // Destroy gameobject
        static void Destroy(RayfireRigid scr)
        {
            scr.fading.state = 3;
            RayfireMan.DestroyFragment (scr, scr.rtP);
        }
        
        // Destroy rigidbody, keep collider
        static void SetStatic(RayfireRigid scr)
        {
            scr.fading.state = 3;
            Object.Destroy (scr.physics.rb);
            scr.physics.rb = null;
        }

        // Set rigidbody to kinematik
        static void SetKinematik(RayfireRigid scr)
        {
            scr.fading.state           = 3;
            scr.physics.rb.isKinematic = true;
        }

        // Destroy rigidbody and collider, keep object in scene
        static void SimExclude (RayfireRigid rigid)
        {
            // Set faded
            rigid.fading.state = 3;

            // Not going to be reused
            if (rigid.reset.action == RFReset.PostDemolitionType.DestroyWithDelay)
            {
                UnityEngine.Object.Destroy (rigid.physics.rb);
                UnityEngine.Object.Destroy (rigid.physics.mc);
                UnityEngine.Object.Destroy (rigid);
            }

            // Going to be reused 
            else if (rigid.reset.action == RFReset.PostDemolitionType.DeactivateToReset)
            {
                // Set kinematic
                rigid.physics.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rigid.physics.rb.isKinematic            = true;
                
                // Disable mesh collider // Null check because of Planar check fragments without collider
                if (rigid.objTp == ObjectType.Mesh && rigid.physics.mc != null)
                    rigid.physics.mc.enabled = false;
                
                // Disable cluster colliders TODO test nested cluster
                else if (rigid.objTp == ObjectType.ConnectedCluster || rigid.objTp == ObjectType.NestedCluster)
                    for (int i = 0; i < rigid.physics.cc.Count; i++)
                        rigid.physics.cc[i].enabled = false;
                
                // Stop all cors
                rigid.StopAllCoroutines();
            }
        }
        
    }
}