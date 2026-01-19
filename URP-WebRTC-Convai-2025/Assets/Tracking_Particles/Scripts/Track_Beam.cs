using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tractorbeam
{


    public class Track_Beam : MonoBehaviour
    {

        GameObject targetObject = null;

        public GameObject beamSource;
        public GameObject beamTarget;
        public float rotationSpeed = 1.0f;


        // Use this for initialization
        void Start()
        {

            targetObject = beamTarget;

        }


        // Update is called once per frame
        void Update()
        {

            // Beam tracking - Get position of target object
            Vector3 targetPosition = targetObject.transform.position;

            // Beam tracking - Ccalculate rotation to be done
            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);


        }

    }

}