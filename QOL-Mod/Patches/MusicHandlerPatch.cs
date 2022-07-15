using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace QOL
{
    class MusicHandlerPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var playNextMethod = AccessTools.Method(typeof(MusicHandler), "PlayNext");
            var playNextMethodPrefix = new HarmonyMethod(typeof(MusicHandlerPatch).GetMethod(nameof(PlayNextMethodPrefix)));
            harmonyInstance.Patch(playNextMethod, prefix: playNextMethodPrefix);
        }

        public static bool PlayNextMethodPrefix(ref AudioSource ___au)
        {
            if (Helper.IsSongLoop)
            {
                ___au.Play();
                return false;
            }

            return true;
        }
    }       
}
