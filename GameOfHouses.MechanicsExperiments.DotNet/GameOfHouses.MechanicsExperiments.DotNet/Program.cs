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
            //initialize world
            var game = Utility.InitializeWorld(world, rnd);

            //add players
            game.NewPlayer("Scott", "Monaghan", Sex.Male, People.Norvik, PlayerType.Live, rnd);
            game.NewPlayer("Emily", "Andersson", Sex.Female, People.Norvik, PlayerType.Live, rnd);
            game.NewPlayer("Michael", "Corley", Sex.Male, People.Norvik, PlayerType.Live, rnd);
            //game.NewPlayer("Joel", "LeFave", Sex.Male, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Rachel", "Mulready", Sex.Female, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Tim", "Randall", Sex.Male, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Tom", "Brady", Sex.Male, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Gisele", "Bundchen", Sex.Female, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Robert", "Kraft", Sex.Male, People.Sulthaen, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Susan", "Mars", Sex.Female, People.Sulthaen, PlayerType.AIAggressive, rnd);
            while (world.Year < 500)
            {
                var playersLeftToTakeTurn = game.Players.Where(p=>(p.House==null)||(p.House.Lord != null && p.House.Lord.House == p.House)).ToList();
                while (playersLeftToTakeTurn.Count() > 0)
                {
                    var nextPlayer = playersLeftToTakeTurn[rnd.Next(0, playersLeftToTakeTurn.Count())];
                    Console.WriteLine(playersLeftToTakeTurn.Count() + " players remaining.");
                    playersLeftToTakeTurn.Remove(nextPlayer);
                    nextPlayer.DoPlayerTurn(rnd);
                }
                Utility.IncrementYear(world, rnd, 5);
            }
            Console.ReadLine();
        }
    }
}
