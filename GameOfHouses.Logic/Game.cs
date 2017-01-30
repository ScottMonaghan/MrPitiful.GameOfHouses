using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public class Game
    {
        public Game()
        {
            Players = new List<Player>();
        }
        public Guid Id { get; set; }
        public List<Player> Players { get; set; }
        public void AddPlayer(Player player)
        {
            if (!Players.Contains(player))
            {
                Players.Add(player);
                player.Game = this;
            }
        }
        public void RemovePlayer(Player player)
        {
            if (Players.Contains(player))
            {
                Players.Remove(player);
                player.Game = null;
            }
        }
    }
}
