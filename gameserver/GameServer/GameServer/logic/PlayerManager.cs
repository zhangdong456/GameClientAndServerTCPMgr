using System;
using System.Collections.Generic;

public class PlayerManager
{
        private static Dictionary<string, Player> players = new Dictionary<string, Player>();

        public static bool isOnLine(string id)
        {
                return players.ContainsKey(id);
        }

        public static Player GetPlayer(string id)
        {
                if (players.ContainsKey(id))
                {
                        return players[id];
                }

                return null;
        }

        public static void AddPlayer(string id,Player player)
        {
                if (players.ContainsKey(id))
                {
                        return;
                }
                players.Add(id,player);
        }

        public static void RemovePlayer(string id)
        {
                if (players.ContainsKey(id))
                        players.Remove(id);
        }
}