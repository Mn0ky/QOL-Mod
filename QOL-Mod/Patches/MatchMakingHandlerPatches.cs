using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Steamworks;

namespace QOL
{
    class MatchmakingHandlerPatch
    {
        public static void Patch(Harmony harmonyInstance) // MatchmakingHandler method to patch with the harmony instance
        {
            var ClientInitLobbyAndOwnerMethod = AccessTools.Method(typeof(MatchmakingHandler), "ClientInitLobbyAndOwner");
            var ClientInitLobbyAndOwnerMethodPostfix = new HarmonyMethod(typeof(MatchmakingHandlerPatch).GetMethod(nameof(MatchmakingHandlerPatch.ClientInitLobbyAndOwnerMethodPostfix))); // Patches OnServerJoinedMethod with postfix method
            harmonyInstance.Patch(ClientInitLobbyAndOwnerMethod, postfix: ClientInitLobbyAndOwnerMethodPostfix);
        }
        public static void ClientInitLobbyAndOwnerMethodPostfix(ref CSteamID lobby)
        {
            // MatchmakingHandler matchmaking = Traverse.Create(__instance).Field("mMatchmakingHandler").GetValue() as MatchmakingHandler;
            // string lobby = Traverse.Create(matchmaking).Field("m_Lobby").GetValue() as string;
            // MultiplayerManagerPatches.lobbyID = new CSteamID(ulong.Parse(lobby));
            Debug.Log("matchmaking lobbyid: " + lobby);
            MatchmakingHandlerPatch.lobbyID = lobby;
        }
        public static CSteamID lobbyID;
    }
}
