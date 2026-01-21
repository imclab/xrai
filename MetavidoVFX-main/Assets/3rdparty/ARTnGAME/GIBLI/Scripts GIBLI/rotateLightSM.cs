using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
namespace Artngame.SKYMASTER
{
    public class rotateLightSM : MonoBehaviour
    {

        public float speed = 5.0f;

        private Vector3 lastMousePosition;

        void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Vector3 DTT = (lastMousePosition - (Vector3)Mouse.current.position.ReadValue())
              * speed * Time.deltaTime;

            if (Mouse.current.leftButton.isPressed &&
                Keyboard.current.leftCtrlKey.isPressed)
            {
                transform.Rotate(new Vector3(-DTT.y, -DTT.x, 0f));
            }

            lastMousePosition = Mouse.current.position.ReadValue();
#else


            Vector3 DTT = (lastMousePosition - Input.mousePosition) * speed * Time.deltaTime ;

            if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))
            {
                transform.Rotate(new Vector3(-DTT.y, -DTT.x, 0));
            }

            lastMousePosition = Input.mousePosition;
#endif

            //Vector3 DTT = (lastMousePosition - Input.mousePosition) * speed * Time.deltaTime ;

            //if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))
            //{
            //    transform.Rotate(new Vector3(-DTT.y, -DTT.x, 0));
            //}

            //lastMousePosition = Input.mousePosition;
        }
    }
}
