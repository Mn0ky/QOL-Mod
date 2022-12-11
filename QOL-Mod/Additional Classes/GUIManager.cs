using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Steamworks;
using UnityEngine;

namespace QOL;

public class GUIManager : MonoBehaviour
{
    private bool _mShowGlobalStats;
    private bool _mShowMenu;
    private bool _mShowStatMenu;
    private bool _mStatsShown;
        
    public static float[] QolMenuPos;
    public static float[] StatMenuPos;

    private Rect _menuRect = new(QolMenuPos[0], QolMenuPos[1], 350f, 375f);
    private Rect _statMenuRect = new(StatMenuPos[0], StatMenuPos[1], 510f, 350f);
    private Rect _globalStatMenuRect = new(StatMenuPos[0], StatMenuPos[1], 650f, 350f);

    private readonly string[] _playerStats = new string[4];

    private JSONNode _globalUserStats;
    private string _playerNamesStr = "Players in Room: \n";
    private string _lobbyHost;

    private KeyCode _qolMenuKey1;
    private KeyCode _qolMenuKey2;
    private bool _singleMenuKey;

    private KeyCode _statWindowKey1;
    private KeyCode _statWindowKey2;
    private bool _singleStatKey;

    private void Start() => Debug.Log("Started GUI in GUIManager!");
        
    private void Awake()
    {
        _qolMenuKey1 = ConfigHandler.GetEntry<KeyboardShortcut>("QOLMenuKeybind").MainKey;
        _qolMenuKey2 = ConfigHandler.GetEntry<KeyboardShortcut>("QOLMenuKeybind").Modifiers.LastOrDefault();
        if (_qolMenuKey2 == KeyCode.None) _singleMenuKey = true;

        _statWindowKey1 = ConfigHandler.GetEntry<KeyboardShortcut>("StatWindowKeybind").MainKey;
        _statWindowKey2 = ConfigHandler.GetEntry<KeyboardShortcut>("StatWindowKeybind").Modifiers.LastOrDefault();
        if (_statWindowKey2 == KeyCode.None) _singleStatKey = true;
    }

    private void Update()
    {
        if (Input.GetKey(_qolMenuKey1) && Input.GetKeyDown(_qolMenuKey2) || Input.GetKeyDown(_qolMenuKey1) && _singleMenuKey)
        {
            Debug.Log("Trying to open GUI menu!");

            _mShowMenu = !_mShowMenu;
            _playerNamesStr = "";

            foreach (var player in FindObjectsOfType<NetworkPlayer>())
            {
                var str = string.Concat(
                    "[",
                    Helper.GetColorFromID(player.NetworkSpawnID),
                    "] ",
                    Helper.GetPlayerName(Helper.GetSteamID(player.NetworkSpawnID)));
                    
                _playerNamesStr +=  "\n" + str;
            }

            _lobbyHost = Helper.GetPlayerName(MatchmakingHandler.Instance.LobbyOwner);
        }

        if (Input.GetKey(_statWindowKey1) && Input.GetKeyDown(_statWindowKey2) || Input.GetKeyDown(_statWindowKey1) && _singleStatKey)
        {
            _mStatsShown = true;
            _mShowStatMenu = !_mShowStatMenu;
        }

        if (_mShowStatMenu && _mStatsShown)
        {
            foreach (var stat in FindObjectsOfType<CharacterStats>())
            {
                switch (stat.GetComponentInParent<NetworkPlayer>().NetworkSpawnID)
                {
                    case 0:
                        _playerStats[0] = stat.GetString();
                        Debug.Log(_playerStats[0]);
                        break;
                    case 1:
                        _playerStats[1] = stat.GetString();
                        Debug.Log(_playerStats[1]);
                        break;
                    case 2:
                        _playerStats[2] = stat.GetString();
                        Debug.Log(_playerStats[2]);
                        break;
                    default:
                        _playerStats[3] = stat.GetString();
                        Debug.Log(_playerStats[3]);
                        break;
                }
            }

            Debug.Log("show stats being set to false via update");
            _mStatsShown = false;
        }

    }
    public void OnGUI() 
    {
        if (_mShowMenu)
            _menuRect = GUILayout.Window(100, _menuRect, KickWindow,
                $"<color=red>Monky's QOL Menu</color>\t[v{Plugin.VersionNumber}]");
        if (_mShowStatMenu) 
            _statMenuRect = GUILayout.Window(101, _statMenuRect, StatWindow, "Stat Menu");
        if (_mShowGlobalStats)
            _globalStatMenuRect = GUILayout.Window(102, _globalStatMenuRect, GlobalStatWindow, "Global Stats Menu");
    }
        
