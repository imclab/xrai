using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
namespace Artngame.GIBLI.Utilities
{
    /// <summary>
    /// A simple free camera to be added to a Unity game object.
    /// 
    /// Keys:
    ///	wasd / arrows	- movement
    ///	q/e 			- up/down (local space)
    ///	r/f 			- up/down (world space)
    ///	pageup/pagedown	- up/down (world space)
    ///	hold shift		- enable fast movement mode
    ///	right mouse  	- enable free look
    ///	mouse			- free look / rotation
    ///     
    /// </summary>
    public class FreeCam : MonoBehaviour
    {
        /// <summary>
        /// Normal speed of camera movement.
        /// </summary>
        public float movementSpeed = 10f;

        /// <summary>
        /// Speed of camera movement when shift is held down,
        /// </summary>
        public float fastMovementSpeed = 100f;

        /// <summary>
        /// Sensitivity for free look.
        /// </summary>
        public float freeLookSensitivity = 3f;

        /// <summary>
        /// Amount to zoom the camera when using the mouse wheel.
        /// </summary>
        public float zoomSensitivity = 10f;

        /// <summary>
        /// Amount to zoom the camera when using the mouse wheel (fast mode).
        /// </summary>
        public float fastZoomSensitivity = 50f;

        /// <summary>
        /// Set to true when free looking (on right mouse button).
        /// </summary>
        private bool looking = false;

        void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            UpdateNEW();
#else
            UpdateOLD();
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        void UpdateNEW()
        {

            // --- Fast mode ---
            var fastMode =
                Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed ||
                 Keyboard.current.rightShiftKey.isPressed);

            var movementSpeedA = fastMode ? fastMovementSpeed : movementSpeed;

            // --- Horizontal movement ---
            if (Keyboard.current != null &&
                (Keyboard.current.aKey.isPressed ||
                 Keyboard.current.leftArrowKey.isPressed))
            {
                transform.position += -transform.right * movementSpeedA * Time.deltaTime;
            }

            if (Keyboard.current != null &&
                (Keyboard.current.dKey.isPressed ||
                 Keyboard.current.rightArrowKey.isPressed))
            {
                transform.position += transform.right * movementSpeedA * Time.deltaTime;
            }

            // --- Forward / Back ---
            if (Keyboard.current != null &&
                (Keyboard.current.wKey.isPressed ||
                 Keyboard.current.upArrowKey.isPressed))
            {
                transform.position += transform.forward * movementSpeedA * Time.deltaTime;
            }

            if (Keyboard.current != null &&
                (Keyboard.current.sKey.isPressed ||
                 Keyboard.current.downArrowKey.isPressed))
            {
                transform.position += -transform.forward * movementSpeedA * Time.deltaTime;
            }

            // --- Local up / down ---
            if (Keyboard.current != null && Keyboard.current.qKey.isPressed)
            {
                transform.position += transform.up * movementSpeedA * Time.deltaTime;
            }

            if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
            {
                transform.position += -transform.up * movementSpeedA * Time.deltaTime;
            }

            // --- World up / down ---
            if (Keyboard.current != null &&
                (Keyboard.current.rKey.isPressed ||
                 Keyboard.current.pageUpKey.isPressed))
            {
                transform.position += Vector3.up * movementSpeedA * Time.deltaTime;
            }

            if (Keyboard.current != null &&
                (Keyboard.current.fKey.isPressed ||
                 Keyboard.current.pageDownKey.isPressed))
            {
                transform.position += -Vector3.up * movementSpeedA * Time.deltaTime;
            }

            // --- Free look ---
            if (looking && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();

                float newRotationX =
                    transform.localEulerAngles.y +
                    mouseDelta.x * freeLookSensitivity * 0.25f;

                float newRotationY =
                    transform.localEulerAngles.x -
                    mouseDelta.y * freeLookSensitivity * 0.25f;

                transform.localEulerAngles =
                    new Vector3(newRotationY, newRotationX, 0f);
            }

            // --- Mouse wheel zoom ---
            if (Mouse.current != null)
            {
                float axis = Mouse.current.scroll.ReadValue().y;

                if (axis != 0f)
                {
                    var zoomSensitivityA =
                        fastMode ? fastZoomSensitivity : zoomSensitivity;

                    transform.position +=
                        transform.forward * axis * zoomSensitivityA;
                }
            }

            // --- Right mouse button look toggle ---
            if (Mouse.current != null &&
                Mouse.current.rightButton.wasPressedThisFrame)
            {
                StartLooking();
            }
            else if (Mouse.current != null &&
                     Mouse.current.rightButton.wasReleasedThisFrame)
            {
                StopLooking();
            }

        }
        #else

        void UpdateOLD()
        {
            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.Q))
            {
                transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.E))
            {
                transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
            {
                transform.position = transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
            {
                transform.position = transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
            }

            if (looking)
            {
                float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
                float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            }

            float axis = Input.GetAxis("Mouse ScrollWheel");
            if (axis != 0)
            {
                var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
                transform.position = transform.position + transform.forward * axis * zoomSensitivity;
            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                StartLooking();
            }
            else if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                StopLooking();
            }
        }
#endif

        void OnDisable()
        {
            StopLooking();
        }

        /// <summary>
        /// Enable free looking.
        /// </summary>
        public void StartLooking()
        {
            looking = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// Disable free looking.
        /// </summary>
        public void StopLooking()
        {
            looking = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}