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
                    FullColor = "Yellow";
                    _id = 0;
                    break;
                case "blue" or "b":
                    FullColor = "Blue";
                    _id = 1;
                    break;
                case "red" or "r":
                    FullColor = "Red";
                    _id = 2;
                    break;
                case "green" or "g":
                    FullColor = "Green";
                    _id = 3;
                    break;
                default:
                    FullColor = "Unknown";
                    break;
            }

            Stats = Helper.GetNetworkPlayer(_id).GetComponentInParent<CharacterStats>();
        }

        private readonly ushort _id;

        public CharacterStats Stats { get; }
        public string FullColor { get; }
    }
}
