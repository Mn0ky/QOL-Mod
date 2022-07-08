using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QOL
{
    public class PlayerStat
    {
        public PlayerStat(string playerColor)
        {
            switch (playerColor)
            {
                case "yellow" or "y":
                    _fullColor = "Yellow";
                    _id = 0;
                    break;
                case "blue" or "b":
                    _fullColor = "Blue";
                    _id = 1;
                    break;
                case "red" or "r":
                    _fullColor = "Red";
                    _id = 2;
                    break;
                case "green" or "g":
                    _fullColor = "Green";
                    _id = 3;
                    break;
                default:
                    _fullColor = "Unknown";
                    break;
            }

            _stats = Helper.GetNetworkPlayer(_id).GetComponentInParent<CharacterStats>();
        }

        private readonly string _fullColor;
        private readonly ushort _id;
        private readonly CharacterStats _stats;

        public CharacterStats Stats { get => _stats; }
        public string FullColor { get => _fullColor; }
    }
}
