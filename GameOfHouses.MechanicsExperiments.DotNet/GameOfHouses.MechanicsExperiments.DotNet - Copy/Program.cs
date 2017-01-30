using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public class Program
    {
        public static void IncrementYear(World world, Random rnd)
        {
            //1. Houses make moves
            var housesToMakeMoves = new List<House>();
            housesToMakeMoves.AddRange(world.NobleHouses);
            while (housesToMakeMoves.Count() > 0)
            {
                var nextIndex = rnd.Next(0, housesToMakeMoves.Count());
                var houseToMakeMoves = housesToMakeMoves[nextIndex];
                HouseMoves(houseToMakeMoves, rnd);
                housesToMakeMoves.Remove(houseToMakeMoves);
            }
            PerformNobleMarriages(world);
            //2. Increment Word Year
            world.IncrementYear();
        }
        public static void HouseMoves(House house, Random rnd)
        {
            /*
             * hold of on colonization for now
            //colonize
            if (house != house.World.Player.House)
            {
                var housesLordships = house.GetLordships();
                foreach (var lordship in housesLordships)
                {
                    var adjacentUnclaimedLands = GetAdjacentUnclaimedLands(lordship);
                    var peasantHouseholds = lordship.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Peasant).ToList();
                    var potentialSettlerLordHouseholds = lordship.Lords.Last().House.GetPotentialSettlerLords().Select(p => p.Household).ToList();//lordship.GetPotentialSettlerLordHouseholds().ToList();
                    while (potentialSettlerLordHouseholds.Count() > 0 && peasantHouseholds.Count() >= Constants.MINIMUM_VILLAGE_SIZE * 2 && adjacentUnclaimedLands.Count() > 0)
                    {
                        var settlerLordship = adjacentUnclaimedLands[rnd.Next(0, adjacentUnclaimedLands.Count())];
                        var settlerPeasantHouseholds = peasantHouseholds.Take(peasantHouseholds.Count()/2).ToList();
                        //settlerLordship.FoundingYear = house.World.Year;
                        adjacentUnclaimedLands.Remove(settlerLordship);
                        var settlerLordHousehold = potentialSettlerLordHouseholds[rnd.Next(0, potentialSettlerLordHouseholds.Count())];
                        potentialSettlerLordHouseholds.Remove(settlerLordHousehold);
                        house.Player.SettleNewLordship(lordship, settlerLordship, settlerLordHousehold, settlerPeasantHouseholds);
                    }
                }
            }
            */
            //Matchmaker
            var potentialHeads = house.GetIndespensibleMembers().Where(x => x.IsEligableForBetrothal()).ToList();

            var potentialSpouses= new List<Person>();
            foreach (var otherHouse in house.World.NobleHouses)
            {
                potentialSpouses.AddRange(otherHouse.GetDispensibleMembers().Where(x => x.IsEligableForBetrothal()));
            }
            MatchBetrothals(potentialHeads, potentialSpouses);

            /* hold off on recruiting for now
        if (potentialHeads.Count(x => x.Age > 5 && x.Age < 25 && x.IsEligableForBetrothal() && x.GetHeirs().Count == 0) > 0)
        {
            //no heirs!  Try to invite a new house from the homeland!
            if (rnd.NextDouble() <= Constants.CHANCE_OF_RECRUITING_FOREIGN_HOUSE && house.Recruitments < Constants.MAX_RECRUITMENTS)
            {
                var maxX = Constants.MAP_WIDTH;
                var minX = 1;
                if(house.Lords.Last().People == People.Dane)
                {
                    minX = Constants.MAP_WIDTH / 2;
                } else
                {
                    maxX = Constants.MAP_WIDTH / 2;
                }

                var unclaimedLands = house.World.Lordships.Where(t => t.Vacant && t.MapX >= minX && t.MapX <= maxX).ToList();//GetUnclaimedLands(house);
                if (unclaimedLands.Count() > 0)
                {
                    //var eldest = unmatchedLordsAndHeirs.Max(h => h.Age) + 5;
                    var newLand = unclaimedLands[rnd.Next(0, unclaimedLands.Count())];
                    var surnames = Constants.SURNAMES.Split(',');
                    var newHouseName = "";
                    while (newHouseName == "" || house.World.NobleHouses.Any(h => h.Name == newHouseName)){
                        newHouseName = surnames[rnd.Next(0, surnames.Count())];
                    }

                    var newHouse = CreateNewHouse(newHouseName, (char)rnd.Next(33, 126), house.Lords.Last().People, newLand, rnd,null);
                    var recruitingLord = house.Lords.Last();
                    var recruitedLord = newHouse.Lords.Last();
                    if (recruitingLord.House == house.World.Player.House)
                    {
                        Console.WriteLine("RECRUITMENT: " + recruitingLord.FullNameAndAge + " RECRUITED " + recruitedLord.FullNameAndAge + " from the " + recruitingLord.People + " homeland.");
                    }
                    house.Recruitments++;
                    MatchBetrothals(potentialHeads, newHouse.GetDispensibleMembers());
                }
            
                }
            }*/

        }
        public static void MatchBetrothals(List<Person> potentialHeads, List<Person> potentialSpouses)
        {
            var headsLeftToMatch = new List<Person>();
            var spousesLeftToMatch = new List<Person>();
            headsLeftToMatch.AddRange(potentialHeads);
            spousesLeftToMatch.AddRange(potentialSpouses);

            while (headsLeftToMatch.Count() > 0 && spousesLeftToMatch.Count() > 0)
            {
                var eligableLordOrHeir = headsLeftToMatch.First();
                var matches = spousesLeftToMatch.Where(x => x.IsCompatibleForBetrothal(eligableLordOrHeir)).ToList();

                if (matches.Count() > 0)
                {
                    //find match closest in age
                    matches = matches.OrderBy(m => Math.Abs(m.Age - eligableLordOrHeir.Age)).ToList();
                    var match = matches.First();
                    eligableLordOrHeir.World.CreateBethrothal(eligableLordOrHeir, match, eligableLordOrHeir.World.Year, true);
                    spousesLeftToMatch.Remove(match);
                }
                headsLeftToMatch.Remove(eligableLordOrHeir);
            }
        }
        public static House CreateNewHouse(string name, char symbol, People people, Lordship lordship, Random rnd, House allegience = null, int yearsToIterate = Constants.YEARS_TO_ITERATE_NEW_HOUSES, int numberOfPeasantHouseholds = Constants.MINIMUM_VILLAGE_SIZE)
        {
            Person newLord = null;
            World oldWorld = null;
            World newWorld = null;
            Lordship oldLordship = null;
            Person lord = null;
            while (newLord == null || !newLord.IsAlive) { 
            oldWorld = new World(rnd) { Year = lordship.World.Year - Constants.YEARS_TO_ITERATE_NEW_HOUSES };//lordship.World;
            newWorld = lordship.World;
            oldLordship = new Lordship(rnd) { Name = "OldLordship" };
            oldWorld.AddLordship(oldLordship);
            //create settlers
            var firstSettlers = new List<Household>();
            for (int i = 0; i < Constants.MINIMUM_VILLAGE_SIZE; i++)
            {
                var husband = new Person(rnd)
                {
                    Age = rnd.Next(18, 36),
                    Sex = Sex.Male,
                    Profession = Profession.Peasant,
                    Class = SocialClass.Peasant,
                    People = people
                };
                husband.House = new House() { Name = husband.Name };
                oldWorld.AddPerson(husband);
                var wife = new Person(rnd)
                {
                    Age = husband.Age,
                    Sex = Sex.Female,
                    Profession = Profession.Peasant,
                    Class = SocialClass.Peasant,
                    People = people
                };
                oldWorld.AddPerson(wife);
                var settlerHousehold = Household.CreateMarriageHousehold(husband, wife);
                settlerHousehold.HouseholdClass = SocialClass.Peasant;
                firstSettlers.Add(settlerHousehold);
            }

            lord = new Person(rnd)
            {
                Age = 18,
                Sex = Sex.Male,
                Profession = Profession.Noble,
                Class = SocialClass.Noble,
                People = people,
                BirthYear = oldWorld.Year - 18
            };
            oldWorld.AddPerson(lord);
            var lady = new Person(rnd)
            {
                Age = 18,
                Sex = Sex.Female,
                Profession = Profession.Noble,
                Class = SocialClass.Noble,
                People = people,
                BirthYear = oldWorld.Year - 18
            };
            oldWorld.AddPerson(lady);
            lord.House = new House() { Name = name, Symbol = symbol };
            lord.House.AddLord(lord);
                lord.House.AddPlayer(new Player());
            oldWorld.AddHouse(lord.House);
            var lordsHousehold = Household.CreateMarriageHousehold(lord, lady);
            lordsHousehold.HouseholdClass = SocialClass.Noble;
            Lordship.PopulateLordship(oldLordship, lordsHousehold, firstSettlers);
            for (var i = 0; i < yearsToIterate; i++)
            {
                oldWorld.IncrementYear();
                var eligibleNobles = oldWorld.Population.Where(x => x.Class == SocialClass.Noble && x.IsEligibleForMarriage());
                while (eligibleNobles.Count() > 0)
                {
                    //create a spouse for each
                    var eligibleNoble = eligibleNobles.First();
                    var spouse = new Person(rnd) { Age = eligibleNoble.Age, Class = SocialClass.Noble, BirthYear = eligibleNoble.BirthYear, People = people };
                    if (eligibleNoble.Sex == Sex.Male)
                    {
                        spouse.Sex = Sex.Female;
                    }
                    else
                    {
                        spouse.Sex = Sex.Male;
                    }
                    oldWorld.AddPerson(spouse);
                    var marriageHousehold = Household.CreateMarriageHousehold(eligibleNoble, spouse);
                    if (!firstSettlers.Contains(marriageHousehold))
                    {
                        firstSettlers.Add(marriageHousehold);
                    }
                }
            }
            //old world is iterated now prepare for the new world
            //1. get the lord's household
            newLord = oldLordship.Lords.Last();
        }
            var newHouse = lord.House;
            var newLordsHouseHold = oldLordship.Lords.Last().Household;
            //2. get the households with all of the lords decendents
            var newLordsDescendents = newLord.GetHeirs();
            var nobleHouseholds = oldLordship.Households.Where(h => h.Members.Intersect(newLordsDescendents).Count() > 0).ToList();
            //3. get commoners
            var peasantHouseholds = oldLordship.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Peasant).Take(numberOfPeasantHouseholds).ToList();
            if (allegience != null)
            {
                allegience.AddVassle(newHouse);
            }
            newWorld.AddHouse(lord.House);
            lord.House.Seat = lordship;
            var settlerHouseholds = new List<Household>();
            settlerHouseholds.AddRange(nobleHouseholds);
            settlerHouseholds.AddRange(peasantHouseholds);
            //everyone else from the old world dies.  It's rough out there.
            oldWorld.Population.Where(p => !newLordsHouseHold.Members.Contains(p) && settlerHouseholds.Count(sh => sh.Members.Contains(p)) == 0).ToList().ForEach(p => p.IsAlive = false);
            newLord.Lordships.Remove(oldLordship); //remove the old lordship
            Lordship.PopulateLordship(lordship, newLordsHouseHold, settlerHouseholds);
            return lord.House;
        }
        public static void PerformNobleMarriages(World world)
        {
            var bethrothalsToCheck = new List<Bethrothal>();
            bethrothalsToCheck.AddRange(world.Bethrothals);
            while (bethrothalsToCheck.Count() > 0)
            {
                var betrothalToCheck = bethrothalsToCheck[0];
                if (!betrothalToCheck.HeadOfHouseholdToBe.IsAlive || !betrothalToCheck.SpouseToBe.IsAlive)
                {
                    //cancel betrothal is someone dies!
                    world.CancelBetrothal(betrothalToCheck);
                }
                else if (betrothalToCheck.HeadOfHouseholdToBe.Age >= Constants.AGE_OF_MAJORITY && betrothalToCheck.SpouseToBe.Age >= Constants.AGE_OF_MAJORITY)
                {
                    Household.CreateMarriageHousehold(betrothalToCheck.HeadOfHouseholdToBe, betrothalToCheck.SpouseToBe, true);
                    world.CancelBetrothal(betrothalToCheck);
                }
                bethrothalsToCheck.Remove(betrothalToCheck);
            }
        }
        public static Game InitializeWorld(World world, Random rnd, Player livePlayer, string playerName = "Eddard", string playerHouse = "Stark", Sex playerSex = Sex.Male)
        {
            var newGame = new Game();
            var lordshipNames = Constants.TOWN_NAMES.Split(',').ToList();
            for (var y = 1; y <= Constants.MAP_HEIGHT; y++)
            {
                for (var x = 1; x <= Constants.MAP_WIDTH; x++)
                {
                    var villageName = lordshipNames[0];
                    world.AddLordship(new Lordship(rnd) { Name = villageName, MapX = x, MapY = y });
                    lordshipNames.Remove(villageName);
                }
            }
            
            var houseNames = new List<string>()
            {
                "Stark",
                "Tully",
                "Arryn",
                "Lannister",
                "Baratheon",
                "Tyrell",
                "Martell",
                "Greyjoy",
                "Targaryen"
            };
            var houseSeats = new List<string>()
            {
                "Winterfell",
                "Riverrun",
                "TheEyrie",
                "CasterlyRock",
                "Storm'sEnd",
                "HighGarden",
                "Dorne",
                "Pyke",
                "Dragonstone"
            };
            var houseSymbols = new List<Char>()
            {
                '!',
                '@',
                '#',
                '$',
                '%',
                '^',
                '&',
                '*',
                '+'
            };
            //}*/
            var unclaimedEasternLordships = world.Lordships.Where(x => x.Vacant && x.MapX < Constants.MAP_WIDTH/2).ToList();
            var unclaimedWesternLordships = world.Lordships.Where(x => x.Vacant && x.MapX > Constants.MAP_WIDTH/2).ToList();

            //create first dane house
            var playerLordship = unclaimedWesternLordships[rnd.Next(0, unclaimedWesternLordships.Count())];
            playerLordship.Name = "Winterfell";
            var firstHouse = CreateNewHouse(playerHouse, '*', People.Dane, playerLordship, rnd, null, Constants.YEARS_TO_ITERATE_PLAYER_HOUSES, 50);
            unclaimedWesternLordships.Remove(playerLordship);
            Console.WriteLine(firstHouse.Name + " populated");
            firstHouse.AddPlayer(livePlayer);
            livePlayer.House.Symbol = '*';
            var playerLord = livePlayer.House.Lords.Last();
            livePlayer.House.Name = playerHouse;
            playerLord.Name = playerName;
            playerLord.Sex = playerSex;
            if (playerLord.Spouse != null)
            {
                if (playerLord.Sex == Sex.Male)
                {
                    playerLord.Spouse.Sex = Sex.Female;
                    playerLord.Spouse.Name = Utility.GetName(Sex.Female, rnd);
                }
                else
                {
                    playerLord.Spouse.Sex = Sex.Male;
                    playerLord.Spouse.Name = Utility.GetName(Sex.Male, rnd);
                }
            }
            livePlayer.PlayerType = PlayerType.Live;
            newGame.AddPlayer(livePlayer);
            //seed other major houses
            for (int i = 1; i<8; i++)
            {
                var majorLordship = unclaimedWesternLordships[rnd.Next(0, unclaimedWesternLordships.Count())];
                majorLordship.Name = houseSeats[i];
                CreateNewHouse(houseNames[i], houseSymbols[i], (People)rnd.Next(0, 2), majorLordship, rnd, null, Constants.YEARS_TO_ITERATE_PLAYER_HOUSES, 50);
            }
            //seed whole world
            foreach (var lordship in world.Lordships.Where(l => l.Vacant))
            {
                var newLand = lordship;
                var surnames = Constants.SURNAMES.Split(',');
                var newHouseName = "";
                while (newHouseName == "" || world.NobleHouses.Any(h => h.Name == newHouseName))
                {
                    newHouseName = surnames[rnd.Next(0, surnames.Count())];
                }
                var newHouse = CreateNewHouse(newHouseName, (char)rnd.Next(33, 126), unclaimedEasternLordships.Contains(lordship) ? People.Saxon : People.Dane, newLand, rnd, null, rnd.Next(20, 40));
                var aiPlayer = newHouse.Player;
                aiPlayer.PlayerType = (PlayerType)rnd.Next(1, 3);
                newGame.AddPlayer(aiPlayer);
                Console.WriteLine(lordship.Name + " populated");
            }

            ////create first saxon house
            //var saxonLordship = easternLordships[rnd.Next(0, easternLordships.Count())];
            //saxonLordship.Name = "Casterly Rock";
            //CreateNewHouse("Lannister", '$', People.Saxon, saxonLordship, rnd);
            //for (int k = 0; k < houseNames.Count(); k++)
            //{
            //    var vacantVillages = world.Lordships.Where(x => x.Vacant && (x.MapY == 1 || x.MapY == Constants.MAP_HEIGHT)).ToList();
            //    var firstVillage = vacantVillages[rnd.Next(0, vacantVillages.Count)];
            //    firstVillage.Name = houseSeats[k];
            //    CreateNewHouse(houseNames[k], houseSymbols[k], People.Saxon, firstVillage, rnd);
            //}

            return newGame;
        }
        public static Game InitializeWorldWithIntro(World world, Random rnd, Player player, string playerName = "Eddard", string playerHouse = "Stark", Sex playerSex = Sex.Male)
        {
            Console.WriteLine("What is your name?");
            playerName = Console.ReadLine();
            playerSex = Sex.Male;
            var sexResponse = "";
            while (sexResponse == "")
            {
                Console.WriteLine(string.Format("\nHello {0}!\n\nAre you a man or a woman?", playerName));
                sexResponse = Console.ReadLine();
                switch (sexResponse.ToLower())
                {
                    case "man":
                        playerSex = Sex.Male;
                        break;
                    case "woman":
                        playerSex = Sex.Female;
                        break;
                    default:
                        sexResponse = "";
                        break;
                }
            }
            Console.WriteLine(string.Format(
                "\n"
                + "{0}, you are the {1} of an ancient storied noble house.  The blood of kings and queens -- and according to legend, the gods themselves -- runs in your veins.\n"
                + "Your noble line runs unbroken to the first of men and is filled with Lords and Ladies both great and terrible.\n"
                + "\n"
                + "What is the name of your house?",
                playerName,
                playerSex == Sex.Male ? "Lord and Patriarch" : "Lady and Matriarch"
            ));
            playerHouse = Console.ReadLine();

            var newGame = InitializeWorld(world, rnd, player, playerName, playerHouse, playerSex);

            var playerLord = player.House.Lords.Last();
            if (playerLord.Father == null)
            {
                playerLord.Father = new Person(rnd);
            }

            Console.Write(
                "\nYou are " + playerLord.FullNameAndAge + "\n\n"
                );
            var playerHeirs = playerLord.GetCurrentHeirs();
            if (playerHeirs.Count() > 0)
            {
                var output = String.Format("You have {0} living heir{1}: ", playerHeirs.Count(), playerHeirs.Count > 1 ? "s" : "");
                foreach (var heir in playerHeirs)
                {
                    if (heir == playerHeirs.First())
                    {
                        output += "a ";
                    }
                    else if (heir == playerHeirs.Last())
                    {
                        output += ", and a ";
                    }
                    else
                    {
                        output += ", a ";
                    }
                    if (heir.Ancestors.IndexOf(playerLord) + 1 > 4)
                    {
                        output += "great ";
                    }
                    if (heir.Ancestors.IndexOf(playerLord) + 1 > 2)
                    {
                        output += "gand";
                    }
                    if (heir.Sex == Sex.Male)
                    {
                        output += "son";
                    }
                    else
                    {
                        output += "daughter";
                    }
                }
                output += string.Format(".\nBy tradition, you as {0} of the house, name your heirs.\n", playerLord.Sex == Sex.Male ? "Lord and Patriarch" : "Lady and Matriarch");
                Console.WriteLine(output);
                foreach (var heir in playerHeirs)
                {
                    var relation = "";
                    if (heir.Ancestors.IndexOf(playerLord) + 1 > 4)
                    {
                        relation += "great ";
                    }
                    if (heir.Ancestors.IndexOf(playerLord) + 1 > 2)
                    {
                        relation += "gand";
                    }
                    if (heir.Sex == Sex.Male)
                    {
                        relation += "son";
                    }
                    else
                    {
                        relation += "daughter";
                    }
                    Console.WriteLine(String.Format("What is your {0}'s name?", relation));
                    heir.Name = Console.ReadLine();

                }
                Console.WriteLine("\nYour heirs are:");
                foreach (var heir in playerHeirs)
                {
                    Console.WriteLine(heir.FullNameAndAge);
                }
                Console.WriteLine("Enter to continue..");
                Console.ReadLine();
            }          
            Console.WriteLine(string.Format(
            "\n"
            + "The great house of {0} has fallen on hard times.\n\n"
            + "The rich soil that has provided bounty for uncounted generations of your people has turned rocky and fruitless.\n\n"
            + "The surrounding land is racked with strife, banditry, and civil war. The sway that House {0} once held over lords and kings alike is a distant memory.\n\n"
            + "Nought remains of the once-great wealth of House {0} except for your family and the good men and women in your service -- and your renouned, sturdy long-ships.\n\n"
            + "Days ago, in a feverish state, lying in his death bed, your father, staring wide-eyed, speaking to no-one, had been raving madly for days-on of a land of plenty to the east.\n\n"
            + "As you knelt at the foot of his bed for his final hours, his eyes suddenly grew sharp and fixed on you with steel-blue intensity that instantly\n"
            + "transformed him from this pitiful frail wretch into the powerful man-god whose presence dominated your childhood heart with equal parts awe, love and fear.\n\n"
            + "And, for the last time, {1}, Lord and Patriarch of House {0}, filled the room with that oh-so-familiar baritone of command:\n"
            + "'{2}! GATHER OUR PEOPLE INTO OUR LONGSHIPS AND SAIL EAST TO SALVATION! IF NOT, ALL IS LOST AND HOUSE {3} WILL BE NO MORE!'\n\n"
            + "He then collapsed into a fugue and spoke naught another word."
            , playerLord.House.Name
            , playerLord.Father.Name
            , playerLord.Name.ToUpper()
            , playerLord.House.Name.ToUpper()
            ));
            Console.WriteLine("Enter to continue..");
            Console.ReadLine();
            return newGame;

        }
        public static void Main(string[] args)
        {
            var rnd = new Random();
            var world = new World(rnd);
            var player = new Player();
            world.Player = player;
            Game game;
            if (Constants.PLAY_INTRO)
            {
                game = InitializeWorldWithIntro(world, rnd, player);
            } else
            {
                game = InitializeWorld(world, rnd, player);
            }

            while (world.Year < 500)
            {
                var playersLeftToTakeTurn = game.Players.ToList();
                while (playersLeftToTakeTurn.Count() > 0)
                {
                    var nextPlayer = playersLeftToTakeTurn[rnd.Next(0, playersLeftToTakeTurn.Count())];
                    playersLeftToTakeTurn.Remove(nextPlayer);
                    nextPlayer.DoPlayerTurn(rnd);
                }
                IncrementYear(world, rnd);
            }
            Console.ReadLine();
        }
    }
}
