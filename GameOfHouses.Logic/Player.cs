using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameOfHouses.Logic.Utility;

namespace GameOfHouses.Logic
{
    public class Player
    {
        public Player()
        {
            House = null;
            VasslesSummonedThisTurn = new List<House>();
            HouseLordshipsSummonedThisTurn = new List<Lordship>();
            AttackHistory = new List<Lordship>();
            PlayerType = PlayerType.AISubmissive;
        }
        public List<House> VasslesSummonedThisTurn { get; set; }
        public List<Lordship> HouseLordshipsSummonedThisTurn { get; set; }
        public Guid Id { get; set; }
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
            VasslesSummonedThisTurn.Clear();
            HouseLordshipsSummonedThisTurn.Clear();
            switch (PlayerType)
            {
                case PlayerType.Live:
                    DoLivePlayerTurn(rnd);
                    break;
                case PlayerType.AIAggressive:
                    DoAggressivePlayerTurn(rnd);
                    break;
            }
            //House.History = "";
        }
        public void DoAggressivePlayerTurn(Random rnd)
        {
            if (!House.Vacant && House.Lordships.Count() > 0) {
                var people = House.Lord.People;
                var world = House.World;
                var nobleSubjects = House.Lordships.SelectMany(l => l.Households).SelectMany(h => h.Members).Where(m=>m.Class == SocialClass.Noble).ToList();
                //receive response on all accepted or rejected proposals
                world.Proposals.Where(p => p.Sender == House && (p.Status == ProposalStatus.Accepted || p.Status == ProposalStatus.Rejected)).ToList().ForEach(
                    p => p.Status = ProposalStatus.ResponseReceived
                );
                //accept all proposals
                var incomingProposals = world.Proposals.Where(p => p.Receiver == House && p.Status == ProposalStatus.New);
                foreach(var proposal in incomingProposals)
                {
                    //only people for now
                    if(nobleSubjects.Intersect(proposal.RequestedPeople).Count() == proposal.RequestedPeople.Count())
                    {
                        proposal.Accept();
                    } else
                    {
                        proposal.Reject();
                    }
                }
                //now see if you have any heirs to betroth
                var heirsLeftToBetroth = House.GetOrderOfSuccession(2).Where(heir => heir.IsEligableForBetrothal() && heir.Household.Lordship !=null && heir.Household.Lordship.Lord.House == House).ToList();
                //first see if you have anyone compatible in your subjects
                var remainingEligibleNobleSubjects = House.Lordships
                    .SelectMany(l => l.Households)
                    .SelectMany(h => h.Members)
                    .Where(
                        m => 
                        m.Class == SocialClass.Noble 
                        && m.People == people
                        &&!heirsLeftToBetroth.Contains(m)
                        && m.IsEligableForBetrothal()
                        ).ToList();

                var unmatchedHeirs = new List<Person>();
                foreach (var heir in heirsLeftToBetroth) {
                    var spouseToBe = remainingEligibleNobleSubjects.Where(s => s.IsCompatibleForBetrothal(heir)).FirstOrDefault();
                    if (spouseToBe != null)
                    {
                        world.CreateBethrothal(heir, spouseToBe, world.Year);
                        remainingEligibleNobleSubjects.Remove(spouseToBe);
                    } else
                    {
                        unmatchedHeirs.Add(heir);
                    }
                }
                //now see what we can get on the market for any unmatched heirs
                foreach(var heir in unmatchedHeirs)
                {
                    if (remainingEligibleNobleSubjects.Count() > 0)
                    {
                        var possibleSpouses = world.EligibleNobles.Where(s =>
                            //make sure there are no open proposals
                            !world.Proposals.SelectMany(p => p.OfferedPeople).Union(world.Proposals.SelectMany(p => p.RequestedPeople)).Contains(s)
                            && s.IsEligableForBetrothal()
                            && s.IsCompatibleForBetrothal(heir)
                            ).Take(20).ToList();

                        if (possibleSpouses.Count() > 0)
                        {
                            var proposedSpouse = possibleSpouses.Where(s => s.Age == possibleSpouses.Min(s2 => Math.Abs(s2.Age - heir.Age))).FirstOrDefault();
                            var nobleToTrade = remainingEligibleNobleSubjects[0];
                            remainingEligibleNobleSubjects.Remove(nobleToTrade);
                            if (proposedSpouse != null && proposedSpouse.Household.Lordship.Lord !=null)
                            {
                                world.Proposals.Add(
                                    new Proposal()
                                    {
                                        Sender = House,
                                        Receiver = proposedSpouse.Household.Lordship.Lord.House,
                                        RequestedPeople = new List<Person>() { proposedSpouse },
                                        OfferedPeople = new List<Person>() { nobleToTrade }
                                    }
                                );
                            }
                        }
                    } 
                }
                world.EligibleNobles = world.EligibleNobles.Union(remainingEligibleNobleSubjects).ToList();
            }
            //attack
            if (!House.Vacant && House.Lord.Household!=null  && House.Lord.Household.Lordship !=null && House.Lord.Household.Lordship.Lord != null && House.Lord.Household.Lordship.Lord.House == House /*&& House.LordshipCandidates.Count() > 0*/)
            {

                var commandingLordship = House.Lord.Household.Lordship;
                var attackers = new List<Lordship>();
                attackers.AddRange(House.Lordships);
                HouseLordshipsSummonedThisTurn.AddRange(House.Lordships);
                attackers.AddRange(House.GetAllSubVassles().SelectMany(v => v.Lordships));
                VasslesSummonedThisTurn = House.Vassles.ToList();
                var attackableLordships = commandingLordship.GetAttackableLordships();
                if (attackableLordships.Count() > 0)
                {
                   var allies = commandingLordship.GetAllies();
                   var distancesToAttackableLordships = commandingLordship.GetShortestAvailableDistanceToLordship(allies.Union(attackableLordships).ToList());
                    var target =
                        attackableLordships
                        //don't attack anything you've attacked in the last 10 turns
                        .Where(t =>
                            !AttackHistory.Skip(Math.Max(0, AttackHistory.Count() - 10)).Contains(t)
                           && t.Lord!=null 
                            && t.Lord.House.Allegience == null
                            && t.Defenders.Count() < 0.66 * attackers.SelectMany(a => a.Army.Where(s => s.IsAlive)).Count()
                            )
                        .OrderBy(t => distancesToAttackableLordships[t])
                        .FirstOrDefault();

                    if (target == null)
                    {
                        //find the closest conquorable target to a capital
                        var conquorableLordships = attackableLordships.Where(t =>
                            !AttackHistory.Skip(Math.Max(0, AttackHistory.Count() - 10)).Contains(t)
                            //&& t.Lord.House.Allegience == null
                            && t.Defenders.Count() < 0.33 * attackers.SelectMany(a => a.Army.Where(s => s.IsAlive)).Count()
                        ).ToList();
                        if (conquorableLordships.Count() > 0)
                        {
                            var capitals = House.World.NobleHouses.Where(h => h.Seat!=null && h.Allegience == null && h.Lordships.Count() > 0).Select(h => h.Seat).ToList();
                            var closestDistance = double.PositiveInfinity;
                            target = conquorableLordships[0];

                            foreach (var lordship in conquorableLordships)
                            {
                                foreach (var capital in capitals)
                                {
                                    var distance = Math.Sqrt(Math.Pow(capital.MapX - lordship.MapX, 2) + Math.Pow(capital.MapY - lordship.MapY, 2));
                                    if (distance < closestDistance)
                                    {
                                        closestDistance = distance;
                                        target = lordship;
                                    }
                                }
                            }
                        }
                    }
                    if (target != null)
                    {
                        AttackHistory.Add(target);
                        Attack(commandingLordship, attackers, target, rnd, false,false,true,1,false);
                    }
                }
            }
        }
        public void PersonMenu(Person person)
        {
            var command = "";
            Console.Write(person.GetDetailsAsString());
            while (command.ToLower() != "x")
            {
                Console.WriteLine("[D]etails, [B]etroth, [K]ill, [C]ompatible Foreign Nobles, [P]ropose Trade or E[x]it");
                command = Console.ReadLine();
                switch (command.ToLower())
                {
                    case "p":
                        {
                            var proposalCommand = "";
                            var subjectToTradeNumber = -1;
                            var world = House.World;
                            Person subjectToTrade = null;
                            var subjects =
                                House.Lordships.SelectMany(l => l.Households)
                                .Where(h => h.HeadofHousehold.Class == SocialClass.Noble)
                                .SelectMany(h => h.Members)
                                .OrderBy(m=>m.Age)
                                .ToList();

                            do
                            {
                                Console.WriteLine("Choose a person or E[x]it");
                                for (var i = 0; i < subjects.Count(); i++)
                                {
                                    Console.WriteLine(string.Format("{0}. {1}", i + 1, subjects[i].FullNameAndAge));
                                }
                                proposalCommand = Console.ReadLine();
                                int.TryParse(proposalCommand, out subjectToTradeNumber);
                                if (subjectToTradeNumber > 0 && subjectToTradeNumber <= subjects.Count())
                                {
                                    subjectToTrade = subjects[subjectToTradeNumber - 1];
                                }
                            }
                            while (subjectToTrade == null && proposalCommand.ToLower() != "x");
                            if (subjectToTrade != null)
                            {
                                world.Proposals.Add(
                                   new Proposal()
                                   {
                                       Sender = House,
                                       Receiver = person.Household.Lordship.Lord.House,
                                       RequestedPeople = new List<Person>() { person },
                                       OfferedPeople = new List<Person>() { subjectToTrade }
                                   }
                               );
                            }

                        }
                        break;
                    case "c":
                        {
                            var compatibleCommand = "";
                            var compatiblePersonNumber = -1;
                            var world = House.World;
                            Person compatiblePerson = null;
                            var compatiblePersons =
                                world.Population.Where(n =>
                                n.Class == SocialClass.Noble &&
                                n.People == person.People &&
                                !world.Proposals.SelectMany(p => p.OfferedPeople).Union(world.Proposals.SelectMany(p => p.RequestedPeople)).Contains(n)
                                && n.IsCompatibleForBetrothal(person)
                                ).OrderBy(n=>n.Age).ToList();

                            do
                            {
                                Console.WriteLine("Choose a person or E[x]it");
                                for (var i = 0; i < compatiblePersons.Count(); i++)
                                {
                                    Console.WriteLine(string.Format("{0}. {1}", i + 1, compatiblePersons[i].FullNameAndAge));
                                }
                                compatibleCommand = Console.ReadLine();
                                int.TryParse(compatibleCommand, out compatiblePersonNumber);
                                if (compatiblePersonNumber > 0 && compatiblePersonNumber <= compatiblePersons.Count())
                                {
                                    compatiblePerson = compatiblePersons[compatiblePersonNumber - 1];
                                }
                            }
                            while (compatiblePerson == null && compatibleCommand.ToLower() != "x");
                            if (compatiblePerson != null)
                            {
                                PersonMenu(compatiblePerson);
                            }
                        }
                        break;
                    case "d":
                        Console.WriteLine(person.GetDetailsAsString());
                        break;
                    case "k":
                        if (person.Household.Lordship.Lord.House == House)
                        {
                            Console.WriteLine("EXECUTION: " + person.FullNameAndAge + " has been exectued!");
                            person.Kill();
                        } else
                        {
                            Console.Write("You do not have juristiction.");
                        }
                        break;
                    case "b":
                        if (person.Household.Lordship.Lord.House == House)
                        {
                            var betrothalCommand = "";
                            var spouseNumber = -1;
                            Person spouse = null;
                            var eligibleSpouses =
                                person.Household.Lordship.Lord.House.Lordships
                                .SelectMany(
                                    l => l.Households.Where(
                                        h => h.HeadofHousehold.Class == SocialClass.Noble)
                                        .SelectMany(h => h.Members.Where(m => m.IsEligableForBetrothal() && m.IsCompatibleForBetrothal(person))
                                        )
                                 )
                                 .OrderByDescending(p => p.Age)
                                 .ThenBy(p=>p.Name)
                                 .ToList();
                             
                            do
                            {
                                Console.WriteLine("Choose a spouse for " + person.FullNameAndAge + ", or E[x]it");
                                for (var i = 0; i < eligibleSpouses.Count(); i++)
                                {
                                    Console.WriteLine(string.Format("{0}. {1}", i+1, eligibleSpouses[i].FullNameAndAge));
                                }
                                betrothalCommand = Console.ReadLine();
                                int.TryParse(betrothalCommand, out spouseNumber);
                                if (spouseNumber>0 && spouseNumber <= eligibleSpouses.Count())
                                {
                                    spouse = eligibleSpouses[spouseNumber - 1];
                                }
                            }
                            while (spouse == null && betrothalCommand.ToLower() != "x");
                            if (spouse != null)
                            {
                                person.World.CreateBethrothal(person, spouse, person.World.Year);
                                Console.WriteLine(string.Format("BETROTHAL: {0} was betrothed to {1}", person.FullNameAndAge, spouse.FullNameAndAge));
                            }
                        }
                        else
                        {
                            Console.Write("You do not have juristiction.");
                        }
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
                Console.WriteLine("Household [D]etails, [M]ember Details, [R]esettle, [K]ill, [A]ttainder, or E[x]it");
                individualHouseholdCommand = Console.ReadLine();
                switch (individualHouseholdCommand.ToLower())
                {
                    case "a":
                        {
                            foreach (var member in household.Members)
                            {
                                member.Class = SocialClass.Peasant;
                                var message = "ATTAINDER: " + member.FullNameAndAge + " was ATTAINED, stripped of all titles and nobility, by " + member.Household.Lordship.Lord.House.Lord.FullNameAndAge + "\n";
                                member.House.RecordHistory(message);
                                member.Household.Lordship.Lord.House.RecordHistory(message);
                                Console.Write(message);
                            }
                        }break;
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
                        var subjectHouse = household.Lordship.Lord.House;
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
                    case "k":
                        var membersToKill = household.Members.ToList();
                        foreach(var person in membersToKill)
                        {
                            Console.WriteLine("EXECUTION: " + person.FullNameAndAge + " has been exectued!");
                            person.Kill();
                        }
                        break;
                }
            }

        }
        public void ChangeLordshipLordMenu(Lordship lordship)
        {
            var possibleLords = lordship.Lord.House.Lordships
                .SelectMany(l=>l.Households.Where(h=>h.HeadofHousehold.Class == SocialClass.Noble))
                .SelectMany(h=>h.Members.Where(m=>m.Age >= Constants.AGE_OF_MAJORITY))
                .ToList();
            //if (possibleLords.Count() > 0)
            //{
            Person newLord = null;
            //    if (getLiveInput)
            //    {
            var memberNumber = -1;
            var memberCommand = "";
            while (newLord == null && memberCommand.ToLower() != "x")
            {
                Console.WriteLine("Enter number of new Lord of " + lordship.Name);
                for (var i = 0; i < possibleLords.Count(); i++)
                {
                    Console.WriteLine(
                        string.Format(
                            "{0}. {1}",
                            i + 1,
                            possibleLords[i].FullNameAndAge
                            )
                        );
                }
                memberCommand = Console.ReadLine();
                int.TryParse(memberCommand, out memberNumber);
                if (memberNumber >= 1 && memberNumber <= possibleLords.Count())
                {
                    newLord = possibleLords[memberNumber - 1];
                }
            }
            if (newLord != null)
            {
                lordship.AddLord(newLord);
                var lordsOldHousehold = newLord.Household;
                Household lordsNewHousehold = null;
                if (newLord.Household.HeadofHousehold == newLord)
                {
                    lordsNewHousehold = lordsOldHousehold;
                }
                else
                {
                    lordsNewHousehold = new Household();
                    lordsNewHousehold.AddMember(newLord);
                    lordsNewHousehold.HeadofHousehold = newLord;
                    lordsOldHousehold.Lordship.AddHousehold(lordsNewHousehold);
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
                    //case "n":
                    //    {
                    //        var subjectHouse = subjectLordship.Lord.House;
                    //        var nobleHouseholds = subjectHouse.Lordships
                    //            .SelectMany(l => l.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Noble))
                    //            .OrderBy(h=>h.HeadofHousehold.House.Name)
                    //            .ThenBy(h=>h.HeadofHousehold.Name)
                    //            .ToList();
                    //        var householdCommand = "";
                    //        while (householdCommand.ToLower() != "x")
                    //        {
                    //            Console.WriteLine("Noble households of House " + subjectHouse.Name + ":");
                    //            for (var i = 0; i < nobleHouseholds.Count(); i++)
                    //            {
                    //                Console.WriteLine(
                    //                    string.Format(
                    //                        "{0}. {1}",
                    //                        i + 1,
                    //                        nobleHouseholds[i].HeadofHousehold.FullNameAndAge
                    //                        )
                    //                    );
                    //            }
                    //            Console.WriteLine("Enter Household Number for more details or E[x]it");
                    //            int householdNumber = -1;
                    //            householdCommand = Console.ReadLine();
                    //            int.TryParse(householdCommand, out householdNumber);
                    //            if (householdNumber >= 1 && householdNumber <= nobleHouseholds.Count)
                    //            {
                    //                HouseholdMenu(nobleHouseholds[householdNumber - 1]);
                    //            }
                    //        }
                    //    }
                    // break;
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
                    case "s":
                        {
                            var targetNumber = -1;
                            var scoutCommand = "";
                            var visibleLordships = subjectLordship.GetVisibleLordships().OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
                            Lordship lordshipToScout = null;
                            while (lordshipToScout == null && scoutCommand.ToLower() != "x")
                            {
                                for (var i = 0; i < visibleLordships.Count(); i++)
                                {
                                    Console.WriteLine(
                                        string.Format(
                                             "{0}. {1}({2},{3}) - {4} defenders",
                                            i + 1,
                                            visibleLordships[i].Name,
                                            visibleLordships[i].MapX,
                                            visibleLordships[i].MapY,
                                            visibleLordships[i].Defenders.Count()
                                        )
                                    );
                                }
                                Console.WriteLine("Enter the number of a lordship to scout or e[x]it.");
                                scoutCommand = Console.ReadLine();
                                int.TryParse(scoutCommand, out targetNumber);
                                if(targetNumber > 0 && targetNumber <= visibleLordships.Count())
                                {
                                    lordshipToScout = visibleLordships[targetNumber - 1];
                                    Console.WriteLine(lordshipToScout.GetDetailsAsString());
                                }
                            }
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
                    //case "c":
                    //case "conscript":
                    //    {
                    //        int soldiersToConscript = int.MaxValue;
                    //        string prompt = "";
                    //        while ((!int.TryParse(prompt, out soldiersToConscript)) || !(soldiersToConscript <= subjectLordship.EligibleForConscription.Count()))
                    //        {
                    //            Console.Write(
                    //                subjectLordship.Name + " has " + subjectLordship.Farmers.Count(f => f.IsAlive) + " farmers and " + subjectLordship.Army.Count(s => s.IsAlive) + " soldiers.\n"
                    //                + "There are " + subjectLordship.EligibleForConscription.Count() + " subjects available for conscription. \n"
                    //                + "How many soldiers would you like to conscript?\n"
                    //                );
                    //            prompt = Console.ReadLine();
                    //        }
                    //        subjectLordship.ConscriptSoldiers(soldiersToConscript);

                    //        Console.Write(string.Format(
                    //            "{0} conscripted {1} soldiers into the army of {2}.\n"
                    //            + "{2} now has {3} farmers and {4} soldiers.\n",
                    //            subjectLordship.Lord.FullNameAndAge,
                    //            soldiersToConscript,
                    //            subjectLordship.Name,
                    //            subjectLordship.Farmers.Count(f => f.IsAlive),
                    //            subjectLordship.Army.Count(s => s.IsAlive)
                    //            ));
                    //    }
                    //    break;
                    //case "g":
                    //case "discharge":
                    //    {
                    //        int soldiersToDischarge = int.MaxValue;
                    //        string prompt = "";
                    //        while ((!int.TryParse(prompt, out soldiersToDischarge)) || !(soldiersToDischarge <= subjectLordship.Army.Count(s => s.IsAlive)))
                    //        {
                    //            Console.Write(string.Format(
                    //                "{0} has {1} farmers and {2} soldiers.\n"
                    //                + "How many soldiers would you like to discharge?\n",
                    //                subjectLordship.Name,
                    //                subjectLordship.Farmers.Count(f => f.IsAlive),
                    //                subjectLordship.Army.Count(s => s.IsAlive)
                    //                ));
                    //            prompt = Console.ReadLine();
                    //        }
                    //        subjectLordship.DischargeSoldiers(soldiersToDischarge);
                    //        Console.Write(
                    //            "{0} discharged {1} soldiers into the army of {2}.\n"
                    //            + "{2} now has {3} farmers and {4} soldiers.\n",
                    //            subjectLordship.Lord.FullNameAndAge,
                    //            soldiersToDischarge,
                    //            subjectLordship.Name,
                    //            subjectLordship.Farmers.Count(),
                    //            subjectLordship.Army.Count()
                    //            );
                    //    }
                    //    break;
                    case "a":
                    case "attack":
                        {
                            //if (!subjectLordship.LordIsInResidence)
                            //{
                            //    Console.WriteLine(subjectLordship.Name + " cannot attack because its lord is not in residence.");
                            //}
                            //else 
                            if (HouseLordshipsSummonedThisTurn.Contains(subjectLordship))
                            {
                                Console.WriteLine(subjectLordship.Name + " has already led the maximum nmber of attacks for this year.");
                            }
                            else
                            {
                                var abortAttack = false;
                                var attackableLordships = subjectLordship.GetAttackableLordships().OrderBy(l=>l.MapX).ThenBy(l=>l.MapY).ToList();
                                var targetNumber = -1;
                                var attackCommand = "";
                                while (!abortAttack && !(targetNumber >= 0 && targetNumber <= attackableLordships.Count()) && attackCommand.ToLower() != "x")
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
                                    if (attackCommand.ToLower() == "x") { abortAttack = true; }
                                }
                                Lordship target = targetNumber > 0?attackableLordships[targetNumber-1]:null;
                                var availableVassles = House.Vassles.Where(v => !VasslesSummonedThisTurn.Contains(v)).ToList();
                                var summmonedVassles = new List<House>();
                                var vassleNumber = -1;
                                var vassleCommand = "";
                                while (!abortAttack && target != null && availableVassles.Count() > 0 && vassleCommand.ToLower() != "n" && vassleCommand.ToLower() != "x")
                                {
                                    for (var i = 0; i < availableVassles.Count(); i++) {
                                        Console.WriteLine(
                                            string.Format(
                                                 "{0}. House {1}, {2} fighters",
                                                i + 1,
                                                availableVassles[i].Name,
                                                availableVassles[i].Lordships.SelectMany(l => l.Army)
                                                    .Union(availableVassles[i].GetAllSubVassles().SelectMany(v => v.Lordships.SelectMany(l => l.Army))).Count()
                                            )
                                        );
                                    }
                                    Console.WriteLine("Enter the number of a vassle to summon, [n]ext when done, summon [a]ll, or e[x]it.");
                                    vassleCommand = Console.ReadLine();
                                    int.TryParse(vassleCommand, out vassleNumber);
                                    if (vassleCommand.ToLower() == "x") { abortAttack = true;}
                                    if (vassleCommand.ToLower() == "a")
                                    {
                                        summmonedVassles.AddRange(availableVassles);
                                        availableVassles.Clear();
                                    }
                                    if (vassleNumber > 0 && vassleNumber <= availableVassles.Count())
                                    {
                                        var summonedVassle = availableVassles[vassleNumber-1];
                                        summmonedVassles.Add(summonedVassle);
                                        availableVassles.Remove(summonedVassle);
                                    }
                                }
                                var availableHouseLorsdships = House.Lordships.Where(l => l!=subjectLordship && !HouseLordshipsSummonedThisTurn.Contains(l)).ToList();
                                var summmonedHouseLordships = new List<Lordship>();
                                var houseLordshipNumber = -1;
                                var houseLordshipCommand = "";
                                while (!abortAttack && target != null  && availableHouseLorsdships.Count()>0 && houseLordshipCommand.ToLower() != "n" && houseLordshipCommand.ToLower() != "x")
                                {
                                    for (var i = 0; i < availableHouseLorsdships.Count(); i++)
                                    {
                                        Console.WriteLine(
                                            string.Format(
                                                 "{0}. {1}({2},{3}) - {4} in army",
                                                i + 1,
                                                availableHouseLorsdships[i].Name,
                                                availableHouseLorsdships[i].MapX,
                                                availableHouseLorsdships[i].MapY,
                                                availableHouseLorsdships[i].Army.Count()
                                            )
                                        );
                                    }
                                    Console.WriteLine("Enter the number of a lordship to summon, [n]ext when done, summon [a]ll, or e[x]it.");
                                    houseLordshipCommand = Console.ReadLine();
                                    int.TryParse(houseLordshipCommand, out houseLordshipNumber);
                                    if (houseLordshipCommand.ToLower() == "x") { abortAttack = true; }
                                    if (houseLordshipCommand.ToLower() == "a")
                                    {
                                        summmonedHouseLordships.AddRange(availableHouseLorsdships);
                                        availableHouseLorsdships.Clear();
                                    }
                                    if (houseLordshipNumber > 0 && houseLordshipNumber <= availableHouseLorsdships.Count())
                                    {
                                        var summondedHouseholdLordship = availableHouseLorsdships[houseLordshipNumber - 1];
                                        summmonedHouseLordships.Add(summondedHouseholdLordship);
                                        availableHouseLorsdships.Remove(summondedHouseholdLordship);
                                    }
                                }
                                if(!abortAttack && target!=null)
                                {
                                    //run attack
                                    HouseLordshipsSummonedThisTurn.AddRange(summmonedHouseLordships);
                                    HouseLordshipsSummonedThisTurn.Add(subjectLordship);
                                    VasslesSummonedThisTurn.AddRange(summmonedVassles);
                                    var attackers = new List<Lordship>();
                                    attackers.Add(subjectLordship);
                                    attackers.AddRange(summmonedHouseLordships);
                                    attackers.AddRange(summmonedVassles.SelectMany(v => v.Lordships.Union(v.GetAllSubVassles().SelectMany(sv=>sv.Lordships))));
                                    Attack(subjectLordship, attackers, target, rnd, true);
                                }
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
                    //case "s":
                    //case "summon":
                    //    {
                    //        /*
                    //        if (command.Length == 3)
                    //        {
                    //            var summoned = world.Lordships.FirstOrDefault(l => l.Name.ToLower() == command[2].ToLower());
                    //            if (summoned != null)
                    //            {
                    //                summoned.DestinationOfLordshipAndArmy = subjectLordship.LocationOfLordAndArmy;
                    //            }
                    //        }*/
                    //        var houseAndVasslLordships = 
                    //            subjectLordship.Lord.House.Lordships
                    //            //.Where(l => l != subjectLordship)
                    //            .Union(subjectLordship.Lord.House.GetAllSubVassles().SelectMany(v => v.Lordships))
                    //            .OrderBy(l=>l.MapX)
                    //            .ThenBy(l=>l.MapY)
                    //            .ToList();
                    //        //which lordship would you like to summon?
                    //        int lordshipIndex = -1;
                    //        var summonCommand = "";
                    //        while (summonCommand!="x" && !(lordshipIndex >= 1 && lordshipIndex <= houseAndVasslLordships.Count()))
                    //        {
                    //            Console.WriteLine("Which lordship would " + subjectLordship.Name + " like to summon? (or e[x]it)");
                    //            for (var i = 1; i <= houseAndVasslLordships.Count(); i++)
                    //            {
                    //                Console.WriteLine(
                    //                    string.Format(
                    //                        "{0}. {1} ({2},{3}), House {4}, {5} fighters - deployed in {6}({7},{8})",
                    //                        i,
                    //                        houseAndVasslLordships[i - 1].Name,
                    //                        houseAndVasslLordships[i - 1].MapX,
                    //                        houseAndVasslLordships[i - 1].MapY,
                    //                        houseAndVasslLordships[i - 1].Lord.House.Name,
                    //                        houseAndVasslLordships[i - 1].Army.Count(), 
                    //                        houseAndVasslLordships[i - 1].LocationOfLordAndArmy.Name,
                    //                        houseAndVasslLordships[i - 1].LocationOfLordAndArmy.MapX,
                    //                        houseAndVasslLordships[i - 1].LocationOfLordAndArmy.MapY
                    //                        )
                    //              );
                    //            }
                    //            summonCommand = Console.ReadLine();
                    //            int.TryParse(summonCommand, out lordshipIndex);
                    //        }
                    //        int destinationIndex = -1;
                    //        summonCommand = "";
                    //        Lordship summoned = null;
                    //        Lordship destination = null;
                    //        if (lordshipIndex > 0) { summoned = houseAndVasslLordships[lordshipIndex - 1]; }
                    //        while (summoned != null && destination == null && summonCommand != "x" && !(destinationIndex >= 1 && destinationIndex <= houseAndVasslLordships.Count()))
                    //        {
                    //            var allies = summoned.GetAllies();
                    //            allies.Add(summoned);
                    //            allies = allies.OrderBy(a => a.MapX).ThenBy(a => a.MapY).ToList();
                    //            Console.WriteLine("Where do you want to send " + summoned.Name + "? ["+ subjectLordship.Name +"] (or e[x]it)");
                                
                    //            for (var i = 1; i <= allies.Count(); i++)
                    //            {
                    //                Console.WriteLine(
                    //                    string.Format(
                    //                        "{0}. {1} ({2},{3}), House {4}, {5} fighters - deployed in {6}({7},{8})",
                    //                        i,
                    //                        allies[i - 1].Name,
                    //                        allies[i - 1].MapX,
                    //                        allies[i - 1].MapY,
                    //                        allies[i - 1].Lord.House.Name,
                    //                        allies[i - 1].Army.Count(), //those eligible for conscription will always be first-line defenders
                    //                        allies[i - 1].LocationOfLordAndArmy.Name,
                    //                        allies[i - 1].LocationOfLordAndArmy.MapX,
                    //                        allies[i - 1].LocationOfLordAndArmy.MapY
                    //                        )
                    //              );
                    //            }
                    //            summonCommand = Console.ReadLine();
                    //            if (summonCommand == "")
                    //            {
                    //                destination = subjectLordship;
                    //            }
                    //            else
                    //            {
                    //                int.TryParse(summonCommand, out destinationIndex);
                    //                if (destinationIndex > 0)
                    //                {
                    //                    destination = allies[destinationIndex - 1];
                    //                } 
                    //            }
                    //        }
                    //        if (summoned!=null & destination !=null)
                    //        {
                    //            var numberOfTroopsToSummon = -1;
                    //            //var numberToSummonCommand = "";
                    //            //while (numberToSummonCommand!= "x" &&!(numberOfTroopsToSummon >= 0 && numberOfTroopsToSummon <= (summoned.EligibleForConscription.Count() + summoned.Army.Count())))
                    //            //{
                    //            //    Console.WriteLine(summoned.Name + " has " + (summoned.EligibleForConscription.Count() + summoned.Army.Count()) + " eligible fighters.  How many does " + subjectLordship.Name + " wish to summon?");
                    //            //    numberToSummonCommand = Console.ReadLine();
                    //            //    int.TryParse(numberToSummonCommand, out numberOfTroopsToSummon);
                    //            //}
                    //            subjectLordship.Lord.House.DeploymentRequests.Add(new DeploymentRequest()
                    //            {
                    //                RequestingHouse = subjectLordship.Lord.House,
                    //                Destination = destination,
                    //                NumberOfTroops = numberOfTroopsToSummon,
                    //                RequestedArmy = summoned
                    //            });
                    //            Console.WriteLine("SUMMONS: " + subjectLordship.Lord.House.Lord.FullNameAndAge + " HAS SUMMONED the army of " + summoned.Name + " from " + summoned.Lord.FullNameAndAge + " to " + subjectLordship.Name);
                    //        }
                    //    }
                    //    break;
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
                            vassle.Lordships.SelectMany(l=>l.Army).Count()                            
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
            //Console.WriteLine("Enter to continue. CTRL-C to quit.");
            //var input = "hacky";
            //while (input.ToLower() != "i")
            //{
                //Console.WriteLine("Enter house  name or [i]ncrement year");
                //input = Console.ReadLine();
                //var command = input.Split(' ');
                //if (input.ToLower() != "i")
                //{
                    //var subjectHouse = world.NobleHouses.FirstOrDefault(h => h.Name.ToLower() == input.Trim().ToLower());
                    var subjectHouse = player.House;
                    if (subjectHouse != null)
                    {
                if (subjectHouse.History.ContainsKey(world.Year - 1))
                {
                    Console.WriteLine(world.Year);
                    Console.Write(subjectHouse.History[world.Year - 1]);
                }
                if (subjectHouse.History.ContainsKey(world.Year))
                {
                    Console.WriteLine(world.Year);
                    Console.Write(subjectHouse.History[world.Year]);
                }
                var houseCommand = "";
                        while (houseCommand.ToLower() != "x")
                        {
                            Console.WriteLine("House " + subjectHouse.Name + " ("+ subjectHouse.FightersAvailable.Count() +" fighters): [H]istory, [D]etails, [S]ubjects, [M]ap, [L]ordships, [V]assles, E[x]it " + subjectHouse.Name);
                            houseCommand = Console.ReadLine();
                            switch (houseCommand.ToLower())
                            {
                                case "w":
                                    Console.WriteLine(world.GetMapAsString());
                                    Console.WriteLine(world.GetDetailsAsString());
                                    break;
                                case "v":
                                    VassleMenu(subjectHouse);
                                    break;
                                case "d":
                                    {
                                        Console.WriteLine(subjectHouse.GetDetailsAsString());
                                    }
                                    break;
                        case "h":
                            {
                                foreach (var recordedYear in subjectHouse.History)
                                {
                                    Console.WriteLine(recordedYear.Key);
                                    Console.WriteLine(recordedYear.Value);
                                }
                            }
                            break;
                        case "s":
                            {
                                var householdCommand = "";
                                while (householdCommand.ToLower() != "x")
                                {
                                    var nobleHouseholds = subjectHouse.Lordships
                                        .SelectMany(l => l.Households.Where(h => h.Members.Count() > 0 && h.HeadofHousehold.Class == SocialClass.Noble))
                                        .OrderBy(h => h.HeadofHousehold.House.Name)
                                        .ThenBy(h => h.HeadofHousehold.Name)
                                        .ToList();
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
                                                        "{0}. {1}({2},{3}) - {4} fighters",
                                                        i + 1,
                                                        subjectHouse.Lordships[i].Name,
                                                        subjectHouse.Lordships[i].MapX,
                                                        subjectHouse.Lordships[i].MapY,
                                                        subjectHouse.Lordships[i].Army.Count()
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
                    //}
                    
                //}
            }
        public List<Lordship> AttackHistory { get; set; }
        }
    }

//}
