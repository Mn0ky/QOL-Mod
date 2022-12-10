using System.Reflection;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace QOL;

class MatchmakingHandlerPatches
{
    public static void Patch(Harmony harmonyInstance) // MatchmakingHandler method to patch with the harmony instance
    {
        var clientInitLobbyAndOwnerMethod = AccessTools.Method(typeof(MatchmakingHandler), "ClientInitLobbyAndOwner");
        var clientInitLobbyAndOwnerMethodPostfix = new HarmonyMethod(typeof(MatchmakingHandlerPatches)
            .GetMethod(nameof(ClientInitLobbyAndOwnerMethodPostfix)));
        harmonyInstance.Patch(clientInitLobbyAndOwnerMethod, postfix: clientInitLobbyAndOwnerMethodPostfix);

        var onLobbyJoinRequestMethod = AccessTools.Method(typeof(MatchmakingHandler), "OnLobbyJoinRequest");
        var onLobbyJoinRequestMethodPrefix = new HarmonyMethod(typeof(MatchmakingHandlerPatches)
            .GetMethod(nameof(OnLobbyJoinRequestMethodPrefix)));
        harmonyInstance.Patch(onLobbyJoinRequestMethod, prefix: onLobbyJoinRequestMethodPrefix);
    }
        
    // Sets lobbyID as the ID of the current lobby for easy access
    public static void ClientInitLobbyAndOwnerMethodPostfix(ref CSteamID lobby)
    {
        Debug.Log("Matchmaking lobbyID: " + lobby); 
        Helper.lobbyID = lobby;
    }

    public static bool OnLobbyJoinRequestMethodPrefix(ref GameLobbyJoinRequested_t param,
        MatchmakingHandler __instance)
    {
        Debug.Log("calling JoinRequest method!");
        if (!param.m_steamIDFriend.IsValid())
        {
            Debug.Log("steamfriend is invalid, directly joining lobby");
            SpecificJoinMethod.Invoke(__instance, new object[] { param.m_steamIDLobby });
            return false;
        }
            
        Debug.Log("steamfriend is valid: " + param.m_steamIDFriend);
        return true;
    }

    private static readonly MethodInfo SpecificJoinMethod =
        AccessTools.Method(typeof(MatchmakingHandler), "JoinSpecificServer");
}