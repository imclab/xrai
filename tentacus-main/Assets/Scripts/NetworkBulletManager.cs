// SPDX-FileCopyrightText: Copyright 2023 Reality Design Lab <dev@reality.design>
// SPDX-FileContributor: Yuchen Zhang <yuchenz27@outlook.com>
// SPDX-FileContributor: Botao Amber Hu <botao.a.hu@gmail.com>
// SPDX-License-Identifier: MIT

using UnityEngine;
using Unity.Netcode;
using HoloKit;

namespace HoloKit.ColocatedMultiplayerBoilerplate
{
    public enum RelocalizationMode
    {
        ImageTrackingRelocalizatioin = 0,
    }

    public class NetworkBulletManager : NetworkBehaviour
    {
        [SerializeField] private RelocalizationMode m_RelocalizationMode;

        [SerializeField] private NetworkBulletController m_BulletPrefab;

        [SerializeField] private Vector3 m_SpawnOffset = new(0f, 0f, 0.3f);

        private Transform m_CenterEyePose;

        private void Start()
        {
            m_CenterEyePose = FindFirstObjectByType<HoloKitCameraManager>().CenterEyePose;

        }

        public void SpawnBullet()
        {
            SpawnBulletServerRpc(m_CenterEyePose.position, m_CenterEyePose.rotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnBulletServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams serverRpcParams = default)
        {
            if (m_RelocalizationMode == RelocalizationMode.ImageTrackingRelocalizatioin)
            {
                var bullet = Instantiate(m_BulletPrefab, position + rotation * m_SpawnOffset, rotation);
                bullet.GetComponent<NetworkObject>().SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);
                return;
            }
        }
    }
}
