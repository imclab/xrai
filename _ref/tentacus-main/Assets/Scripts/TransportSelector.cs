// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public enum AvailableTransport
    {
        Router = 0
    }

    public class TransportSelector : MonoBehaviour
    {
        [SerializeField] private UnityTransport m_UnityTransport;

        private AvailableTransport m_CurrentTransport;

        private void Start()
        {
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = m_UnityTransport;
            m_CurrentTransport = AvailableTransport.Router;
        }
    }
}
