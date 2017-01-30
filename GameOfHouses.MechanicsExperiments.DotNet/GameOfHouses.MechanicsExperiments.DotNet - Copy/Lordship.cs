using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public class Lordship
    {
        private Random _rnd;
        private int _maxYeild;
        public Lordship(Random rnd)
        {
            _rnd = rnd;
            Households = new List<Household>();
            Lords = new List<Person>();
            Wealth = 0;
            Surplus = 0;
            _maxYeild = _rnd.Next(Constants.MAX_YEILD_LOW, Constants.MAX_YEILD_HIGH);
            AssessedIncome = new List<AssessedIncome>();
            //Vassles = new List<Lordship>();
            Vacant = true;
            PlayerMoves = new List<Player>();
            Army = new List<Person>();
            Population = new List<Person>();
        }
        public Person Lord
        {
            get
            {
                if (!Vacant)
                {
                    return Lords.Last();
                }
                else
                {
                    return null;
                }
            }
        }
        public List<Person> Population { get; set; }
        public World World { get; set; }
        public List<Person> Army { get; set; }
        public void ConscriptSoldiers(int totalToConscript)
        {
            var eligibleForConcription = EligibleForConscription.ToList();
            var conscripts = new List<Person>();
            while (conscripts.Count() < totalToConscript && eligibleForConcription.Count() > 0)
            {
                var conscript = eligibleForConcription[_rnd.Next(0, eligibleForConcription.Count())];
                conscript.Profession = Profession.Soldier;
                eligibleForConcription.Remove(conscript);
                conscripts.Add(conscript);
                conscript.MoveToLocation(LocationOfLordAndArmy);
            }
            Army.AddRange(conscripts);
        }
        public List<Person> EligibleForConscription
        {
            get
            {
                return Households.SelectMany(h => h.Members.Where(
                    m =>
                    m.IsAlive
                    && m.Class == SocialClass.Peasant
                    && m.Age >= Constants.AGE_OF_MAJORITY
                    && m.Age <= Constants.AGE_OF_RETIREMENT
                    && m.Profession != Profession.Soldier
                    )).ToList();
            }
        }
        public List<Person> Farmers
        {
            get
            {
                return Households.SelectMany(h => h.Members.Where(
                    m =>
                    m.IsAlive
                    && m.Class == SocialClass.Peasant
                    && m.Age >= Constants.AGE_OF_MAJORITY
                    && m.Age <= Constants.AGE_OF_RETIREMENT
                    && m.Profession == Profession.Peasant
                    )).ToList();
            }
        }
        public void DischargeSoldiers(int totalToDischarge)
        {
            var eligibleForDischarge = Army.Where(s => s.IsAlive).ToList();
            var dischargedSoldiers = new List<Person>();
            while (eligibleForDischarge.Count() > 0 && dischargedSoldiers.Count() <= totalToDischarge)
            {
                var dischargedSoldier = eligibleForDischarge[_rnd.Next(0, eligibleForDischarge.Count())];
                dischargedSoldier.Profession = Profession.Peasant;
                eligibleForDischarge.Remove(dischargedSoldier);
                dischargedSoldiers.Add(dischargedSoldier);
                dischargedSoldier.MoveToLocation(dischargedSoldier.Household.Lordship);
            }
            Army.RemoveAll(s => dischargedSoldiers.Contains(s));
        }
        //public List<Lordship> Vassles { get; set; }
        public List<Player> PlayerMoves { get; set; }
        //public Lordship Allegience { get; set; }
        public List<Person> GetReinforcements()
        {
            return Defenders;
        }
        //public void AddVassle(Lordship vassle)
        //{
        //    if (!Vassles.Contains(vassle))
        //    {
        //        if (vassle.Allegience != null)
        //        {
        //            vassle.Allegience.RemoveVassle(vassle);
        //            if (vassle.GetAllSubVassles().Contains(this))
        //            {
        //                //can't have circular allegience
        //                this.Allegience.RemoveVassle(this);
        //            }
        //        }
        //        Vassles.Add(vassle);
        //        vassle.Allegience = this;
        //    }
        //}
        //public void RemoveVassle(Lordship vassle)
        //{
        //    if (Vassles.Contains(vassle))
        //    {
        //        if (vassle.Allegience != null)
        //        {
        //            vassle.Allegience = null;
        //        }
        //        Vassles.Remove(vassle);
        //    }
        //}
        //public List<Lordship> GetAllSubVassles()
        //{
        //    var subVassles = new List<Lordship>();
        //    foreach (var vassle in Vassles)
        //    {
        //        subVassles.Add(vassle);
        //        subVassles.AddRange(vassle.GetAllSubVassles());
        //    }
        //    return subVassles;
        //}
        public List<Household> GetPotentialSettlerLordHouseholds()
        {
            var potentialSettlerLordHouseholds = Households.Where(h =>
            h.HeadofHousehold.IsAlive
            && h.HeadofHousehold.Class == SocialClass.Noble
            && h.HeadofHousehold.Lordships.Count() == 0
            && h.HeadofHousehold.House == Lords.Last().House
            && h.HeadofHousehold.GetHeirs().Count(heir => heir.House != Lords.Last().House) == 0
            ).ToList();
            return potentialSettlerLordHouseholds;
        }
        public List<Person> Defenders
        {
            get
            {
                //includes occupying armies
                //return Population.Where(
                //    m =>
                //    m.IsAlive
                //    && m.Class == SocialClass.Peasant
                //    && m.Age >= Constants.AGE_OF_MAJORITY
                //    && m.Age <= Constants.AGE_OF_RETIREMENT
                //    ).ToList();
                return EligibleForConscription.Union(OccupyingLordsAndArmies.SelectMany(l => l.Army.Where(s => s.IsAlive))).ToList();
            }
        }
        public bool Vacant { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public int FoundingYear { get; set; }
        public String Name { get; set; }
        public List<Household> Households { get; set; }
        public List<AssessedIncome> AssessedIncome { get; set; }
        public double Yeild { get; set; }
        public double Cost { get; set; }
        public double Surplus { get; set; }
        public double Wealth { get; set; }
        public void IncrementYear()
        {
            PlayerMoves.Clear();
            AttacksLedThisYear = 0;
            //lets fix this economy!
            //var currentHeirs = 
            if (!Vacant)
            {
                var subjects = Households.SelectMany(h => h.Members);//new List<Person>();
                //Households.ForEach(x => villagers.AddRange(x.Members));

                //4. Marry villagers! 
                var villagersInPrime = subjects.Where(x => x.IsAlive && x.Age >= 18 && x.Age < 50 && x.Household.HouseholdClass == SocialClass.Peasant).ToList();
                Person.CreateMarriages(villagersInPrime, _rnd);
                //5. Check for succession of Lord
                var incumbentLord = Lords.Last();
                if (!incumbentLord.IsAlive && !Vacant)
                {
                    //The lord is Dead!  Long live the lord!

                    Person heir = null;
                    var orderOfSuccession = GetOrderOfSuccession(1);
                    if (orderOfSuccession.Count > 0)
                    {
                        heir = orderOfSuccession[0];
                    }
                    if (heir != null)
                    {
                        AddLord(heir);
                        if (World.Player != null && World.Player.House != null && heir.House == World.Player.House && World.Player.House.Lords.Last().GetCurrentHeirs().Contains(heir))
                        {
                            Console.WriteLine("INHERITANCE: " + heir.FullNameAndAge + " INHERITED the Lordship of " + Name + " from " + incumbentLord.FullNameAndAge + " in " + World.Year);
                            if (heir.House != incumbentLord.House)
                            {
                                Console.WriteLine("CHANGE OF HOUSE! " + Name + " passed from House " + incumbentLord.House.Name + " to " + heir.House.Name + " in " + World.Year);
                            }
                        }
                    }
                    else
                    {
                        Vacant = true;
                        if (World.Player != null && incumbentLord.House == World.Player.House)
                        {
                            Console.WriteLine("VACANCY: The Lordship of " + Name + " is vacant.");
                        }
                    }
                }
                //6. Give jobs to unemployed villagers in prime
                var unemployedVillagersInPrime = villagersInPrime.Where(x => x.Profession == Profession.Dependant).ToList();
                foreach (var unemployedVillagerInPrime in unemployedVillagersInPrime)
                {
                    unemployedVillagerInPrime.Profession = Profession.Peasant;
                }
                //7. Calculate total cost
                Cost = subjects.Sum(x => x.Cost);
                //8. Calculate total yeild
                Yeild = subjects.Sum(x => x.Income);
                //+/- 20%
                var thisYearsMaxYeild = Math.Round(_maxYeild + (_maxYeild * (_rnd.NextDouble() * 0.2) * _rnd.Next(-1, 2)), 2);
                if (thisYearsMaxYeild < Yeild)
                {
                    Yeild = thisYearsMaxYeild;
                }
                //9. Add tax to income
                var income = Math.Round(Yeild * Constants.MIN_TAX_RATE, 2);
                //10. Calculate surplus/deficit
                Surplus = Yeild - Cost - income;
                //11. Add surplus to income if positive
                if (Surplus > 0) { income += Surplus; }
                //12. Add Income to treasury
                Wealth += income;
                //Add income to assessedIncome (for tax to house)
                AssessedIncome.Add(new AssessedIncome() { Year = World.Year, Income = income });
                //13. If there is a deficit then villagers die :(
                //if (Surplus < 0)
                //{
                //    var deficit = Surplus * -1;
                //    if (Wealth < 0)
                //    {
                //        deficit += Wealth * -1;
                //    }
                //    while (deficit > 0)
                //    {
                //        var peasantVillagers = villagers.Where(v => v.Class == SocialClass.Peasant).ToList();
                //        if (peasantVillagers.Count() > 0)
                //        {
                //            //kill a random villager
                //            var deadOne = peasantVillagers[_rnd.Next(0, peasantVillagers.Count())];

                //            deadOne.IsAlive = false;
                //            if (deadOne.Household != null)
                //            {
                //                deadOne.Household.RemoveMember(deadOne);
                //            }
                //            deficit -=
                //                deadOne.Cost;
                //        }
                //    }
                //}
                //Clear out any empty households
                Households.RemoveAll(x => x.Members.Count() == 0);
                //Retire old subjects and send them home
                subjects.Where(s => s.Age >= Constants.AGE_OF_RETIREMENT && s.Class == SocialClass.Peasant && s.Profession != Profession.Dependant).ToList().ForEach(retiree =>
                {
                    retiree.MoveToLocation(this);
                    retiree.Profession = Profession.Dependant;
                });
                //remove dead & old subjects from army
                Army.RemoveAll(soldier => !soldier.IsAlive || soldier.Age >= Constants.AGE_OF_RETIREMENT);
                // move lord and army to destination
                /*
                if (DeploymentRequest != null)
                {
                    if (DeploymentRequest.RequestingHouse == Lord.House || (Lord.House.Allegience != null && DeploymentRequest.RequestingHouse == Lord.House.Allegience))
                    {
                        if (!double.IsPositiveInfinity(GetShortestAvailableDistanceToLordship(DeploymentRequest.Destination, GetAllies())))
                        {
                            DischargeSoldiers(Army.Count());
                            ConscriptSoldiers(DeploymentRequest.NumberOfTroops);
                            DeploymentRequest.Destination.AddOccupyingLordAndArmy(this);
                            if (DeploymentRequest.RequestingHouse.Player.PlayerType == PlayerType.Live)
                            {
                                Console.WriteLine("ARRIVAL: " + Lord.FullNameAndAge + " HAS ARRIVED AT " + DeploymentRequest.Destination.Name + " with " + Army.Count() + " soldiers.");
                            }
                            DeploymentRequest = null;
                        }
                        else
                        {
                            if (DeploymentRequest.RequestingHouse.Player.PlayerType == PlayerType.Live)
                            {
                                Console.WriteLine("UNREACHABLE: " + Lord.FullNameAndAge + " CANNOT REACH " + DeploymentRequest.Destination.Name);
                            }
                        }
                    }
                }*/
            }
        }
        public void AddHousehold(Household newHousehold)
        {
            if (!Households.Contains(newHousehold))
            {
                Lordship oldLorship = null;
                if (newHousehold.Lordship != null)
                {
                    oldLorship = newHousehold.Lordship;
                    oldLorship.Removehousehold(newHousehold);
                }
                Households.Add(newHousehold);
                newHousehold.Lordship = this;
                //only move members that are currently in oldLorship (other may be deployed in army)
                newHousehold.Members.Where(m=>m.Location==oldLorship).ToList().ForEach(m => m.MoveToLocation(this));
            }
        }
        public void Removehousehold(Household oldHousehold)
        {
            if (Households.Contains(oldHousehold))
            {
                oldHousehold.Lordship = null;
                Households.Remove(oldHousehold);
            }
        }
        public void AddLord(Person newLord)
        {
            var oldLord = Lord;
            if (oldLord != null)
            {
                oldLord.Lordships.Remove(this);
            }
            if (!newLord.Lordships.Contains(this))
            {
                newLord.Lordships.Add(this);
            }
            Lords.Add(newLord);
            Vacant = false;
        }
        public List<Lordship> GetAdjacentLordships()
        {
            return World.Lordships.Where(v => v != this && (Math.Abs(v.MapX - MapX) == 1 || v.MapX == MapX) && (Math.Abs(v.MapY - MapY) == 1 || v.MapY == MapY)).ToList();
        }
        public List<Person> Lords { get; set; }
        public List<Person> GetOrderOfSuccession(int depth)
        {
            var successionList = new List<Person>();
            var lordIndex = Lords.Count - 1;
            while (successionList.Count() < depth && lordIndex >= 0)
            {
                var lordsHeirs = Lords[lordIndex].GetHeirs();
                foreach (var heir in lordsHeirs)
                {
                    if (!successionList.Contains(heir) && !Lords.Contains(heir))
                    {
                        successionList.Add(heir);
                    }
                }
                lordIndex--;
            }
            return successionList.Where(x => x.IsAlive).Take(depth).ToList();
        }
        public string GetDetailsAsString()
        {
            var retString = "";
            if (!Vacant)
            {
                //retString += World.GetMapAsString(null, this);
                retString += ("Lordship: " + Name + '\n');
                retString += ("Founding Year: " + FoundingYear + '\n');
                retString += ("Lord: " + Lord.FullNameAndAge + '\n');
                retString += ("Order of Succession:" + '\n');
                var orderOfSuccession = GetOrderOfSuccession(10);
                for (int i = 0; i < orderOfSuccession.Count(); i++)
                {
                    retString += ((i + 1) + ": " + orderOfSuccession[i].FullNameAndAge + '\n');
                }
                //var villagers = Households.SelectMany(h => h.Members);
                retString += ("Army: " + Army.Count(s => s.IsAlive) + '\n');
                retString += ("Occupying Armies: " + '\n');
                foreach (var occupyingArmy in OccupyingLordsAndArmies)
                {
                    retString += "\t" + occupyingArmy.Name + "(" + occupyingArmy.Army.Count(s => s.IsAlive) + ")\n";
                }
                retString += ("Eligible for Conscription: " + EligibleForConscription.Count() + '\n');
                retString += ("Location of Lord and Army: " + LocationOfLordAndArmy.Name + '\n');
                retString += ("Noble Households:" + Households.Count(v => v.HouseholdClass == SocialClass.Noble) + '\n');
                retString += ("Peasant Households:" + Households.Count(v => v.HouseholdClass == SocialClass.Peasant) + '\n');
                retString += ("Native Population:" + Households.SelectMany(h => h.Members).Count() + '\n');
                retString += ("Total Population:" + Population.Count() + '\n');
                retString += ("Defenders:" + Defenders.Count() + '\n');
                retString += ("Farmers: " + Farmers.Count() + '\n');
                retString += ("Yeild: " + Yeild + '\n');
                retString += ("Cost: " + Cost + '\n');
                retString += ("Surplus: " + Surplus + '\n');
                if (AssessedIncome.Count > 0)
                {
                    retString += ("Income: " + AssessedIncome.Last().Income.ToString("0.00") + '\n');
                }
                retString += ("Wealth: " + Wealth + '\n');
                retString += ("------------------------" + '\n');
            }
            return retString;
        }
        public static void PopulateLordship(Lordship newVillage, Household lordsHouseHold, List<Household> peasantHouseholds)
        {
            //add lord
            var newLord = lordsHouseHold.HeadofHousehold;
            newVillage.AddLord(newLord);
            var settlerHouseholds = new List<Household>();
            settlerHouseholds.Add(lordsHouseHold);
            settlerHouseholds.AddRange(peasantHouseholds);
            foreach (var settlerHousehold in settlerHouseholds)
            {
                newVillage.AddHousehold(settlerHousehold);
            }
            settlerHouseholds.ForEach(household => household.Members.ForEach(member => { newVillage.World.AddPerson(member); member.MoveToLocation(newVillage); }));

            if (newLord.World != null && newLord.World.Player != null && newLord.House == newLord.World.Player.House) //&& playerLords.Count > 0 && playerLords.Last().GetCurrentHeirs().Contains(newLord))
            {
                Console.WriteLine("EXPANSION: " + newLord.FullNameAndAge + " FOUNDED " + newVillage.Name + " in " + newVillage.World.Year);
            }
            newVillage.Vacant = false;
        }
        public double GetShortestAvailableDistanceToLordship(Lordship targetLordship, List<Lordship> lordshipsWithRightOfPassage)
        {
            //calculate shortest available distance between lorships based on variant of Dijkstra's Algorithm taking into account the 
            //allowed path

            //Pseudocode from https://en.wikipedia.org/wiki/Dijkstra's_algorithm#Pseudocode
            // 1  function Dijkstra(Graph, source):
            //-- Graph is allowed path, source is this
            // 2
            // 3      create vertex set Q
            var allowedPath = lordshipsWithRightOfPassage.ToList();
            if (!allowedPath.Contains(this))
            {
                allowedPath.Add(this);
            }
            if (!allowedPath.Contains(targetLordship))
            {
                allowedPath.Add(targetLordship);
            }
            var Q = new List<Lordship>();
            // 4
            var dist = new Dictionary<Lordship, double>();
            var prev = new Dictionary<Lordship, Lordship>();
            // 5      for each vertex v in Graph:             // Initialization
            foreach (var lordship in allowedPath)
            {
                // 6          dist[v] ← INFINITY                  // Unknown distance from source to v
                dist[lordship] = double.PositiveInfinity;
                // 7          prev[v] ← UNDEFINED                 // Previous node in optimal path from source
                prev[lordship] = null;
                // 8          add v to Q                          // All nodes initially in Q (unvisited nodes)
                Q.Add(lordship);
                // 9
            }
            //10      dist[source] ← 0                        // Distance from source to source
            dist[this] = 0;
            //11
            //12      while Q is not empty:
            while (Q.Count() > 0)
            {
                //13          u ← vertex in Q with min dist[u]    // Source node will be selected first
                var closestRemainingLordship = Q.OrderBy(lordship => dist[lordship]).First();
                //14          remove u from Q
                Q.Remove(closestRemainingLordship);
                //15
                //16          for each neighbor v of u:           // where v is still in Q.
                var allowedNeighbors = closestRemainingLordship.GetAdjacentLordships().Where(neighbor => Q.Contains(neighbor) && allowedPath.Contains(neighbor)).ToList();
                foreach (var v in allowedNeighbors)
                {
                    //17              alt ← dist[u] + length(u, v)
                    var alt = dist[closestRemainingLordship] + 1; //in our map neighbors are ALWAYS a distance of 1 away
                    //18              if alt < dist[v]:               // A shorter path to v has been found
                    if (alt < dist[v])
                    {
                        //19                  dist[v] ← alt
                        dist[v] = alt;
                        //20                  prev[v] ← u
                        prev[v] = closestRemainingLordship;
                        //21
                    }
                }
            }
            //22      return dist[], prev[]
            return dist[targetLordship];
        }
        public Lordship LocationOfLordAndArmy { get { return Lord.Location; } }
        public List<Lordship> OccupyingLordsAndArmies { get { return World.Lordships.Where(ls => !ls.Vacant && ls.Lord.Location == this).ToList(); } }
        public void AddOccupyingLordAndArmy(Lordship occupyingLordAndArmy)
        {
            if (!OccupyingLordsAndArmies.Contains(occupyingLordAndArmy))
            {
                occupyingLordAndArmy.Lord.MoveToLocation(this);
                occupyingLordAndArmy.Army.ForEach(soldier => soldier.MoveToLocation(this));
            }
        }
        public List<Lordship> GetAllies()
        {
            //return vassles' and lord's lordships
            var allies = new List<Lordship>();
            if (!Vacant)
            {
                allies.AddRange(Lord.House.Lordships.Where(ally => ally != this));
                if (Lord.House.Allegience != null)
                {
                    allies.AddRange(Lord.House.Allegience.Lordships);
                    if (Lord.House.Allegience.Vassles.Count() > 0)
                    {
                        allies = allies.Union(Lord.House.Allegience.Vassles.SelectMany(h => h.Lordships)).ToList();
                    }
                }
                if (Lord.House.Vassles.Count() > 0)
                {
                    allies.AddRange(
                        Lord.House.Vassles.SelectMany(h => h.Lordships)
                    );
                }
            }
            return allies;
        }
        public List<Lordship> GetAttackableLordships()
        {
            var subjectLordship = this;
            var allies = subjectLordship.GetAllies();
            var attackableLordships =
                subjectLordship.World.Lordships
                .Where(l => !allies.Contains(l) && l!=subjectLordship && !double.IsPositiveInfinity(subjectLordship.LocationOfLordAndArmy.GetShortestAvailableDistanceToLordship(l, allies)))
                .ToList();
            return attackableLordships;
        }
        public void Attack(Lordship target, Random rnd, bool getLiveInput = true, double acceptFealtyRatio = 0.5, double retreatRatio = 1, bool echo = true)
        {

            //Gather initial armies
            var attackingLordship = this;
            if (double.IsPositiveInfinity(attackingLordship.LocationOfLordAndArmy.GetShortestAvailableDistanceToLordship(target, attackingLordship.GetAllies())))
            {
                if (echo) { Console.WriteLine(attackingLordship.Lords.Last().FullNameAndAge + " cannot reach " + target.Name); }
            }
            else
            {
                var livingDefenders = target.Defenders.ToList();
                var livingAttackers = attackingLordship.Army.ToList();
                //build army of vassles -- army will only fight if they are attackers subvassle
                livingAttackers.AddRange(OccupyingLordsAndArmies.Where(lordship => Lord.House.Vassles.Contains(lordship.Lord.House)).SelectMany(lordship => lordship.Army));

                //set booleans for commands
                var offerFealty = false;
                var acceptFealty = false;
                var retreat = false;
                var endBattle = false;
                var timeSinceStartOfAttack = 0;
                var attackerArrived = false;
                //var attack = false;
                do
                {

                    timeSinceStartOfAttack++;
                    //--check and see if attacker has arrived
                    if (timeSinceStartOfAttack < attackingLordship.LocationOfLordAndArmy.GetShortestAvailableDistanceToLordship(target, attackingLordship.GetAllies()))
                    {
                        if (echo) { Console.WriteLine(attackingLordship.Lords.Last().FullNameAndAge + " is on the march!"); }
                    }
                    else
                    {
                        attackerArrived = true;
                        if (echo) { Console.WriteLine(attackingLordship.Lords.Last().FullNameAndAge + " has arrived at " + target.Name + " with an army of " + livingAttackers.Count()); }
                    }

                    //--check for defender reinforcements
                    var defendersAllies = target.GetAllies().ToList();
                    var unarrivedDefendersAllies = defendersAllies.ToList();
                    for (var i = 0; i < unarrivedDefendersAllies.Count; i++)
                    {
                        var unarrivedAlly = unarrivedDefendersAllies[i];
                        if (target.GetShortestAvailableDistanceToLordship(unarrivedAlly, defendersAllies) <= timeSinceStartOfAttack)
                        {
                            unarrivedDefendersAllies.Remove(unarrivedAlly);
                            var reinforcements = unarrivedAlly.Defenders.ToList();
                            if (echo) { Console.WriteLine(unarrivedAlly.Lords.Last().FullNameAndAge + " arrived with " + reinforcements.Count() + " reinforcements!"); }
                            livingDefenders.AddRange(unarrivedAlly.Defenders);
                        }
                    }
                    //--defender may surrender and offer fealty
                    if (livingAttackers.Count() > livingDefenders.Count() * Constants.SURRENDER_RATIO && !target.Vacant && target.Lord.House.Lord.Location == target)
                    {
                        //SURRENDER 
                        if (echo) { Console.WriteLine("OFFER OF FEALTY: " + target.Lord.FullNameAndAge + "sues for peace and OFFERS FEALTY to " + attackingLordship.Lord.FullNameAndAge); }
                        offerFealty = true;
                    }
                    //--attacker chooses to continue attack or not
                    var validInput = false;
                    while (!validInput && getLiveInput)
                    {
                        Console.WriteLine("Remaining Attackers: " + livingAttackers.Count());
                        Console.WriteLine("Remaining Defenders: " + livingDefenders.Count());
                        Console.WriteLine("[R]etreat?\n[A]ttack?");
                        if (offerFealty)
                        {
                            Console.WriteLine("Accept [F]ealty?");
                        }
                        Console.WriteLine("[R]etreat?\n[A]ttack?");
                        var command = Console.ReadLine();
                        switch (command.ToUpper().Trim())
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
                        }
                    }
                    if (!getLiveInput)
                    {
                        var defenderToAttackerRatio = (double)livingDefenders.Count() / livingAttackers.Count();
                        if (offerFealty && defenderToAttackerRatio > acceptFealtyRatio)
                        {
                            acceptFealty = true;
                        }
                        else if (defenderToAttackerRatio > retreatRatio)
                        {
                            retreat = true;
                        }
                    }
                    //--if retreat then battle is over
                    if (retreat)
                    {
                        endBattle = true;
                        if (echo) { Console.WriteLine("RETREAT: " + attackingLordship.Lord.FullNameAndAge + " RETREATED from battle."); }
                    }
                    //--if fealty is accepted battle is over
                    if (acceptFealty)
                    {
                        endBattle = true;
                        attackingLordship.Lord.House.AddVassle(target.Lord.House);
                        if (echo) { Console.WriteLine("FEALTY: " + attackingLordship.Lord.House.Lord.FullNameAndAge + " accepted FEALTRY from " + target.Lord.House.Lord.FullNameAndAge); }
                    }
                    if (!endBattle)
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
                        if (echo)
                        {
                            Console.WriteLine(string.Format("ATTACK WAVE {0} RESULTS:", timeSinceStartOfAttack));
                            Console.WriteLine("\tAttackers in Wave: " + countOfAttackersInWave);
                            Console.WriteLine("\tDefender in Wave: " + countOfDefendersInWave);
                            Console.WriteLine("\tAttacker Casulties in Wave: " + attackerCasultiesInWave);
                            Console.WriteLine("\tDefender Casulties in Wave: " + defenderCasultiesInWave);
                        }
                    }
                } while (livingDefenders.Count() > 0 && livingAttackers.Count() > 0 && !endBattle);
                if (livingAttackers.Count() == 0)
                {
                    //DEFEAT
                    endBattle = true;
                    if (echo) { Console.WriteLine("DEFEAT: " + attackingLordship.Lord.FullNameAndAge + " WAS DEFEATED by " + target.Lord.FullNameAndAge); }
                }
                if (livingDefenders.Count() == 0)
                {
                    //--if attacker wins they become Lord of lordship and take all nobles hostage
                    endBattle = true;
                    var oldLord = target.Lord;
                    var newLord = attackingLordship.Lord;
                    if (echo) {
                        Console.Write("CONQUEST: " + attackingLordship.Lord.FullNameAndAge + " HAS CONQUORED " + target.Name + "\n");
                    }
                    if (oldLord != null)
                    {
                        oldLord.Lordships.Remove(target);
                    }
                    target.Lords.Clear();
                    target.Vacant = true;
                    target.AddLord(newLord);
                }
            }
        }
        public List<Lordship> GetVisibleLordships()
        {
            var visibleLorships = new List<Lordship>();
            var allies = GetAllies();
            var neighbors = allies.SelectMany(v => v.GetAdjacentLordships()).Union(GetAdjacentLordships()).Distinct().Where(v => !allies.Contains(v)).ToList();
            visibleLorships.Add(this);
            visibleLorships = visibleLorships.Union(allies).Union(neighbors).ToList();
            return visibleLorships;
        }
        public string GetMapOfKnownWorld(Lordship lordship = null)
        {
            //output map
            //List<Lordship> subVassles = null;
            if (lordship == null)
            {
                lordship = this;
            }
            var allies = lordship.GetAllies();
            var neighbors = allies.SelectMany(v => v.GetAdjacentLordships()).Union(GetAdjacentLordships()).Distinct().Where(v => !allies.Contains(v)).ToList();

            var map = "    |";
            for (var x = 1; x <= Constants.MAP_WIDTH; x++)
            {
                map += x.ToString("0000") + "|";
            }
            map += "\n";
            for (var y = 1; y <= Constants.MAP_HEIGHT; y++)
            {
                map += y.ToString("0000");
                for (var x = 1; x <= Constants.MAP_WIDTH; x++)
                {
                    map += "|";
                    var lordshipOnMap = World.Lordships.First(v => v.MapX == x && v.MapY == y);
                    var mapsymbol = "?";
                    if (lordshipOnMap == this)
                    {
                        mapsymbol = "U";
                    }
                    else if (!lordshipOnMap.Vacant && !lordship.Vacant && lordshipOnMap.Lord.House == lordship.Lord.House)
                    {
                        mapsymbol = lordship.Lord.House.Symbol.ToString();
                    }
                    else if (allies.Contains(lordshipOnMap))
                    {
                        mapsymbol = "+";
                    }
                    else if (neighbors.Contains(lordshipOnMap))
                    {
                        mapsymbol = "-";
                    }

                    //var vassles = lordshipOnMap
                    if (mapsymbol != "?")
                    {
                        map += mapsymbol + lordshipOnMap.Defenders.Count.ToString("000");
                    }
                    else
                    {
                        map += " ?? ";
                    }
                }
                map += "|" + y.ToString("0000") + "\n";
            }
            map += "    |";
            for (var x = 1; x <= Constants.MAP_WIDTH; x++)
            {
                map += x.ToString("0000") + "|";
            }
            map += "\n";
            return map;
        }
        //public Lordship DestinationOfLordshipAndArmy { get; set; }
        //public DeploymentRequest DeploymentRequest { get; set; }
        public int AttacksLedThisYear { get; set; }
    }
}
