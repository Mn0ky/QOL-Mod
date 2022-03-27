using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace QOL
{
    // From: https://forum.unity.com/threads/solved-rainbow-hue-shift-over-time-c-script.351751/#post-2277135
    class RainbowManager : MonoBehaviour
    {
        public float Speed = Plugin.configRainbowSpeed.Value;
        private SpriteRenderer[] rend1;
        private LineRenderer[] rend2;

        void Start()
        {
            Debug.Log("starting rainbow");
            var character = Helper.clientData[Helper.localNetworkPlayer.NetworkSpawnID].PlayerObject;

            rend1 = character.GetComponentsInChildren<SpriteRenderer>();
            rend2 = character.GetComponentsInChildren<LineRenderer>();

            foreach (var renderer in rend1)
            {
                renderer.color = Color.white;
            }
            foreach (var t in rend2)
            {
                t.sharedMaterial.color = Color.white;
            }
        }

        void Update()
        {
            foreach (var spriteRenderer in rend1)
            {
                spriteRenderer.material.SetColor("_Color", HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * Speed, 1), 1, 1)));
            }

            foreach (var lineRenderer in rend2)
            {
                lineRenderer.material.SetColor("_Color", HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * Speed, 1), 1, 1)));
            }
        }

        void OnDisable() //TODO: Clean this up, move functions to a helper class
        {
            Debug.Log("rainbow disabled");
            var player = Helper.localNetworkPlayer.transform.root.gameObject;

            if (Helper.customPlayerColor != new Color(1, 1, 1))
            {
                MultiplayerManagerPatches.ChangeLineRendColor(Helper.customPlayerColor, player);
                MultiplayerManagerPatches.ChangeSpriteRendColor(Helper.customPlayerColor, player);
                return;
            }

            MultiplayerManagerPatches.ChangeLineRendColor(Helper.defaultColors[Helper.localNetworkPlayer.NetworkSpawnID], player);
            MultiplayerManagerPatches.ChangeSpriteRendColor(Helper.defaultColors[Helper.localNetworkPlayer.NetworkSpawnID], player);
            MultiplayerManagerPatches.ChangeSpriteRendColor(Helper.defaultColors[Helper.localNetworkPlayer.NetworkSpawnID], player);
        }
    }
}
