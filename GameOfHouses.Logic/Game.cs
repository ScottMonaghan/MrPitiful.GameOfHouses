using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public class Game
    {
        private World _world;
        public Game()
        {
            Id = Guid.NewGuid();
            Players = new List<Player>();
        }
        public Game(Random rnd):this()
        {
            Random = rnd;
        }
        public Random Random { get; set; }
        public Guid Id { get; set; }
        public World World { get { return _world; } set { _world = value; _world.Game = this; } }
        public List<Player> Players { get; set; }
        public Player NewPlayer(string givenName, string houseName, Sex sex, People people, PlayerType playerType, Random rnd, Guid? id = null)
        {
            var player = new Player() { PlayerType = playerType };
            if (id.HasValue)
            {
                player.Id = id.Value;
            }
            AddPlayer(player);

            if (playerType == PlayerType.Live)
            {
                
                var fnamePrompt = "What is your first name?\n";
                var lnamePrompt = "{0}, what is the name of your house?\n";
                var sexPrompt = "{0}, are you a [m]an or a [w]oman?\n";
                var fnameCommand = new PlayerInputCommand(player);
                var lnameCommand = new PlayerInputCommand(player);
                var sexCommand = new PlayerInputCommand(player);
                player.NextCommand = fnameCommand;
                fnameCommand.PromptFunc = () =>
                {
                    return fnamePrompt;
                };
                fnameCommand.CommandFunc = (input) =>
                {
                    if (input.Length > 0)
                    {
                        givenName = input;
                        player.NextCommand = lnameCommand;
                    }
                    
                };
                lnameCommand.PromptFunc = () =>
                {
                    return string.Format(lnamePrompt, givenName);
                };
                lnameCommand.CommandFunc = (input) =>
                {
                    if (input.Length > 0){
                        houseName = input;
                        player.NextCommand = sexCommand;
                    }
                };
                sexCommand.PromptFunc = () =>
                {
                    return string.Format(sexPrompt, givenName + " " + houseName);
                };
                sexCommand.CommandFunc = (input) =>
                {
                    switch (input.Trim().ToLower())
                    {
                        case "m":
                        case "man":
                            sex = Sex.Male;
                            initPlayerHouse(player, givenName, houseName, sex, people, rnd, id);
                        break;
                        case "w":
                        case "woman":
                            sex = Sex.Female;
                            initPlayerHouse(player, givenName, houseName, sex, people, rnd, id);
                        break;
                    }
                    if (player.House != null)
                    {
                        player.NextCommand = player.NewPlayerMenu(player,null,rnd);
                    }
                };

            }
            else
            {
               initPlayerHouse(player, givenName, houseName, sex, people, rnd, id);
            }
            return player;
        }
        public void initPlayerHouse(Player player, string givenName, string houseName, Sex sex, People people, Random rnd, Guid? id = null)
        {
            //var availableLordships = World.Lordships.Where(l => (l.MapX == 1 || l.MapX == Constants.MAP_WIDTH) && l.LordIsInResidence && l.Lord.People == People.Kyltcled).ToArray();
            //var conquoredLordship = availableLordships[rnd.Next(availableLordships.Count() - 1)];
            var playerHouse = Utility.CreateNewHouse(houseName, houseName[0], sex, people, _world, rnd, null, Constants.YEARS_TO_ITERATE_PLAYER_HOUSES, Constants.MINIMUM_VILLAGE_SIZE * 4, player);
            playerHouse.Lord.Name = givenName;
            playerHouse.Lord.Sex = sex;
            if (sex == Sex.Female && playerHouse.Lord.Spouse != null)
            {
                playerHouse.Lord.Spouse.Sex = Sex.Male;
                playerHouse.Lord.Spouse.Name = Person.GenerateName(rnd, Sex.Male);
            }
        }
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
        public Game Flatten()
        {
            return new Game()
            {
                Id = Id,
                World = World.Flatten(),
                Players = Players.Select(p => p.Flatten()).ToList()
            };
        }
    }
}
