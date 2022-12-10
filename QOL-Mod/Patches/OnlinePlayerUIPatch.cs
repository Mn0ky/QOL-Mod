using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace QOL;

class OnlinePlayerUIPatch
{
    public static void Patch(Harmony harmonyInstance)
    {
        var updateMethod = AccessTools.Method(typeof(OnlinePlayerUI), "Update");
        var updateMethodPrefix = new HarmonyMethod(typeof(OnlinePlayerUIPatch)
            .GetMethod(nameof(UpdateMethodPrefix)));
        harmonyInstance.Patch(updateMethod, prefix: updateMethodPrefix);

        var populateMethod = AccessTools.Method(typeof(OnlinePlayerUI), "Populate");
        var populateMethodPrefix = new HarmonyMethod(typeof(OnlinePlayerUIPatch)
            .GetMethod(nameof(PopulateMethodPrefix)));
        harmonyInstance.Patch(populateMethod, prefix: populateMethodPrefix);
    }

    public static bool UpdateMethodPrefix(ref bool ___mIsStaying, ref ConnectedClientData[] ___mClients,
        ref TextMeshProUGUI[] ___mPlayerTexts)
    {
        if (!___mIsStaying) return false;

        for (var i = 0; i < ___mClients.Length; i++)
        {
            var client = ___mClients[i];
                
            if (client == null || !client.ClientID.IsValid() || client.PlayerObject == null) 
                ___mPlayerTexts[i].text = "";

            else
            {
                if (Helper.IsCustomName && i == Helper.localNetworkPlayer.NetworkSpawnID) 
                    ___mPlayerTexts[i].text = Plugin.ConfigCustomName.Value;
                    
                else ___mPlayerTexts[i].text = client.PlayerName;

                var component = ___mPlayerTexts[i].GetComponent<CodeStateAnimation>();
                var gameObject = client.PlayerObject.GetComponentInChildren<Torso>().gameObject;
                component.state1 = true;

                if (gameObject == null) break;
                component.transform.position = gameObject.transform.position + Vector3.up * 1.5f;

                if (gameObject == null) component.state1 = false;
            }
        }

        return false;
    }

    public static bool PopulateMethodPrefix(ref bool ___mIsStaying, ref ConnectedClientData[] ___mClients,
        ref TextMeshProUGUI[] ___mPlayerTexts, OnlinePlayerUI __instance)
    {
        if (___mClients == null) return false;

        ___mIsStaying = false;
        for (var i = 0; i < ___mClients.Length; i++)
        {
            var client = ___mClients[i];
                
            if (client == null || !client.ClientID.IsValid() || client.PlayerObject == null) 
                ___mPlayerTexts[i].text = "";
                
            else
            {
                if (Helper.IsCustomName && i == Helper.localNetworkPlayer.NetworkSpawnID) 
                    ___mPlayerTexts[i].text = Plugin.ConfigCustomName.Value;

                else ___mPlayerTexts[i].text = client.PlayerName + client.Ping;

                var showTextMethod = AccessTools.Method(typeof(OnlinePlayerUI), "ShowText");

                __instance.StartCoroutine((IEnumerator) showTextMethod.Invoke(__instance,
                    new object[]
                    {
                        ___mPlayerTexts[i].GetComponent<CodeStateAnimation>(),
                        client.PlayerObject.GetComponentInChildren<Torso>().gameObject
                    }));
            }
        }

        return false;
    }
}