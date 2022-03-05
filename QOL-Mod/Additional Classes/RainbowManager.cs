using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QOL
{
    // From: https://forum.unity.com/threads/solved-rainbow-hue-shift-over-time-c-script.351751/#post-2277135
    class RainbowManager : MonoBehaviour
    {
        public float Speed = 1;
        private SpriteRenderer[] rend1;
        private LineRenderer[] rend2;

        void Start()
        {
            var character = Helper.localNetworkPlayer.transform.root.gameObject;

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
            //spriteRenderer.GetComponentInParent<SetColorWhenDamaged>().startColor = colorWanted;
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
    }
}
