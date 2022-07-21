using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace QOL
{
    class RainbowManager : MonoBehaviour
    {
        private readonly float _speed = Plugin.ConfigRainbowSpeed.Value;
        private SpriteRenderer[] _rend1;
        private LineRenderer[] _rend2;

        void Start()
        {
            Debug.Log("starting rainbow");
            var character = Helper.localNetworkPlayer.transform.root.gameObject;

            _rend1 = character.GetComponentsInChildren<SpriteRenderer>();
            _rend2 = character.GetComponentsInChildren<LineRenderer>();

            foreach (var renderer in _rend1) renderer.color = Color.white;
            foreach (var t in _rend2) t.sharedMaterial.color = Color.white;
        }

        void Update()
        {
            // From: https://forum.unity.com/threads/solved-rainbow-hue-shift-over-time-c-script.351751/#post-2277135
            var rbColor = HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * _speed, 1), 1, 1));

            // USE "sharedMaterial" property and not "material" property so new instances aren't created every time!!
            foreach (var spriteRenderer in _rend1) spriteRenderer.color = rbColor;
            foreach (var lineRenderer in _rend2) lineRenderer.sharedMaterial.color = rbColor;
        }

        void OnDisable()
        {
            Debug.Log("rainbow disabled");
            var player = Helper.localNetworkPlayer.transform.root.gameObject;

            if (Helper.IsCustomPlayerColor)
            {
                ResetPlayerColor(Helper.CustomPlayerColor, player);
                return;
            }

            ResetPlayerColor(Plugin.DefaultColors[Helper.localNetworkPlayer.NetworkSpawnID], player);
        }

        void ResetPlayerColor(Color color, GameObject playerObj)
        {
            MultiplayerManagerPatches.ChangeLineRendColor(color, playerObj);
            MultiplayerManagerPatches.ChangeSpriteRendColor(color, playerObj);
        }
    }
}