    private void KickWindow(int window)
    {
        var normAlignment = GUI.skin.label.alignment;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("<color=#228f69>(Click To Drag)</color>");
        GUI.skin.label.alignment = normAlignment;

        GUILayout.Label("Host: " + _lobbyHost);
        GUILayout.Label(_playerNamesStr);

        if (GUI.Button(new Rect(2f, 300f, 80f, 30f), "<color=yellow>HP Yellow</color>"))
        {
            var yellowHp = Helper.GetPlayerHp(0);
            Helper.SendPublicOutput(Helper.GetColorFromID(0) + " HP: " + yellowHp);
        }

        if (GUI.Button(new Rect(89f, 300f, 80f, 30f), "<color=blue>HP Blue</color>"))
        {
            var yellowHp = Helper.GetPlayerHp(1);
            Helper.SendPublicOutput(Helper.GetColorFromID(0) + " HP: " + yellowHp);
        }

        if (GUI.Button(new Rect(176f, 300f, 80f, 30f), "<color=red>HP Red</color>"))
        {
            var yellowHp = Helper.GetPlayerHp(2);
            Helper.SendPublicOutput(Helper.GetColorFromID(0) + " HP: " + yellowHp);
        }

        if (GUI.Button(new Rect(263f, 300f, 80f, 30f), "<color=green>HP Green</color>"))
        {
            var yellowHp = Helper.GetPlayerHp(3);
            Helper.SendPublicOutput(Helper.GetColorFromID(0) + " HP: " + yellowHp);
        }

        if (GUI.Button(new Rect(3f, 335f, 80f, 30f), "Lobby Link"))
        {
            GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
            ChatCommands.CmdDict["invite"].Execute();
        }

        if (GUI.Button(new Rect(133f, 265f, 80f, 30f), "Stat Menu"))
        {
            _mShowStatMenu = !_mShowStatMenu;
            _mStatsShown = true;
        }

        if (GUI.Button(new Rect(263f, 265f, 80f, 30f), "Shrug"))
            Helper.SendPublicOutput($" \u00af\\_{ConfigHandler.GetEntry<string>("ShrugEmoji")}_/\u00af");
            
        if (GUI.Button(new Rect(2f, 265f, 80f, 30f), "Help"))
            SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
            
        if (GUI.Button(new Rect(133f, 335f, 80f, 30f), "Private")) 
            ChatCommands.CmdDict["private"].Execute();
            
        if (GUI.Button(new Rect(263f, 335f, 80f, 30f), "Public")) 
            ChatCommands.CmdDict["public"].Execute();

        ChatCommands.CmdDict["gg"].IsEnabled = GUI.Toggle(new Rect(6f, 188f, 100f, 30f), 
            ChatCommands.CmdDict["gg"].IsEnabled, "AutoGG");
            
        ChatCommands.CmdDict["translate"].IsEnabled = GUI.Toggle(new Rect(100f, 220f, 106f, 30f), 
            ChatCommands.CmdDict["translate"].IsEnabled, "AutoTranslations");
            
        Helper.TMPText.richText = GUI.Toggle(new Rect(6f, 220f, 115f, 30f), 
            Helper.TMPText.richText, "RichText");
            
        ChatCommands.CmdDict["uncensor"].IsEnabled = GUI.Toggle(new Rect(100, 188f, 150f, 30f), 
            ChatCommands.CmdDict["uncensor"].IsEnabled, "ChatCensorshipBypass");

        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }

    private void StatWindow(int window)
    {
        var normAlignment = GUI.skin.label.alignment;
        GUI.skin.label.alignment = TextAnchor.UpperCenter;
        GUI.skin.button.alignment = TextAnchor.LowerCenter;
        GUILayout.Label("<color=#228f69>(Click To Drag)</color>");

        if (GUI.Button(new Rect(237.5f, 310f, 80f, 25f), "Close")) 
            _mShowStatMenu = !_mShowStatMenu;
        if (GUI.Button(new Rect(150f, 310f, 85f, 25f), "Global Stats"))
        {
            _mShowStatMenu = !_mShowStatMenu;
            _globalStatMenuRect.x = _statMenuRect.x;
            _globalStatMenuRect.y = _statMenuRect.y;
            _mShowGlobalStats = true;
                
            _globalUserStats = Plugin.StatsFileExists ? JSONNode.Parse(File.ReadAllText(Plugin.StatsPath)) : null;
        }

        GUI.skin.label.alignment = normAlignment;

        GUILayout.BeginHorizontal();
        for (ushort i = 0; i < _playerStats.Length; i++)
        {
            var stat = _playerStats[i];
            var color = Helper.GetColorFromID(i);

            GUILayout.BeginVertical();
            GUILayout.Label("<color=" + color + ">" + color + "</color>");
            GUILayout.Label(stat);  
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }
        
    private void GlobalStatWindow(int window)
    {
        var normAlignment = GUI.skin.label.alignment;
        GUI.skin.label.alignment = TextAnchor.UpperCenter;
        GUI.skin.button.alignment = TextAnchor.LowerCenter;
        GUILayout.Space(20f);
        GUILayout.Label("<color=#228f69>(Click To Drag)</color>");

        if (GUI.Button(new Rect(_globalStatMenuRect.width / 2f - 40, _globalStatMenuRect.height - 50, 80, 25), 
                "Close"))
        {
            _mShowGlobalStats = !_mShowGlobalStats;
            _globalUserStats = null;
        }

        GUI.skin.label.alignment = normAlignment;

        if (_globalUserStats != null)
        {
            const int maxStatsPerColumn = 6;
            var currentStatsPerRow = 0;
                
            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            foreach (var stat in _globalUserStats)
            {
                if (currentStatsPerRow == maxStatsPerColumn)
                {
                    currentStatsPerRow = 0;
                    GUILayout.EndVertical();
                    GUILayout.Space(30f);
                }
                    
                if (currentStatsPerRow == 0)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Space(20);
                }

                GUILayout.Label(stat.Key + ": " + stat.Value);
                currentStatsPerRow++;
            }
            GUILayout.EndHorizontal();
        }    
            
        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }
}