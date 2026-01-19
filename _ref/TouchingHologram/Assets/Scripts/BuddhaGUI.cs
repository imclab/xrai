// SPDX-FileCopyrightText: Copyright 2023 Holo Interactive <dev@holoi.com>
// SPDX-FileContributor: Yuchen Zhang <yuchen@holoi.com>
// SPDX-License-Identifier: MIT
using UnityEngine;
using UnityEngine.UI;
using HoloInteractive.XR.HoloKit;

namespace HoloInteractive.CourseSample.InteractWithBuddha
{
    public class BuddhaGUI : MonoBehaviour
    {
        [SerializeField] Text m_BtnText;

        private void Start()
        {
            // we do not need to lock the screen orientation in this scene:
            // Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        // This function used to switch the render mode between Mono(Screen AR) and Stereo(HoloKit AR)
        public void SwitchRenderMode()
        {
            var holokitCamera = FindObjectOfType<HoloKitCameraManager>();
            holokitCamera.ScreenRenderMode = holokitCamera.ScreenRenderMode == ScreenRenderMode.Mono ? ScreenRenderMode.Stereo : ScreenRenderMode.Mono;
            m_BtnText.text = holokitCamera.ScreenRenderMode == ScreenRenderMode.Mono ? "Stereo" : "Mono";
        }
    }
}