using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QOL
{
    public class PlayerHP
    {
        public PlayerHP(string playerColor)
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

            HP = Helper.GetNetworkPlayer(_id).GetComponentInChildren<HealthHandler>().health + "%";
        }

        private readonly ushort _id;
        public string HP { get; }
        public string FullColor { get; }
    }
}
