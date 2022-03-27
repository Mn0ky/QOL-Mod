using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QOL
{
    // UNUSED, left for reference purposes
    class SceneManagerPatch
    {
        public static void Patch(Harmony harmonyInstance) // GameManager methods to patch with the harmony __instance
        {
            MethodInfo LoadSceneMethod = typeof(SceneManager).GetMethod("LoadScene",  bindingAttr: BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, new[] {typeof(int), typeof(LoadSceneMode)}, null); // Gets MethodInfo for LoadScene()
            var LoadSceneMethodPostfix = new HarmonyMethod(typeof(SceneManagerPatch).GetMethod(nameof(SceneManagerPatch.LoadSceneMethodPostfix))); // Patches LoadScene(int index) with postfix method
            harmonyInstance.Patch(LoadSceneMethod, postfix: LoadSceneMethodPostfix);
        }

        public static void LoadSceneMethodPostfix(ref int sceneBuildIndex)
        {
            Debug.Log("index of scene: " + sceneBuildIndex);
            if (sceneBuildIndex == 0) Plugin.InitModText();
        }
    }
}
    