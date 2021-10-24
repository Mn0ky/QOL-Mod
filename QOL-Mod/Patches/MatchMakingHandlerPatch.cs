using HarmonyLib;
using UnityEngine;
using Steamworks;

namespace QOL
{
    class MatchmakingHandlerPatch
    {
        public static void Patch(Harmony harmonyInstance) // MatchmakingHandler method to patch with the harmony instance
        {
            var ClientInitLobbyAndOwnerMethod = AccessTools.Method(typeof(MatchmakingHandler), "ClientInitLobbyAndOwner");
            var ClientInitLobbyAndOwnerMethodPostfix = new HarmonyMethod(typeof(MatchmakingHandlerPatch).GetMethod(nameof(MatchmakingHandlerPatch.ClientInitLobbyAndOwnerMethodPostfix))); // Patches ClientInitLobbyAndOwnerMethod with postfix method
            harmonyInstance.Patch(ClientInitLobbyAndOwnerMethod, postfix: ClientInitLobbyAndOwnerMethodPostfix);
        }
        public static void ClientInitLobbyAndOwnerMethodPostfix(ref CSteamID lobby) // Sets lobbyID as the ID of the current lobby for easy access
        {
            Debug.Log("Matchmaking lobbyID: " + lobby);
            MatchmakingHandlerPatch.lobbyID = lobby;
        }
        public static CSteamID lobbyID; // The ID of the current lobby
    }
}
