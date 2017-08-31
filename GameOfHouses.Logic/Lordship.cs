using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public class Lordship
    {
        private Random _rnd;
        private int _maxYeild;
        private List<Lordship> _adjacentLordships;
        public Lordship(Random rnd)
        {
            _rnd = rnd;
            Id = Guid.NewGuid();
            Households = new List<Household>();
            Lords = new List<Person>();
            Wealth = 0;
            Surplus = 0;
            _maxYeild = _rnd.Next(Constants.MAX_YEILD_LOW, Constants.MAX_YEILD_HIGH);
            AssessedIncomes = new List<AssessedIncome>();
            //Vassles = new List<Lordship>();
            Vacant = true;
            PlayerMoves = new List<Player>();
            //Army = new List<Person>();
            //Population = new List<Person>();
        }
        public Guid Id { get; set; }
        public Person Lord
        {
            get
            {
                if(Lords.Count()>0 && Lords.Last().IsAlive)
                {
                    return Lords.Last();
                } else
                {
                    return null;
                }
            }
        }
        public List<Person> Population { get { return Households.SelectMany(h => h.Members).ToList(); } }
        public World World { get; set; }
        public List<Person> Army {
            get {
                return Households.SelectMany(h=>
                    h.Members.Where(p=>
                    p.IsAlive
                    //&& p.Sex == Sex.Male
                    && p.Class == SocialClass.Peasant
                    && p.Age >= Constants.AGE_OF_MAJORITY
                    && p.Age < Constants.AGE_OF_RETIREMENT
                    )
                ).ToList();
            }
        }
        public List<Player> PlayerMoves { get; set; }
        public List<Person> Defenders
        {
            get
            {
                var allies = GetAllies();
                return Army.Union(GetAdjacentLordships().Where(l => /*l.LordIsInResidence &&*/ allies.Contains(l)).SelectMany(l => l.Army)).ToList();
                //OccupyingLordsAndArmies.SelectMany(l => l.Army.Where(s => s.IsAlive)).ToList();
                //return EligibleForConscription.Union(OccupyingLordsAndArmies.SelectMany(l => l.Army.Where(s => s.IsAlive))).ToList();
            }
        }
        public bool Vacant { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public int FoundingYear { get; set; }
        public String Name { get; set; }
        public List<Household> Households { get; set; }
        public List<AssessedIncome> AssessedIncomes { get; set; }
        public double Yeild { get; set; }
        public double Cost { get; set; }
        public double Surplus { get; set; }
        public double Wealth { get; set; }
        public void IncrementYear()
        {
            PlayerMoves.Clear();
            //AttacksLedThisYear = 0;
            //lets fix this economy!
            if (!Vacant)
            {
                var subjects = Households.SelectMany(h => h.Members);
 
                //4. Marry villagers! 
                var villagersInPrime = subjects.Where(x => x.IsAlive && x.Age >= Constants.AGE_OF_MAJORITY && x.Age < Constants.AGE_OF_RETIREMENT && x.Household.HouseholdClass == SocialClass.Peasant).ToList();
                Person.CreateMarriages(villagersInPrime.Where(x=>x.IsEligibleForMarriage()).ToList(), _rnd);
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
                    if (heir == null && incumbentLord.House.Lord != null)
                    {
                        heir = incumbentLord.House.Lord;
                    }
                    if (heir != null)
                    {
                        AddLord(heir);
                        var record = "INHERITANCE: " + heir.FullNameAndAge + " INHERITED the Lordship of " + Name + " from " + incumbentLord.FullNameAndAge + " in " + World.Year + "\n";
                        heir.House.RecordHistory(record);
                        if (heir.House != incumbentLord.House)
                        {
                           incumbentLord.House.RecordHistory(record);
                            record = ("CHANGE OF HOUSE! " + Name + " passed from House " + incumbentLord.House.Name + " to " + heir.House.Name + " in " + World.Year + "\n");
                            heir.House.RecordHistory(record);
                            incumbentLord.House.RecordHistory(record);
                        }
                    }
                    else
                    {
                        Vacant = true;
                        incumbentLord.House.RecordHistory("VACANCY: The Lordship of " + Name + " is vacant.\n");
                    }
                }
                //6. Give jobs to unemployed villagers in prime
                //var unemployedVillagersInPrime = villagersInPrime.Where(x => x.Profession == Profession.Dependant).ToList();
                //foreach (var unemployedVillagerInPrime in unemployedVillagersInPrime)
                //{
                //    unemployedVillagerInPrime.Profession = Profession.Peasant;
                //}
                //7. Calculate total cost
                Cost = subjects.Sum(x => x.Cost);
                //8. Calculate total yeild
                Yeild = subjects.Sum(x => x.Income);
                //+/- 20%
                _maxYeild = World.Population.Count() / World.Lordships.Count() * 2;
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
                if (Lord != null)
                {
                    //Wealth += income;
                    Lord.House.Wealth += income;
                }
                //Add income to assessedIncome (for tax to house)
                //AssessedIncomes.Add(new AssessedIncome() { Year = World.Year, Income = income });
                //13. If there is a deficit then villagers die :(
                if (Surplus < 0)
                {
                    //make house treasury available
                    var deficit = Surplus * -1;
                    if (Lord != null)
                    {
                        var housefunds = Lord.House.Wealth;
                        if (deficit >= housefunds)
                        {
                            deficit -= housefunds;
                            Lord.House.Wealth = 0;
                        } else
                        {
                            Lord.House.Wealth -= deficit;
                            deficit = 0;
                        }                       
                    }

                    
                    
                    //if (Wealth < 0)
                    //{
                    //    deficit += Wealth * -1;
                    //}
                    var peasantVillagers = subjects.Where(v => v.Class == SocialClass.Peasant).ToList();
                    while (deficit > 0 && peasantVillagers.Count() > 0)
                    {
                            //kill a random villager
                            var deadOne = peasantVillagers[_rnd.Next(0, peasantVillagers.Count())];

                            deadOne.Kill();

                            deficit -=
                                deadOne.Cost;
                    }
                }
                //Clear out any empty households
                Households.RemoveAll(x => x.Members.Count() == 0);
            }
            //increase max yeild by 2% every year
            _maxYeild = (int)Math.Ceiling(_maxYeild * 1.02);
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
                //newHousehold.Members.Where(m=>m.Location==oldLorship).ToList().ForEach(m => m.MoveToLocation(this));
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
            if (_adjacentLordships == null)
            {
                _adjacentLordships = World.Lordships.Where(v => v != this && (Math.Abs(v.MapX - MapX) == 1 || v.MapX == MapX) && (Math.Abs(v.MapY - MapY) == 1 || v.MapY == MapY)).ToList();
            }
            return _adjacentLordships;
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
                retString += string.Format("Lordship: {0} ({1},{2})\n", Name, MapX, MapY);
                //retString += ("Founding Year: " + FoundingYear + '\n');
                retString += ("Lord: " + Lord.FullNameAndAge + '\n');
                retString += ("Order of Succession:" + '\n');
                var orderOfSuccession = GetOrderOfSuccession(3);
                for (int i = 0; i < orderOfSuccession.Count(); i++)
                {
                    retString += ((i + 1) + ": " + orderOfSuccession[i].FullNameAndAge + '\n');
                }
                //var villagers = Households.SelectMany(h => h.Members);
                retString += ("Fighters: " + Army.Count(s => s.IsAlive) + '\n');
                retString += ("1st Wave Defenders:" + Defenders.Count() + '\n');
                retString += ("Population:" + Population.Count() + '\n');
                retString += ("Noble Households:" + Households.Count(v => v.HouseholdClass == SocialClass.Noble) + '\n');
                retString += ("Peasant Households:" + Households.Count(v => v.HouseholdClass == SocialClass.Peasant) + '\n');
                //retString += ("Occupying Armies: " + '\n');
                //foreach (var occupyingArmy in OccupyingLordsAndArmies)
                //{
                //    retString += "\t" + occupyingArmy.Name + "(" + occupyingArmy.Army.Count(s => s.IsAlive) + ")\n";
                //}
                //retString += ("Eligible for Conscription: " + EligibleForConscription.Count() + '\n');
                //retString += ("Location of Lord and Army: " + LocationOfLordAndArmy.Name + '\n');
                //retString += ("Native Population:" + Households.SelectMany(h => h.Members).Count() + '\n');
                //retString += ("Farmers: " + Farmers.Count() + '\n');
                retString += ("Yeild: " + Yeild + '\n');
                retString += ("Cost: " + Cost + '\n');
                retString += ("Surplus: " + Surplus + '\n');
                if (AssessedIncomes.Count > 0)
                {
                    retString += ("Income: " + AssessedIncomes.Last().Income.ToString("0.00") + '\n');
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
            settlerHouseholds.ForEach(household => household.Members.ForEach(member => { newVillage.World.AddPerson(member);}));

            newVillage.Vacant = false;
        }
        public Dictionary<Lordship,double> GetShortestAvailableDistanceToLordship(List<Lordship> lordshipsWithRightOfPassage)
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
            //if (!allowedPath.Contains(targetLordship))
            //{
            //    allowedPath.Add(targetLordship);
            //}
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
            return dist;//[targetLordship];
        }
        public List<Lordship> OccupyingLordsAndArmies { get { return World.Lordships.Where(ls => ls.Lord !=null && ls.Lord.Location == this).ToList(); } }
        public List<Lordship> GetAllies()
        {
            //return everyone with the same sovreign
            var allies = new List<Lordship>();
            if (Lord!=null)
            {
                var sovreign = Lord.House.GetSovreign();
                allies = World.Lordships.Where(l => l!=this && l.Lord!=null && l.Lord.House.GetSovreign() == sovreign).ToList();
                //allies = sovreign.Lordships.Union(sovreign.GetAllSubVassles().SelectMany(v=>v.Lordships)).Where(ally=>ally!=this).ToList();

                //allies.AddRange(Lord.House.Lordships.Where(ally => ally != this));
                //if (Lord.House.Allegience != null)
                //{
                //    allies.AddRange(Lord.House.Allegience.Lordships);
                //    if (Lord.House.Allegience.Vassles.Count() > 0)
                //    {
                //        allies = allies.Union(Lord.House.Allegience.Vassles.SelectMany(h => h.Lordships)).ToList();
                //    }
                //}
                //if (Lord.House.Vassles.Count() > 0)
                //{
                //    allies.AddRange(
                //        Lord.House.Vassles.SelectMany(h => h.Lordships)
                //    );
                //}

            }
            return allies;
        }
        public List<Lordship> GetAttackableLordships()
        {
            //var subjectLordship = this;
            //var allies = subjectLordship.GetAllies();
            //var attackableLordships =
            //    subjectLordship.World.Lordships
            //    .Where(l => !allies.Contains(l) && l!=subjectLordship && !double.IsPositiveInfinity(subjectLordship.LocationOfLordAndArmy.GetShortestAvailableDistanceToLordship(l, allies)))
            //    .ToList();
            //return attackableLordships;
            var allies = GetAllies();
            return GetVisibleLordships().Where(l => l!=this && !allies.Contains(l)).ToList();
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
                    else if (lordshipOnMap.Lord!=null && lordship.Lord!=null && lordshipOnMap.Lord.House == lordship.Lord.House)
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
        //public int AttacksLedThisYear { get; set; }
        public Lordship Flatten()
        {
            return new Lordship(new Random())
            {
                Id = Id,
                AssessedIncomes = AssessedIncomes.Select(a => a.Flatten()).ToList(),
                //AttacksLedThisYear = AttacksLedThisYear,
                Cost = Cost,
                FoundingYear = FoundingYear,
                Households = Households.Select(h => h.Flatten()).ToList(),
                Lords = Lords.Select(l => new Person(new Random()) { Id = l.Id }).ToList(),
                MapX = MapX,
                MapY = MapY,
                Surplus = Surplus,
                Vacant = Vacant,
                Wealth = Wealth,
                World = World,
                Yeild = Yeild
            };
        }
    }
}
