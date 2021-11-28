using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Steamworks;

namespace QOL
{
    class MatchmakingHandlerPatches
    {
        public static void Patch(Harmony harmonyInstance) // MatchmakingHandler method to patch with the harmony instance
        {
            var ClientInitLobbyAndOwnerMethod = AccessTools.Method(typeof(MatchmakingHandler), "ClientInitLobbyAndOwner");
            var ClientInitLobbyAndOwnerMethodPostfix = new HarmonyMethod(typeof(MatchmakingHandlerPatches).GetMethod(nameof(MatchmakingHandlerPatches.ClientInitLobbyAndOwnerMethodPostfix))); // Patches ClientInitLobbyAndOwnerMethod with postfix method
            harmonyInstance.Patch(ClientInitLobbyAndOwnerMethod, postfix: ClientInitLobbyAndOwnerMethodPostfix);

            var OnLobbyJoinRequestMethod = AccessTools.Method(typeof(MatchmakingHandler), "OnLobbyJoinRequest");
            var OnLobbyJoinRequestMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerPatches).GetMethod(nameof(MatchmakingHandlerPatches.OnLobbyJoinRequestMethodPrefix))); // Patches SyncClientChat with prefix method
            harmonyInstance.Patch(OnLobbyJoinRequestMethod, prefix: OnLobbyJoinRequestMethodPrefix);
        }
        public static void ClientInitLobbyAndOwnerMethodPostfix(ref CSteamID lobby) // Sets lobbyID as the ID of the current lobby for easy access
        {
            Debug.Log("Matchmaking lobbyID: " + lobby); 
            Helper.lobbyID = lobby;
        }

        public static bool OnLobbyJoinRequestMethodPrefix(ref GameLobbyJoinRequested_t param, MatchmakingHandler __instance)
        {
            Debug.Log("calling joinrequest method!");
            if (!param.m_steamIDFriend.IsValid())
            {
                Debug.Log("steamfriend is invalid, directly joining lobby");
                specificJoinMethod.Invoke(__instance, new object[] { param.m_steamIDLobby });
                return false;
            }
            Debug.Log("steamfriendfriend is valid: " + param.m_steamIDFriend);
            return true;
        }
        private static readonly MethodInfo specificJoinMethod = typeof(MatchmakingHandler).GetMethod("JoinSpecificServer", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
