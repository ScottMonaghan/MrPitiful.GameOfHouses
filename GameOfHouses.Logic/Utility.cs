﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public static class Utility
    {
        public static string GetName(Sex sex, Random rnd)
        {
            var maleNames = Constants.MALE_NAMES.Split(',');
            var femaleNames = Constants.FEMALE_NAMES.Split(',');
            if (sex == Sex.Male)
            {
                return maleNames[rnd.Next(0, maleNames.Count())];
            }
            else
            {
                return femaleNames[rnd.Next(0, femaleNames.Count())];
            }
        }
        public static void IncrementYear(World world, Random rnd, int yearsToIncrement = 1) //Player player, World world, PlayerInputCommand callingCommand, PlayerInputCommand returnCommand, Random rnd)
        {
            
            world.Game.Players.ForEach(p => p.IncrementYear());
            for (int i = 0; i < yearsToIncrement; i++)
            {
                //1. Marriages
                var housesToMakeMoves = world.NobleHouses.Where(h => h.Lord != null).ToList();
                while (housesToMakeMoves.Count() > 0)
                {
                    var nextIndex = rnd.Next(0, housesToMakeMoves.Count());
                    var houseToMakeMoves = housesToMakeMoves[nextIndex];
                    housesToMakeMoves.Remove(houseToMakeMoves);
                    var heirsLeftToBetroth = houseToMakeMoves.Lord.GetCurrentHeirs().Where(heir => heir.IsEligableForBetrothal() && heir.Household.Lordship != null && heir.Household.Lordship.Lord!=null && heir.Household.Lordship.Lord.House == houseToMakeMoves).ToList();
                    if (houseToMakeMoves.Lord.IsEligableForBetrothal())
                    {
                        heirsLeftToBetroth.Add(houseToMakeMoves.Lord);
                    }
                    var remainingEligibleNobleSubjects = houseToMakeMoves.Lordships
                    .SelectMany(l => l.Households)
                    .SelectMany(h => h.Members)
                    .Where(
                        m =>
                        m.Class == SocialClass.Noble
                        && m.People == houseToMakeMoves.Lord.People
                        && !heirsLeftToBetroth.Contains(m)
                        && m.IsEligableForBetrothal()
                        ).ToList();
                    world.EligibleNobles = world.EligibleNobles.Union(remainingEligibleNobleSubjects).ToList();
                }
                //world.NobleHouses.ForEach(house => house.History[world.Year] = "");
                //callingCommand.Result.Output+="Performing Noble Marriages...\n";

                PerformNobleMarriages(world);// player, callingCommand, returnCommand, world);
                                             //2. Increment Word Year
                                             //callingCommand.Result.Output+="Incrementing World Year...";
                world.IncrementYear();
            }
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
            house.World.EligibleNobles = house.World.EligibleNobles.Union(house.Members.Except(potentialHeads).Where(m => m.IsEligableForBetrothal())).ToList();
            ////var potentialSpouses = new List<Person>();
            ////foreach (var otherHouse in house.World.NobleHouses)
            ////{
            ////    potentialSpouses.AddRange(otherHouse.GetDispensibleMembers().Where(x => x.IsEligableForBetrothal()));
            ////}
            var potentialSpouses = house.World.EligibleNobles;
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
        public static House CreateNewHouse(string name, char symbol, Sex lordsSex, People people, World newWorld, Random rnd, House allegience = null, int yearsToIterate = Constants.YEARS_TO_ITERATE_NEW_HOUSES, int numberOfPeasantHouseholds = Constants.MINIMUM_VILLAGE_SIZE, Player player = null)
        {
            Person newLord = null;
            World oldWorld = null;
            Lordship oldLordship = null;
            Person lord = null;
            while (newLord == null || !newLord.IsAlive)
            {
                oldWorld = new World(rnd) { Year = newWorld.Year - Constants.YEARS_TO_ITERATE_NEW_HOUSES };//lordship.World;
                oldLordship = new Lordship(rnd) { Name = "the homeland" };
                oldWorld.AddLordship(oldLordship);
                //create settlers
                var firstSettlers = new List<Household>();
                for (int i = 0; i < numberOfPeasantHouseholds; i++)
                {
                    var husband = new Person(rnd)
                    {
                        Age = rnd.Next(Constants.AGE_OF_MAJORITY, Constants.AGE_OF_MAJORITY * 2),
                        Sex = Sex.Male,
                        Profession = Profession.Peasant,
                        Class = SocialClass.Peasant,
                        People = people
                    };
                    husband.House = new House() { Name = husband.Name };
                    new Household().AddMember(husband);
                    husband.Household.HeadofHousehold = husband;
                    oldWorld.AddPerson(husband);
                    oldLordship.AddHousehold(husband.Household);                    
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
                    //settlerHousehold.HouseholdClass = SocialClass.Peasant;
                    firstSettlers.Add(settlerHousehold);
                }
                lord = new Person(rnd)
                {
                    Age = Constants.AGE_OF_MAJORITY,
                    Sex = lordsSex,
                    Profession = Profession.Noble,
                    Class = SocialClass.Noble,
                    People = people,
                    BirthYear = oldWorld.Year - Constants.AGE_OF_MAJORITY
                };
                oldWorld.AddPerson(lord);
                var lordsSpouse = new Person(rnd)
                {
                    Age = Constants.AGE_OF_MAJORITY,
                    Sex = lordsSex == Sex.Female? Sex.Male:Sex.Female,
                    Profession = Profession.Noble,
                    Class = SocialClass.Noble,
                    People = people,
                    BirthYear = oldWorld.Year - Constants.AGE_OF_MAJORITY
                };
                oldWorld.AddPerson(lordsSpouse);
                lord.House = new House() { Name = name, Symbol = symbol};
                lord.House.AddLord(lord);
                lord.House.AddMember(lord);
                if (player != null)
                {
                    lord.House.AddPlayer(player);
                }
                oldWorld.AddHouse(lord.House);
                var lordsHousehold = Household.CreateMarriageHousehold(lord, lordsSpouse);
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

                        //set sex of all headsOfHousehold to ensure player-chosen sex 
                        eligibleNoble.Sex = lordsSex;
                        eligibleNoble.Name = GetName(lordsSex, rnd);

                        spouse.Sex = lordsSex == Sex.Female ? Sex.Female : Sex.Male;
                        eligibleNoble.Name = GetName(spouse.Sex, rnd);
                        
                        oldWorld.AddPerson(spouse);

                        var marriageHousehold = Household.CreateMarriageHousehold(eligibleNoble, spouse);
                        if (!firstSettlers.Contains(marriageHousehold))
                        {
                            firstSettlers.Add(marriageHousehold);
                        }
                    }
                }
                newLord = oldLordship.Lords.Last();
            }
            var newHouse = lord.House;
            var newLordsHouseHold = oldLordship.Lords.Last().Household;
            //2. get the households with all of the lords decendents
            var newLordsDescendents = newLord.GetHeirs();
            var nobleHouseholds = oldLordship.Households.Where(h=>h.HeadofHousehold.Class==SocialClass.Noble);//(h => h.Members.Intersect(newLordsDescendents).Count() > 0).ToList();
            //3. get commoners
            var peasantHouseholds = oldLordship.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Peasant).Take(numberOfPeasantHouseholds).ToList();
            if (allegience != null)
            {
                allegience.AddVassle(newHouse);
            }
            //newWorld.AddHouse(lord.House);
            var settlerHouseholds = new List<Household>();
            settlerHouseholds.AddRange(nobleHouseholds);
            settlerHouseholds.AddRange(peasantHouseholds);
            
            //everyone else from the old world dies.  It's rough out there.
            oldWorld.Population.Where(p => !newLordsHouseHold.Members.Contains(p) && settlerHouseholds.Count(sh => sh.Members.Contains(p)) == 0).ToList().ForEach(p => p.IsAlive = false);
            //newLord.Lordships.Remove(oldLordship); //remove the old lordship
            //Lordship.PopulateLordship(lordship, newLordsHouseHold, settlerHouseholds);
            return lord.House;
        }
        public static void PerformNobleMarriages(World world)//Player player, PlayerInputCommand callingCommand, PlayerInputCommand returnCommand, World world)
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
        public static Game InitializeWorld(World world, Random rnd)//, string playerName = "Eddard", string playerHouse = "Stark", Sex playerSex = Sex.Male)
        {
            var newGame = new Game(rnd) { World = world };
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

            //Create Kyltklen lordships
            foreach (var lordship in world.Lordships.Where(l => l.Lord == null))
            {
                var newLand = lordship;
                var surnames = Constants.KYLTKLED_SURNAMES.Split(',');
                var newHouseName = "";
                while (newHouseName == "" || world.NobleHouses.Any(h => h.Name == newHouseName))
                {
                    if (world.NobleHouses.Any(h => h.Name == newHouseName))
                    {
                        newHouseName = "Kar" + newHouseName;
                    }
                    else {
                        newHouseName = surnames[rnd.Next(0, surnames.Count())];
                    }
                }
                var aiPlayer = new Player() { PlayerType = PlayerType.AIAggressive };
                newGame.AddPlayer(aiPlayer);
                //create house
                var newHouse = CreateNewHouse(newHouseName, (char)rnd.Next(33, 126), (Sex)rnd.Next(0,1), People.Kyltcled, world, rnd, null, rnd.Next(Constants.AGE_OF_MAJORITY, Constants.AGE_OF_MAJORITY*2), rnd.Next(Constants.MINIMUM_VILLAGE_SIZE, Constants.MINIMUM_VILLAGE_SIZE * 2), aiPlayer);
                //remove temp lordship created with house
                newHouse.Lord.Lordships.Clear();
                world.AddHouse(newHouse);
                //populate lordship
                Lordship.PopulateLordship(lordship, newHouse.Lord.Household, newHouse.Lord.Household.Lordship.Households.Where(h=>h!=newHouse.Lord.Household).ToList());

                Console.WriteLine(lordship.Name + " populated");
            }
            return newGame;
        }
        public static Game InitializeWorldWithIntro(World world, Random rnd, string playerName = "Eddard", string playerHouse = "Stark", Sex playerSex = Sex.Male)
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

            return InitializeWorld(world, rnd);//, playerName, playerHouse, playerSex);

            //var newGame = InitializeWorld(world, rnd, player, playerName, playerHouse, playerSex);

            //var playerLord = player.House.Lords.Last();
            //if (playerLord.Father == null)
            //{
            //    playerLord.Father = new Person(rnd);
            //}

            //Console.Write(
            //    "\nYou are " + playerLord.FullNameAndAge + "\n\n"
            //    );
            //var playerHeirs = playerLord.GetCurrentHeirs();
            //if (playerHeirs.Count() > 0)
            //{
            //    var output = String.Format("You have {0} living heir{1}: ", playerHeirs.Count(), playerHeirs.Count > 1 ? "s" : "");
            //    foreach (var heir in playerHeirs)
            //    {
            //        if (heir == playerHeirs.First())
            //        {
            //            output += "a ";
            //        }
            //        else if (heir == playerHeirs.Last())
            //        {
            //            output += ", and a ";
            //        }
            //        else
            //        {
            //            output += ", a ";
            //        }
            //        if (heir.Ancestors.IndexOf(playerLord) + 1 > 4)
            //        {
            //            output += "great ";
            //        }
            //        if (heir.Ancestors.IndexOf(playerLord) + 1 > 2)
            //        {
            //            output += "gand";
            //        }
            //        if (heir.Sex == Sex.Male)
            //        {
            //            output += "son";
            //        }
            //        else
            //        {
            //            output += "daughter";
            //        }
            //    }
            //    output += string.Format(".\nBy tradition, you as {0} of the house, name your heirs.\n", playerLord.Sex == Sex.Male ? "Lord and Patriarch" : "Lady and Matriarch");
            //    Console.WriteLine(output);
            //    foreach (var heir in playerHeirs)
            //    {
            //        var relation = "";
            //        if (heir.Ancestors.IndexOf(playerLord) + 1 > 4)
            //        {
            //            relation += "great ";
            //        }
            //        if (heir.Ancestors.IndexOf(playerLord) + 1 > 2)
            //        {
            //            relation += "gand";
            //        }
            //        if (heir.Sex == Sex.Male)
            //        {
            //            relation += "son";
            //        }
            //        else
            //        {
            //            relation += "daughter";
            //        }
            //        Console.WriteLine(String.Format("What is your {0}'s name?", relation));
            //        heir.Name = Console.ReadLine();

            //    }
            //    Console.WriteLine("\nYour heirs are:");
            //    foreach (var heir in playerHeirs)
            //    {
            //        Console.WriteLine(heir.FullNameAndAge);
            //    }
            //    Console.WriteLine("Enter to continue..");
            //    Console.ReadLine();
            //}
            //Console.WriteLine(string.Format(
            //"\n"
            //+ "The great house of {0} has fallen on hard times.\n\n"
            //+ "The rich soil that has provided bounty for uncounted generations of your people has turned rocky and fruitless.\n\n"
            //+ "The surrounding land is racked with strife, banditry, and civil war. The sway that House {0} once held over lords and kings alike is a distant memory.\n\n"
            //+ "Nought remains of the once-great wealth of House {0} except for your family and the good men and women in your service -- and your renouned, sturdy long-ships.\n\n"
            //+ "Days ago, in a feverish state, lying in his death bed, your father, staring wide-eyed, speaking to no-one, had been raving madly for days-on of a land of plenty to the east.\n\n"
            //+ "As you knelt at the foot of his bed for his final hours, his eyes suddenly grew sharp and fixed on you with steel-blue intensity that instantly\n"
            //+ "transformed him from this pitiful frail wretch into the powerful man-god whose presence dominated your childhood heart with equal parts awe, love and fear.\n\n"
            //+ "And, for the last time, {1}, Lord and Patriarch of House {0}, filled the room with that oh-so-familiar baritone of command:\n"
            //+ "'{2}! GATHER OUR PEOPLE INTO OUR LONGSHIPS AND SAIL EAST TO SALVATION! IF NOT, ALL IS LOST AND HOUSE {3} WILL BE NO MORE!'\n\n"
            //+ "He then collapsed into a fugue and spoke naught another word."
            //, playerLord.House.Name
            //, playerLord.Father.Name
            //, playerLord.Name.ToUpper()
            //, playerLord.House.Name.ToUpper()
            //));
            //Console.WriteLine("Enter to continue..");
            //Console.ReadLine();
            //return newGame;

        }
        public static String GetDecendentsRelatioinshipToAncestor(Person ancestor, Person descendent)
        {
            var output = "";
            if (descendent.Ancestors.IndexOf(ancestor) + 1 > 4)
            {
                output += "great ";
            }
            if (descendent.Ancestors.IndexOf(ancestor) + 1 > 2)
            {
                output += "grand";
            }
            if (descendent.Sex == Sex.Male)
            {
                output += "son";
            }
            else
            {
                output += "daughter";
            }
            return output;
        }
        public static void Attack(Player player, Lordship commandingLordship, List<Lordship> attackers, Lordship target, PlayerInputCommand callingCommand, PlayerInputCommand returnMenu, Random rnd, bool getLiveInput = true, bool acceptFealty = false, bool acceptSurrender = false, double retreatRatio = 1, bool echo = true, bool ignoreDistance = false)
        {

            //Gather initial armies
            //var attackingLordship = this;
            //var attackingLordships = attackers.ToList();
            //attackingLordships.Add(commandingLordship);
            var availablePath = new List<Lordship>();
            availablePath.Add(commandingLordship);
            availablePath.Add(target);
            availablePath = availablePath.Union(commandingLordship.GetAllies()).ToList();
            var distancesFromAttackersToTarget = target.GetShortestAvailableDistanceToLordship(availablePath);
            var livingAttackers = new List<Person>();
            foreach (var attackingLordship in attackers) {
                if (!ignoreDistance && (!distancesFromAttackersToTarget.ContainsKey(attackingLordship) || double.IsPositiveInfinity(distancesFromAttackersToTarget[attackingLordship])))
                {
                    if (callingCommand != null)
                    {
                        callingCommand.Result.Output += (attackingLordship.Lords.Last().FullNameAndAge + " cannot reach " + commandingLordship.Name) + "\n";
                    }
                }
                else
                {
                    livingAttackers.AddRange(attackingLordship.Army.Where(p => p.IsAlive));
                }
            }
            //if (livingAttackers.Count()>0)
            //{
                var livingDefenders = target.Army.ToList();

                //set booleans for commands
                var offerSurrender = false;
                var offerFealty = false;
                //var acceptFealty = false;
                //var acceptSurrender = false;
                var retreat = false;
                var endBattle = false;
                var timeSinceStartOfAttack = 1;
                var attackerArrived = false;
                var distanceFromAttackerToTarget = distancesFromAttackersToTarget[commandingLordship];
                var defendersAllies = target.GetAllies().ToList();//.Where(l=>l.LordIsInResidence).ToList();
                var unarrivedDefendersAllies = defendersAllies.ToList();
                var DistancesToDefenderAllies = target.GetShortestAvailableDistanceToLordship(defendersAllies);
                var attackRoundCommand = new PlayerInputCommand(player);
                player.NextCommand = attackRoundCommand;
                attackRoundCommand.PromptFunc = () =>
                {
                    var prompt = "";
                    //--check and see if attacker has arrived
                    if (timeSinceStartOfAttack < distanceFromAttackerToTarget)
                    {
                        prompt += (commandingLordship.Lords.Last().FullNameAndAge + " is on the march!\n"); 
                    }
                    else
                    {
                        attackerArrived = true;
                        prompt += (commandingLordship.Lords.Last().FullNameAndAge + " has arrived at " + target.Name + " with an army of " + livingAttackers.Count()) + "\n";
                    }
                    //--check for defender reinforcements
                    for (var i = 0; i < unarrivedDefendersAllies.Count; i++)
                    {
                        var unarrivedAlly = unarrivedDefendersAllies[i];
                        if (DistancesToDefenderAllies[unarrivedAlly] <= timeSinceStartOfAttack)
                        {
                            unarrivedDefendersAllies.Remove(unarrivedAlly);
                            var reinforcements = unarrivedAlly.Army.ToList();
                            prompt+= (unarrivedAlly.Lords.Last().FullNameAndAge + " arrived with " + reinforcements.Count() + " reinforcements!\n"); 
                            livingDefenders.AddRange(reinforcements);
                        }
                    }

                    //--defender may surrender and offer fealty
                    if (livingAttackers.Count() > livingDefenders.Count() * Constants.SURRENDER_RATIO)
                    {

                        var adjacentLordships = target.GetAdjacentLordships();
                        var targetAdjacentHouseLordships = new List<Lordship>();
                        var targetAdjacentAllyLordships = new List<Lordship>();
                        if (target.Lord != null)
                        {
                            targetAdjacentHouseLordships = adjacentLordships.Intersect(target.Lord.House.Lordships).ToList();
                            targetAdjacentAllyLordships = adjacentLordships.Intersect(defendersAllies).ToList();
                        }
                        if (targetAdjacentHouseLordships.Count() > 0 || targetAdjacentAllyLordships.Count() > 0)
                        {
                            Lordship sanctuary = null;
                            if (targetAdjacentHouseLordships.Count() > 0)
                            {
                                sanctuary = targetAdjacentHouseLordships[rnd.Next(0, targetAdjacentHouseLordships.Count())];
                            }
                            else
                            {
                                sanctuary = targetAdjacentAllyLordships[rnd.Next(0, targetAdjacentAllyLordships.Count())];
                            }

                            //move all households to sanctuary
                            foreach (var fleeingHousehold in target.Households.ToList())
                            {
                                sanctuary.AddHousehold(fleeingHousehold);
                            }
                            livingDefenders.Clear();
                            prompt += ("FLIGHT: " + target.Lord.FullNameAndAge + " HAS FLED to " + sanctuary.Name) + "\n";
                        }
                        else
                        {
                            if (target.Lord != null && commandingLordship.Lord.People != People.Kyltcled && target.Lord.People == commandingLordship.Lord.People && target.Lord.House.GetSovreign().Lord.Household != null && target.Lord.House.GetSovreign().Lord.Household.Lordship == target)
                            {
                                //Fealty 
                                prompt += ("OFFER OF FEALTY: " + target.Lord.FullNameAndAge + "sues for peace and OFFERS FEALTY to " + commandingLordship.Lord.FullNameAndAge) + "\n";
                                offerFealty = true;
                            } 
                            prompt += ("SURRENDER: " + target.Name + " SURRENDERS to " + commandingLordship.Lord.FullNameAndAge) + "\n";
                            offerSurrender = true;
                        }
                    }
                    prompt += "Remaining Attackers: " + livingAttackers.Count() + "\n";
                    prompt += "Remaining Defenders: " + livingDefenders.Count() + "\n";
                    prompt += ("[R]etreat?\n[A]ttack?\n");
                    if (offerFealty)
                    {
                        prompt+=("Accept [F]ealty?\n");
                    }
                    if (offerSurrender)
                    {
                        prompt+=("Accept [S]urrender?\n");
                    }
                    return prompt;
                };
                attackRoundCommand.CommandFunc = (attackRoundCommandInput) =>
                {
                    var validInput = false;
                    switch (attackRoundCommandInput.ToUpper().Trim())
                    {
                        case "R":
                            validInput = true;
                            retreat = true;
                            break;
                        case "A":
                            validInput = true;
                            break;
                        case "F":
                            if (offerFealty)
                            {
                                validInput = true;
                                acceptFealty = true;
                            }
                            break;
                        case "S":
                            if (offerSurrender)
                            {
                                validInput = true;
                                acceptSurrender = true;
                            }
                            break;
                    }

                    if (livingDefenders.Count() == 0 || livingAttackers.Count() == 0)
                    {
                        endBattle = true;
                    }

                    //--attacker chooses to continue attack or not
                    if (!getLiveInput)
                    {
                        validInput = true;
                        var defenderToAttackerRatio = (double)livingDefenders.Count() / livingAttackers.Count();

                        if (defenderToAttackerRatio > retreatRatio)
                        {
                            retreat = true;
                        }
                    }
                    //--if retreat then battle is over
                    if (retreat || double.IsInfinity(distanceFromAttackerToTarget)) 
                    {
                        endBattle = true;
                        attackRoundCommand.Result.Output += ("RETREAT: " + commandingLordship.Lord.FullNameAndAge + " RETREATED from battle.\n");
                    }
                    //--if fealty is accepted battle is over
                    if (offerFealty && acceptFealty)
                    {
                        endBattle = true;
                        commandingLordship.Lord.House.AddVassle(target.Lord.House);
                        var record = ("FEALTY: " + commandingLordship.Lord.House.Lord.FullNameAndAge + " accepted FEALTRY from " + target.Lord.House.Lord.FullNameAndAge) + "\n";
                        commandingLordship.Lord.House.RecordHistory(record);
                        target.Lord.House.RecordHistory(record);
                        attackRoundCommand.Result.Output += record;
                    }
                    else if (offerSurrender && acceptSurrender)
                    {
                        endBattle = true;
                    }
                    if (validInput) {
                        timeSinceStartOfAttack++;
                        if (!endBattle && attackerArrived)
                        {
                            //proceed with attack
                            var waveOfAttackers = livingAttackers.ToList();
                            var countOfAttackersInWave = waveOfAttackers.Count();
                            var countOfDefendersInWave = livingDefenders.Count();
                            int attackerCasultiesInWave = 0;
                            int defenderCasultiesInWave = 0;
                            while (waveOfAttackers.Count() > 0 && livingDefenders.Count > 0)
                            {
                                var defender = livingDefenders[rnd.Next(0, livingDefenders.Count())];
                                var attacker = waveOfAttackers[rnd.Next(0, waveOfAttackers.Count())];
                                waveOfAttackers.Remove(attacker);
                                var battleResult = rnd.Next(0, 2);
                                if (battleResult == 0)
                                {
                                    attacker.IsAlive = false;
                                    attackerCasultiesInWave++;
                                }
                                else
                                {
                                    defender.IsAlive = false;
                                    defenderCasultiesInWave++;
                                }
                                livingDefenders.RemoveAll(p => !p.IsAlive);
                                livingAttackers.RemoveAll(p => !p.IsAlive);
                            }
                            attackRoundCommand.Result.Output+=(string.Format("ATTACK WAVE {0} RESULTS:", timeSinceStartOfAttack)) + "\n";
                            attackRoundCommand.Result.Output += ("\tAttackers in Wave: " + countOfAttackersInWave) + "\n";
                            attackRoundCommand.Result.Output += ("\tDefender in Wave: " + countOfDefendersInWave) + "\n";
                            attackRoundCommand.Result.Output += ("\tAttacker Casulties in Wave: " + attackerCasultiesInWave) + "\n";
                            attackRoundCommand.Result.Output += ("\tDefender Casulties in Wave: " + defenderCasultiesInWave) + "\n";
                        }
                    }
                    if (livingAttackers.Count() == 0)
                    {
                        //DEFEAT
                        endBattle = true;
                        var record = ("DEFEAT: " + commandingLordship.Lord.FullNameAndAge + " WAS DEFEATED at " + target.Name + " by " + target.Lord.FullNameAndAge) + "\n";
                        commandingLordship.Lord.House.RecordHistory(record);
                        target.Lord.House.RecordHistory(record);
                        attackRoundCommand.Result.Output += record;
                    }
                    if (livingDefenders.Count() == 0 || (offerSurrender && acceptSurrender))
                    {
                        //--if attacker wins they become Lord of lordship and take all nobles hostage                  
                        var record = 
                        string.Format("SURRENDER: {0} SURRENDERED {1} to {2}\n", 
                            target.Lord.FullNameAndAge, 
                            target.Name,
                            commandingLordship.Lord.FullNameAndAge);
                        target.Lord.House.RecordHistory(record);
                        commandingLordship.Lord.House.RecordHistory(record);
                        attackRoundCommand.Result.Output += record;

                        var newLord = commandingLordship.Lord;
                        endBattle = true;
                        var oldLord = target.Lord;
                        if (oldLord != null)
                        {
                            oldLord.Lordships.Remove(target);
                        }
                        target.Lords.Clear();
                        target.Vacant = true;
                        target.AddLord(newLord);
                        //you can't attack from a newly conquored lordship
                        newLord.House.Player.HouseLordshipsSummonedThisTurn.Add(target);
                    }
                    if (endBattle)
                    {
                        player.NextCommand = returnMenu;
                    }

                };
                
            //}
       }

    }
}
