using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public class Player
    {
        public Player()
        {
            House = null;
        }
        public Game Game { get; set; }
        public House House { get; set; }
        public void SettleNewLordship(Lordship sourceLordship, Lordship targetLordship, Household lordsHouseHold, List<Household> peasantHouseholds)
        {
            if (sourceLordship.PlayerMoves.Count(p => p == this) < Constants.ALLOWED_MOVES_PER_YEAR)
            {
                Lordship.PopulateLordship(targetLordship, lordsHouseHold, peasantHouseholds);
                sourceLordship.PlayerMoves.Add(this);
            }
        }
        public PlayerType PlayerType { get; set; }
        public void DoPlayerTurn(Random rnd)
        {
            // move lord and army to destination
            while (House.DeploymentRequests.Count() > 0)
            {
                var deploymentRequest = House.DeploymentRequests[0];
                House.DeploymentRequests.Remove(deploymentRequest);
                var requestedArmy = deploymentRequest.RequestedArmy;
                if (!requestedArmy.Vacant && (requestedArmy.Lord.House == House || House.Vassles.Contains(requestedArmy.Lord.House)))
                {
                    if (!double.IsPositiveInfinity(requestedArmy.GetShortestAvailableDistanceToLordship(deploymentRequest.Destination, requestedArmy.GetAllies())))
                    {
                        requestedArmy.DischargeSoldiers(requestedArmy.Army.Count());
                        requestedArmy.ConscriptSoldiers(deploymentRequest.NumberOfTroops);
                        deploymentRequest.Destination.AddOccupyingLordAndArmy(requestedArmy);
                        if (deploymentRequest.RequestingHouse.Player.PlayerType == PlayerType.Live)
                        {
                            Console.WriteLine("ARRIVAL: " + requestedArmy.Lord.FullNameAndAge + " HAS ARRIVED AT " + deploymentRequest.Destination.Name + " with " + requestedArmy.Army.Count() + " soldiers.");
                        }
                    }
                    else
                    {
                        if (deploymentRequest.RequestingHouse.Player.PlayerType == PlayerType.Live)
                        {
                            Console.WriteLine("UNREACHABLE: " + requestedArmy.Lord.FullNameAndAge + " CANNOT REACH " + deploymentRequest.Destination.Name);
                        }
                    }
                }
            }
            switch (PlayerType)
            {
                case PlayerType.Live:
                    DoLivePlayerTurn(rnd);
                    break;
                case PlayerType.AIAggressive:
                    DoAggressivePlayerTurn(rnd);
                    break;
            }
        }
        public void DoAggressivePlayerTurn(Random rnd)
        {
            if (!House.Seat.Vacant && House.Seat.Lord.House == House)
            {
                //conscript all in seat
                House.Seat.ConscriptSoldiers(House.Seat.EligibleForConscription.Count());
                //summon all outside seat
                foreach (var lordship in House.Vassles.SelectMany(v => v.Lordships).Union(House.Lordships.Where(l => l != House.Seat)))
                {
                    House.DeploymentRequests.Add(
                        new DeploymentRequest() { Destination = House.Seat, NumberOfTroops = lordship.EligibleForConscription.Count(), RequestingHouse = House, RequestedArmy = lordship}
                        );
                    //lordship.DeploymentRequest = new DeploymentRequest() { Destination = House.Seat, NumberOfTroops = lordship.EligibleForConscription.Count(), RequestingHouse = House };
                }
                //aggro attacks closest lordship with the least defenders 
                var target = House.Seat.GetAttackableLordships().OrderBy(l => l.Defenders.Count()).FirstOrDefault();
                if (target != null)
                {
                    //if army is less than half the size of aggro's then aggro will conquor
                    //if defender is more than half the size of aggro will accept fealty
                    //if defender is more than 90% of aggro, aggro will retreat
                    var echoAttack = (!target.Vacant && target.Lord.House.Player.PlayerType == PlayerType.Live);
                    House.Seat.Attack(target, rnd, false, 0.5, 0.9, echoAttack);
                }
            }
        }
        public void PersonMenu(Person person)
        {
            var command = "";
            Console.Write(person.GetDetailsAsString());
            while (command.ToLower() != "x")
            {
                Console.WriteLine("[D]etails or E[x]it");
                command = Console.ReadLine();
                switch (command.ToLower())
                {
                    case "d":
                        Console.WriteLine(person.GetDetailsAsString());
                        break;
                }
            }
        }
        public void HouseholdMenu(Household household)
        {
            //var household = nobleHouseholds[householdNumber - 1];
            var individualHouseholdCommand = "";
            Console.WriteLine(household.GetDetailsAsString());
            while (individualHouseholdCommand.ToLower() != "x")
            {
                Console.WriteLine("Household [D]etails, [M]ember Details, [R]esettle, or E[x]it");
                individualHouseholdCommand = Console.ReadLine();
                switch (individualHouseholdCommand.ToLower())
                {
                    case "d":
                        Console.WriteLine(household.GetDetailsAsString());
                        break;
                    case "m":
                        var memberNumber = -1;
                        var memberCommand = "";
                        while (memberCommand.ToLower() != "x" && !(memberNumber >= 1 && memberNumber <= household.Members.Count()))
                        {
                            Console.WriteLine("Enter number for more details or e[x]it.");
                            for (var i = 0; i < household.Members.Count(); i++)
                            {
                                Console.WriteLine(
                                    string.Format(
                                        "{0}. {1}",
                                        i + 1,
                                        household.Members[i].FullNameAndAge
                                        )
                                    );
                            }
                            memberCommand = Console.ReadLine();
                            int.TryParse(memberCommand, out memberNumber);
                            if(memberNumber >= 1 && memberNumber <= household.Members.Count())
                            {
                                PersonMenu(household.Members[memberNumber - 1]);
                            }
                        }
                        break;
                    case "r":
                        var subjectHouse = household.HeadofHousehold.House;
                        var lordshipSelectionCommand = "";
                        while (lordshipSelectionCommand.ToLower() != "x")
                        {
                            var selectedLordshipNumber = -1;
                            Console.WriteLine("Select a lorship by number to resettle " + household.HeadofHousehold.FullNameAndAge);
                            for (var i = 0; i < subjectHouse.Lordships.Count; i++)
                            {
                                Console.WriteLine(
                                    string.Format(
                                        "{0}. {1}({2},{3})",
                                        i + 1,
                                        subjectHouse.Lordships[i].Name,
                                        subjectHouse.Lordships[i].MapX,
                                        subjectHouse.Lordships[i].MapY
                                        )
                                    );
                            }
                            lordshipSelectionCommand = Console.ReadLine();
                            int.TryParse(lordshipSelectionCommand, out selectedLordshipNumber);
                            if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= subjectHouse.Lordships.Count)
                            {
                                var newLordship = subjectHouse.Lordships[selectedLordshipNumber - 1];
                                household.Resettle(newLordship);
                                Console.WriteLine(
                                    string.Format(
                                    "RESETTLED: {0} has RESETTLED in {1}",
                                    household.HeadofHousehold.FullNameAndAge,
                                    newLordship.Name
                                    ));

                                //LordshipMenu(subjectHouse.Lordships[selectedLordshipNumber - 1], rnd);
                            }
                        }
                        break;

                }
            }

        }
        public void ChangeLordshipLordMenu(Lordship lordship)
        {
            var changeLordshipCommand = "";
            var newLordNumber = -1;
            var subjectHouse = lordship.Lord.House;
            var nobleHouseholds = subjectHouse.Lordships
                .SelectMany(l => l.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Noble))
                .OrderBy(h=>h.HeadofHousehold.House.Name)
                .ThenBy(h=>h.HeadofHousehold.Name)
                .ToList();
            var householdCommand = "";
            while (householdCommand.ToLower() != "x")
            {
                Console.WriteLine("Noble households of House " + subjectHouse.Name + ":");
                for (var i = 0; i < nobleHouseholds.Count(); i++)
                {
                    Console.WriteLine(
                        string.Format(
                            "{0}. {1}",
                            i + 1,
                            nobleHouseholds[i].HeadofHousehold.FullNameAndAge
                            )
                        );
                }
                Console.WriteLine("Enter Household Number to take over lordship of " + lordship.Name + "  or E[x]it");
                int householdNumber = -1;
                householdCommand = Console.ReadLine();
                int.TryParse(householdCommand, out householdNumber);
                if (householdNumber >= 1 && householdNumber <= nobleHouseholds.Count)
                {
                    var newLordHousehold = nobleHouseholds[householdNumber - 1];
                    lordship.AddLord(newLordHousehold.HeadofHousehold);
                    Console.WriteLine("NEW LORD: " + lordship.Lord.FullNameAndAge + " HAS BEEN GRANTED LORDSHIP OF " + lordship.Name);
                }
            }

        }
        public void LordshipMenu(Lordship subjectLordship, Random rnd)
        {
            //var subjectLordship = subjectHouse.Lordships[selectedLordshipNumber - 1];
            var lordshipCommand = "";
            Console.WriteLine(subjectLordship.GetDetailsAsString());
            while (lordshipCommand.ToLower() != "x")
            {
                Console.WriteLine("[D]etails, [H]ouse, House [N]obles, [M]ap, [C]onscript, Dischar[g]e, [A]ttack, [S]ummon Army, [R]elease Army, Change [L]ord, Add [P]easant Households, E[x]it " + subjectLordship.Name);
                lordshipCommand = Console.ReadLine();
                switch (lordshipCommand.ToLower())
                {
                    case "p":
                        //Add [P]easant Households
                        var command = "";
                        var lordshipNumber = -1;
                        var otherLordships = subjectLordship.Lord.House.Lordships.Where(l => l != subjectLordship).ToList();
                        while (command!="x" && !(lordshipNumber>=1 && lordshipNumber <= otherLordships.Count()))
                        {
                            //list lordships with available peasant households
                            for (int i = 0; i < otherLordships.Count(); i++)
                            {
                                Console.WriteLine(
                                    string.Format(
                                        "{0}. {1} ({2} peasant housholds)",
                                        i + 1,
                                        otherLordships[i].Name,
                                        otherLordships[i].Households.Count(h => h.HeadofHousehold.Class == SocialClass.Peasant)                                        
                                        ));
                            }
                            //choose lorship
                            Console.WriteLine("Choose lordship to resettle from or e[x]it.");
                            command = Console.ReadLine();
                            int.TryParse(command, out lordshipNumber);
                            if (lordshipNumber >= 1 && lordshipNumber <= otherLordships.Count())
                            {
                                var immigrantLordship = otherLordships[lordshipNumber - 1];
                                var availableHouseholds = immigrantLordship.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Peasant).ToList();
                                //ask how may households to move
                                var numberOfHouseholdsCommand = "";
                                var numberOfHouseholds = -1;
                                while (!(numberOfHouseholds > 0) && numberOfHouseholdsCommand.ToLower() != "x")
                                {
                                    Console.WriteLine(string.Format(
                                        "{0} has {1} households available. How many households would you like to resettle to {2}? (E[x]it)",
                                        immigrantLordship.Name,
                                        availableHouseholds.Count(),
                                        subjectLordship.Name
                                        ));
                                    numberOfHouseholdsCommand = Console.ReadLine();
                                    int.TryParse(numberOfHouseholdsCommand, out numberOfHouseholds);
                                }
                                if (numberOfHouseholds > 0)
                                {
                                    //choose random households to move
                                    var immigrantHouseholds = new List<Household>();
                                    while (availableHouseholds.Count() > 0 && immigrantHouseholds.Count() < numberOfHouseholds)
                                    {
                                        var immigrantHousehold = availableHouseholds[rnd.Next(0, availableHouseholds.Count())];
                                        availableHouseholds.Remove(immigrantHousehold);
                                        immigrantHouseholds.Add(immigrantHousehold);
                                    }
                                    immigrantHouseholds.ForEach(h =>
                                    {
                                        h.Resettle(subjectLordship);
                                    });
                                    Console.WriteLine(string.Format(
                                        "RESETTLE: {0} households RESETTLED in {1} from {2}.",
                                        immigrantHouseholds.Count(),
                                        subjectLordship.Name,
                                        immigrantLordship.Name                                   
                                        ));
                                }
                            }
                        }
                        break;
                    case "l":
                        ChangeLordshipLordMenu(subjectLordship);
                        break;
                    case "n":
                        {
                            var subjectHouse = subjectLordship.Lord.House;
                            var nobleHouseholds = subjectHouse.Lordships
                                .SelectMany(l => l.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Noble))
                                .OrderBy(h=>h.HeadofHousehold.House.Name)
                                .ThenBy(h=>h.HeadofHousehold.Name)
                                .ToList();
                            var householdCommand = "";
                            while (householdCommand.ToLower() != "x")
                            {
                                Console.WriteLine("Noble households of House " + subjectHouse.Name + ":");
                                for (var i = 0; i < nobleHouseholds.Count(); i++)
                                {
                                    Console.WriteLine(
                                        string.Format(
                                            "{0}. {1}",
                                            i + 1,
                                            nobleHouseholds[i].HeadofHousehold.FullNameAndAge
                                            )
                                        );
                                }
                                Console.WriteLine("Enter Household Number for more details or E[x]it");
                                int householdNumber = -1;
                                householdCommand = Console.ReadLine();
                                int.TryParse(householdCommand, out householdNumber);
                                if (householdNumber >= 1 && householdNumber <= nobleHouseholds.Count)
                                {
                                    HouseholdMenu(nobleHouseholds[householdNumber - 1]);
                                }
                            }
                        }
                     break;
                     case "d":
                    case "details":
                        Console.WriteLine(subjectLordship.GetDetailsAsString());
                        break;
                    case "h":
                    case "house":
                        Console.WriteLine(subjectLordship.Lord.House.GetDetailsAsString());
                        break;
                    case "m":
                    case "map":
                        {
                            Console.WriteLine(subjectLordship.GetMapOfKnownWorld());
                        }
                        break;
                    //case "i":
                    //case "investigate":
                    //    {
                    //        if (command.Length > 2)
                    //        {
                    //            switch (command[2].ToLower())
                    //            {
                    //                case "lordship":
                    //                    {
                    //                        Lordship objectLordship = null;
                    //                        if (command.Length == 4)
                    //                        {
                    //                            objectLordship = world.Lordships.FirstOrDefault(l => l.Name.ToLower() == command[3].ToLower());
                    //                        }
                    //                        else if (command.Length == 5)
                    //                        {
                    //                            int x;
                    //                            int y;
                    //                            if (int.TryParse(command[3], out x) && int.TryParse(command[4], out y))
                    //                            {
                    //                                objectLordship = world.Lordships.FirstOrDefault(l => l.MapX == x && l.MapY == y);
                    //                            }
                    //                        }
                    //                        var visibleLordships = player.House.Seat.GetVisibleLordships();
                    //                        if (objectLordship != null && visibleLordships.Contains(objectLordship))
                    //                        {
                    //                            Console.WriteLine(objectLordship.GetDetailsAsString());
                    //                        }
                    //                    }
                    //                    break;
                    //                case "person":
                    //                    {
                    //                        if (command.Length == 5)
                    //                        {
                    //                            var name = command[3];
                    //                            var house = command[4];
                    //                            var persons = world.Population.Where(p => p.Name.ToLower() == name.ToLower() && p.House.Name.ToLower() == house.ToLower());
                    //                            foreach (var person in persons)
                    //                            {
                    //                                Console.WriteLine(person.GetDetailsAsString());
                    //                            }
                    //                        }
                    //                    }
                    //                    break;
                    //                case "world":
                    //                    {
                    //                        Console.WriteLine(world.GetDetailsAsString());
                    //                    }
                    //                    break;
                    //                case "people":
                    //                    {

                    //                    }
                    //                    break;
                    //            }
                    //        }
                    //    }
                    //    break;
                    case "c":
                    case "conscript":
                        {
                            int soldiersToConscript = int.MaxValue;
                            string prompt = "";
                            while ((!int.TryParse(prompt, out soldiersToConscript)) || !(soldiersToConscript <= subjectLordship.EligibleForConscription.Count()))
                            {
                                Console.Write(
                                    subjectLordship.Name + " has " + subjectLordship.Farmers.Count(f => f.IsAlive) + " farmers and " + subjectLordship.Army.Count(s => s.IsAlive) + " soldiers.\n"
                                    + "There are " + subjectLordship.EligibleForConscription.Count() + " subjects available for conscription. \n"
                                    + "How many soldiers would you like to conscript?\n"
                                    );
                                prompt = Console.ReadLine();
                            }
                            subjectLordship.ConscriptSoldiers(soldiersToConscript);

                            Console.Write(string.Format(
                                "{0} conscripted {1} soldiers into the army of {2}.\n"
                                + "{2} now has {3} farmers and {4} soldiers.\n",
                                subjectLordship.Lord.FullNameAndAge,
                                soldiersToConscript,
                                subjectLordship.Name,
                                subjectLordship.Farmers.Count(f => f.IsAlive),
                                subjectLordship.Army.Count(s => s.IsAlive)
                                ));
                        }
                        break;
                    case "g":
                    case "discharge":
                        {
                            int soldiersToDischarge = int.MaxValue;
                            string prompt = "";
                            while ((!int.TryParse(prompt, out soldiersToDischarge)) || !(soldiersToDischarge <= subjectLordship.Army.Count(s => s.IsAlive)))
                            {
                                Console.Write(string.Format(
                                    "{0} has {1} farmers and {2} soldiers.\n"
                                    + "How many soldiers would you like to discharge?\n",
                                    subjectLordship.Name,
                                    subjectLordship.Farmers.Count(f => f.IsAlive),
                                    subjectLordship.Army.Count(s => s.IsAlive)
                                    ));
                                prompt = Console.ReadLine();
                            }
                            subjectLordship.DischargeSoldiers(soldiersToDischarge);
                            Console.Write(
                                "{0} discharged {1} soldiers into the army of {2}.\n"
                                + "{2} now has {3} farmers and {4} soldiers.\n",
                                subjectLordship.Lord.FullNameAndAge,
                                soldiersToDischarge,
                                subjectLordship.Name,
                                subjectLordship.Farmers.Count(),
                                subjectLordship.Army.Count()
                                );
                        }
                        break;
                    case "a":
                    case "attack":
                        {
                            if (subjectLordship.AttacksLedThisYear >= Constants.ALLOWED_ATTACKS_PER_YEAR)
                            {
                                Console.WriteLine(subjectLordship.Name + " has already led the maximum nmber of attacks for this year.");
                            }
                            else
                            {
                                //var allies = subjectLordship.GetAllies();
                                //var attackableLordships =
                                //    subjectLordship.World.Lordships
                                //    .Where(l => !allies.Contains(l) && !double.IsPositiveInfinity(subjectLordship.LocationOfLordAndArmy.GetShortestAvailableDistanceToLordship(l, allies)))
                                //    .ToList();
                                var attackableLordships = subjectLordship.GetAttackableLordships().OrderBy(l=>l.MapX).ThenBy(l=>l.MapY).ToList();
                                var targetNumber = -1;
                                var attackCommand = "";
                                while (!(targetNumber >= 0 && targetNumber <= attackableLordships.Count()) && attackCommand.ToLower() != "x")
                                {
                                    for (var i = 0; i < attackableLordships.Count(); i++)
                                    {
                                        Console.WriteLine(
                                            string.Format(
                                                 "{0}. {1}({2},{3}) - {4} defenders",
                                                i + 1,
                                                attackableLordships[i].Name,
                                                attackableLordships[i].MapX,
                                                attackableLordships[i].MapY,
                                                attackableLordships[i].Defenders.Count()
                                            )
                                        );
                                    }
                                    Console.WriteLine("Enter the number of a lordship to attack or e[x]it.");
                                    attackCommand = Console.ReadLine();
                                    int.TryParse(attackCommand, out targetNumber);
                                }
                                if (targetNumber > 0)
                                {
                                    subjectLordship.AttacksLedThisYear++;
                                    var targetLordship = attackableLordships[targetNumber - 1];
                                    subjectLordship.Attack(targetLordship, rnd);
                                }
                                //Lordship objectLordship = null;
                                //if (command.Length == 3)
                                //{
                                //    objectLordship = world.Lordships.FirstOrDefault(l => l.Name.ToLower() == command[2].ToLower());
                                //}
                                //else if (command.Length == 4)
                                //{
                                //    int x;
                                //    int y;
                                //    if (int.TryParse(command[2], out x) && int.TryParse(command[3], out y))
                                //    {
                                //        objectLordship = world.Lordships.FirstOrDefault(l => l.MapX == x && l.MapY == y);
                                //    }
                                //}
                                //if (objectLordship != null)
                                //{
                                //    var attackingLordship = subjectLordship;
                                //    var targetLordship = objectLordship;
                                //    if (attackingLordship != null && targetLordship != null && !attackingLordship.Vacant && !targetLordship.Vacant)
                                //    {
                                //        attackingLordship.Attack(targetLordship, rnd);
                                //    }
                                //}
                            }
                        }
                        break;
                    //case "pledgefealty":
                    //    {
                    //        if (command.Length == 3)
                    //        {
                    //            var allegience = world.Lordships.FirstOrDefault(l => l.Name.ToLower() == command[2].ToLower());
                    //            if (allegience != null)
                    //            {
                    //                allegience.AddVassle(subjectLordship);
                    //            }
                    //        }
                    //    }
                    //    break;
                    case "r":
                        {
                            //Release Army
                            var releaseCommand = "";
                            var releaseArmyNumber = -1;
                            while (releaseCommand.ToLower()!="x" && !(releaseArmyNumber>=1 && releaseArmyNumber <= subjectLordship.OccupyingLordsAndArmies.Count()))
                            {
                                Console.WriteLine("Which lordship would " + subjectLordship.Name + " like to release? (or E[x]it)");
                                for (var i = 0; i < subjectLordship.OccupyingLordsAndArmies.Count(); i++)
                                {
                                    Console.WriteLine(
                                        string.Format(
                                            "{0}. {1} ({2},{3}), House {4}, {5} fighters in army",
                                            i+1,
                                            subjectLordship.OccupyingLordsAndArmies[i].Name,
                                            subjectLordship.OccupyingLordsAndArmies[i].MapX,
                                            subjectLordship.OccupyingLordsAndArmies[i].MapY,
                                            subjectLordship.OccupyingLordsAndArmies[i].Lord.House.Name,
                                            subjectLordship.OccupyingLordsAndArmies[i].Army.Count() 
                                        )
                                  );
                                }
                                releaseCommand = Console.ReadLine();
                                int.TryParse(releaseCommand, out releaseArmyNumber);
                            }
                            if (releaseArmyNumber >= 1 && releaseArmyNumber <= subjectLordship.OccupyingLordsAndArmies.Count())
                            {
                                var armyToRelease = subjectLordship.OccupyingLordsAndArmies[releaseArmyNumber - 1];
                                //armyToRelease.DeploymentRequest = 
                                House.DeploymentRequests.Add(new DeploymentRequest() { Destination = armyToRelease.Lord.Household.Lordship, NumberOfTroops = armyToRelease.Army.Count(), RequestingHouse = House, RequestedArmy=armyToRelease});
                                Console.WriteLine("Release: " + subjectLordship.Lord.House.Lord.FullNameAndAge + " HAS RELEASED the Army of " + armyToRelease.Lord.FullNameAndAge + " from " + subjectLordship.Name);
                            }
                        }
                        break;
                    case "s":
                    case "summon":
                        {
                            /*
                            if (command.Length == 3)
                            {
                                var summoned = world.Lordships.FirstOrDefault(l => l.Name.ToLower() == command[2].ToLower());
                                if (summoned != null)
                                {
                                    summoned.DestinationOfLordshipAndArmy = subjectLordship.LocationOfLordAndArmy;
                                }
                            }*/
                            var houseAndVasslLordships = 
                                subjectLordship.Lord.House.Lordships
                                .Where(l => l != subjectLordship)
                                .Union(subjectLordship.Lord.House.Vassles.SelectMany(v => v.Lordships))
                                .OrderBy(l=>l.MapX)
                                .ThenBy(l=>l.MapY)
                                .ToList();
                            //which lordship would you like to summon?
                            int lordshipIndex = -1;
                            var summonCommand = "";
                            while (summonCommand!="x" && !(lordshipIndex >= 1 && lordshipIndex <= houseAndVasslLordships.Count()))
                            {
                                Console.WriteLine("Which lordship would " + subjectLordship.Name + " like to summon? (or e[x]it)");
                                for (var i = 1; i <= houseAndVasslLordships.Count(); i++)
                                {
                                    Console.WriteLine(
                                        string.Format(
                                            "{0}. {1} ({2},{3}), House {4}, {5} eligible fighters - deployed in {6}({7},{8})",
                                            i,
                                            houseAndVasslLordships[i - 1].Name,
                                            houseAndVasslLordships[i - 1].MapX,
                                            houseAndVasslLordships[i - 1].MapY,
                                            houseAndVasslLordships[i - 1].Lord.House.Name,
                                            houseAndVasslLordships[i - 1].EligibleForConscription.Count() + houseAndVasslLordships[i - 1].Army.Count(), //those eligible for conscription will always be first-line defenders
                                            houseAndVasslLordships[i - 1].LocationOfLordAndArmy.Name,
                                            houseAndVasslLordships[i - 1].LocationOfLordAndArmy.MapX,
                                            houseAndVasslLordships[i - 1].LocationOfLordAndArmy.MapY
                                            )
                                  );
                                }
                                summonCommand = Console.ReadLine();
                                int.TryParse(summonCommand, out lordshipIndex);
                            }
                            if (lordshipIndex >= 1 && lordshipIndex <= houseAndVasslLordships.Count())
                            {
                                var summoned = houseAndVasslLordships[lordshipIndex - 1];
                                var numberOfTroopsToSummon = -1;
                                var numberToSummonCommand = "";
                                while (numberToSummonCommand!= "x" &&!(numberOfTroopsToSummon >= 0 && numberOfTroopsToSummon <= (summoned.EligibleForConscription.Count() + summoned.Army.Count())))
                                {
                                    Console.WriteLine(summoned.Name + " has " + (summoned.EligibleForConscription.Count() + summoned.Army.Count()) + " eligible fighters.  How many does " + subjectLordship.Name + " wish to summon?");
                                    numberToSummonCommand = Console.ReadLine();
                                    int.TryParse(numberToSummonCommand, out numberOfTroopsToSummon);
                                }
                                subjectLordship.Lord.House.DeploymentRequests.Add(new DeploymentRequest()
                                {
                                    RequestingHouse = subjectLordship.Lord.House,
                                    Destination = subjectLordship.LocationOfLordAndArmy,
                                    NumberOfTroops = numberOfTroopsToSummon,
                                    RequestedArmy = summoned
                                });
                                Console.WriteLine("SUMMONS: " + subjectLordship.Lord.House.Lord.FullNameAndAge + " HAS SUMMONED " + numberOfTroopsToSummon + " fighters from " + summoned.Lord.FullNameAndAge + " to " + subjectLordship.Name);
                            }
                        }
                        break;
                }
            }

        }
        public void VassleMenu(House subjectHouse)
        {
            //select vassle
            var selectVassleCommand = "";
            var selectedVassleNumber = -1;
            do
            {
                Console.WriteLine("Select vassle or e[x]it.");
                for (var i =0; i<subjectHouse.Vassles.Count(); i++)
                {
                    var vassle = subjectHouse.Vassles[i];
                    Console.WriteLine(
                        string.Format(
                            "{0}. House {1}, {2} fighters",
                            i+1,
                            vassle.Name,
                            vassle.Lordships.SelectMany(l=>l.Army.Where(s=>s.IsAlive).Union(l.EligibleForConscription)).Count()                            
                            )
                        );
                }
                selectVassleCommand = Console.ReadLine();
                int.TryParse(selectVassleCommand, out selectedVassleNumber);
                if (selectedVassleNumber >= 1 && selectedVassleNumber <= subjectHouse.Vassles.Count())
                {
                    var vassle = subjectHouse.Vassles[selectedVassleNumber - 1];
                    var vassleCommand = "";
                    do
                    {
                        Console.WriteLine(vassle.GetDetailsAsString());
                        Console.WriteLine("[R]elease Vassle, E[x]it");
                        vassleCommand = Console.ReadLine();
                    } while (vassleCommand != "x");
                }
            } while (selectVassleCommand.ToLower() != "x" && !(selectedVassleNumber >= 1 && selectedVassleNumber < subjectHouse.Vassles.Count()));
            //manipulate vassle
        }
        public void DoLivePlayerTurn(Random rnd)
        {
            var player = this;
            var world = player.House.World;
            Console.WriteLine("Year: " + world.Year);
            //Console.WriteLine(player.House.Seat.GetMapOfKnownWorld());
            Console.WriteLine("Enter to continue. CTRL-C to quit.");
            var input = "hacky";
            while (input.ToLower() != "i")
            {
                Console.WriteLine("Enter house  name or [i]ncrement year");
                input = Console.ReadLine();
                //var command = input.Split(' ');
                if (input.ToLower() != "i")
                {
                    var subjectHouse = world.NobleHouses.FirstOrDefault(h => h.Name.ToLower() == input.Trim().ToLower());
                    if (subjectHouse != null)
                    {
                        var houseCommand = "";
                        while (houseCommand.ToLower() != "x")
                        {
                            Console.WriteLine("House " + subjectHouse.Name + ": [D]etails, [N]obles, [M]ap, [L]ordships, [V]assles, E[x]it " + subjectHouse.Name);
                            houseCommand = Console.ReadLine();
                            switch (houseCommand.ToLower())
                            {
                                case "v":
                                    VassleMenu(subjectHouse);
                                    break;
                                case "d":
                                    {
                                        Console.WriteLine(subjectHouse.GetDetailsAsString());
                                    }
                                    break;
                                case "n":
                                    {
                                        var nobleHouseholds = subjectHouse.Lordships
                                            .SelectMany(l => l.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Noble))
                                            .OrderBy(h=>h.HeadofHousehold.House.Name)
                                            .ThenBy(h=>h.HeadofHousehold.Name)
                                            .ToList();
                                        var householdCommand = "";
                                        while (householdCommand.ToLower() != "x")
                                        {
                                            Console.WriteLine("Noble households of House " + subjectHouse.Name + ":");
                                            for (var i = 0; i < nobleHouseholds.Count(); i++)
                                            {
                                                Console.WriteLine(
                                                    string.Format(
                                                        "{0}. {1}",
                                                        i + 1,
                                                        nobleHouseholds[i].HeadofHousehold.FullNameAndAge
                                                        )
                                                    );
                                            }
                                            Console.WriteLine("Enter Household Number for more details or E[x]it");
                                            int householdNumber = -1;
                                            householdCommand = Console.ReadLine();
                                            int.TryParse(householdCommand, out householdNumber);
                                            if (householdNumber >= 1 && householdNumber <= nobleHouseholds.Count)
                                            {
                                                HouseholdMenu(nobleHouseholds[householdNumber - 1]);
                                            }
                                        }
                                    }
                                    break;
                                case "m":
                                    {
                                        Console.WriteLine(subjectHouse.Seat.GetMapOfKnownWorld());
                                    }
                                    break;
                                case "l":
                                    {
                                        var lordshipSelectionCommand = "";
                                        while (lordshipSelectionCommand.ToLower() != "x")
                                        {
                                            var selectedLordshipNumber = -1;
                                            Console.WriteLine("Select a lorship by number or e[x]it.");
                                            for (var i = 0; i < subjectHouse.Lordships.Count; i++)
                                            {
                                                Console.WriteLine(
                                                    string.Format(
                                                        "{0}. {1}({2},{3}) - {4} defenders",
                                                        i + 1,
                                                        subjectHouse.Lordships[i].Name,
                                                        subjectHouse.Lordships[i].MapX,
                                                        subjectHouse.Lordships[i].MapY,
                                                        subjectHouse.Lordships[i].Defenders.Count()
                                                        )
                                                    );
                                            }
                                            lordshipSelectionCommand = Console.ReadLine();
                                            int.TryParse(lordshipSelectionCommand, out selectedLordshipNumber);
                                            if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= subjectHouse.Lordships.Count)
                                            {
                                                LordshipMenu(subjectHouse.Lordships[selectedLordshipNumber - 1], rnd);
                                            }
                                        }

                                    }
                                    break;
                                }
                            }
                        }
                    }
                    
                }
            }

        }
    }

//}
