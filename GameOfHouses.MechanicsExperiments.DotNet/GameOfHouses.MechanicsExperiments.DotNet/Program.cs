using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameOfHouses.Logic;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var rnd = new Random();
            var world = new World(rnd);
            var player = new Player();
            world.Player = player;
            Game game;
            if (Constants.PLAY_INTRO)
            {
#pragma warning disable CS0162 // Unreachable code detected
                game = Utility.InitializeWorldWithIntro(world, rnd, player);
#pragma warning restore CS0162 // Unreachable code detected
            } else
            {
                game = Utility.InitializeWorld(world, rnd, player);
            }

            while (world.Year < 500)
            {
                var playersLeftToTakeTurn = game.Players.ToList();
                while (playersLeftToTakeTurn.Count() > 0)
                {
                    var nextPlayer = playersLeftToTakeTurn[rnd.Next(0, playersLeftToTakeTurn.Count())];
                    //Console.WriteLine(playersLeftToTakeTurn.Count() + " players remaining.");
                    playersLeftToTakeTurn.Remove(nextPlayer);
                    nextPlayer.DoPlayerTurn(rnd);
                }
                Utility.IncrementYear(world, rnd);
            }
            Console.ReadLine();
        }
    }
}
