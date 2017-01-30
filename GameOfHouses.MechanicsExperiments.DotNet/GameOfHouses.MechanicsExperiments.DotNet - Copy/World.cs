using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public class World
    {
        private Random _rnd;
        public World(Random rnd)
        {
            Year = 0;
            Lordships = new List<Lordship>();
            Population = new List<Person>();
            NobleHouses = new List<House>();
            Bethrothals = new List<Bethrothal>();
            Player = null;
            _rnd = rnd;
        }
        public Player Player { get; set; }
        public int Year { get; set; }
        public List<Lordship> Lordships { get; set; }
        public List<Person> Population { get; set; }
        public List<House> NobleHouses { get; set; }
        public void AddLordship(Lordship lordship)
        {
            if (!Lordships.Contains(lordship))
            {
                if (lordship.World != null)
                {
                    lordship.World.RemoveLordship(lordship);
                }
                Lordships.Add(lordship);
                lordship.World = this;
            }
        }
        public void RemoveLordship(Lordship lordship)
        {
            if (Lordships.Contains(lordship))
            {
                Lordships.Remove(lordship);
                lordship.World = null;
            }
        }
        public void AddHouse(House house)
        {
            if (!NobleHouses.Contains(house))
            {
                if (house.World != null)
                {
                    house.World.RemoveHouse(house);
                }
                NobleHouses.Add(house);
                house.World = this;
            }
        }
        public void RemoveHouse(House house)
        {
            if (NobleHouses.Contains(house))
            {
                NobleHouses.Remove(house);
                house.World = null;
            }
        }
        public void AddPerson(Person person)
        {
            if (person.World != null)
            {
                person.World.RemovePerson(person);
            }
            Population.Add(person);
            person.World = this;
        }
        public void RemovePerson(Person person)
        {
            Population.Remove(person);
            person.World = null;
        }
        public void IncrementYear()
        {
            Year++;
            var population = new List<Person>();
            var nobleHouses = new List<House>();
            var lordships = new List<Lordship>();

            population.AddRange(Population);
            nobleHouses.AddRange(NobleHouses);
            lordships.AddRange(Lordships);
            population.ForEach(person => person.IncrementYear());
            nobleHouses.ForEach(house => house.IncrementYear(_rnd));
            lordships.ForEach(lordship => lordship.IncrementYear());
        }
        public string GetMapAsString(House house = null, Lordship lordship = null, int? people = null)
        {
            //output map
            List<Lordship> subVasslesLordships = null;
            if (lordship != null)
            {
                subVasslesLordships = lordship.Lord.House.GetAllSubVassles().SelectMany(v => v.Lordships).ToList();
            }
            var map = "   |";
            for (var x = 1; x <= Constants.MAP_WIDTH; x++)
            {
                map += x.ToString("000") + "|";
            }
            map += "\n";
            for (var y = 1; y <= Constants.MAP_HEIGHT; y++)
            {
                map += y.ToString("000");
                for (var x = 1; x <= Constants.MAP_WIDTH; x++)
                {
                    map += "|";
                    var lordshipOnMap = Lordships.First(v => v.MapX == x && v.MapY == y);
                    //var vassles = lordshipOnMap
                    if (!lordshipOnMap.Vacant
                        && (house == null || lordshipOnMap.Lords.Last().House == house)
                        && (lordship == null || (lordshipOnMap == lordship || (subVasslesLordships != null && subVasslesLordships.Contains(lordshipOnMap))))
                        && (people == null || (int)lordshipOnMap.Lords.Last().People == people.Value))
                    {
                        if (house != null || (subVasslesLordships != null && subVasslesLordships.Contains(lordshipOnMap)))
                        {
                            map += lordshipOnMap.Defenders.Count.ToString("000");
                        }
                        else
                        {
                            map += "  " + lordshipOnMap.Lords.Last().House.Symbol;
                        }
                    }
                    else
                    {
                        map += "   ";
                    }
                }
                map += "|" + y.ToString("000") + "\n";
            }
            map += "   |";
            for (var x = 1; x <= Constants.MAP_WIDTH; x++)
            {
                map += x.ToString("000") + "|";
            }
            map += "\n";
            return map;
        }
        public string GetDetailsAsString()
        {
            var retString = "";
            var world = this;
            retString += world.GetMapAsString();
            retString += ("Dane Noble Houses: " + world.NobleHouses.Count(x => !x.Vacant && x.Lords.Last().People == People.Dane) + "\n");
            retString += ("Saxon Noble Houses: " + world.NobleHouses.Count(x => !x.Vacant && x.Lords.Last().People == People.Saxon) + "\n");
            retString += ("Lordships: " + world.Lordships.Count(x => x.Households.Count() > 0)) + "\n";
            retString += ("Total Population: " + world.Population.Count(x => x.IsAlive)) + "\n";
            retString += ("Total Saxon Population: " + world.Population.Count(x => x.IsAlive && x.People == People.Saxon)) + "\n";
            retString += ("Total Dane Population: " + world.Population.Count(x => x.IsAlive && x.People == People.Dane)) + "\n";
            retString += ("Total Noble Households: " + world.Lordships.Sum(x => x.Households.Count(y => y.HouseholdClass == SocialClass.Noble))) + "\n";
            retString += ("Total Peasant Households: " + world.Lordships.Sum(x => x.Households.Count(y => y.HouseholdClass == SocialClass.Peasant))) + "\n";
            retString += ("------------------------") + "\n";
            return retString;
        }
        public List<Bethrothal> Bethrothals
        {
            get; set;
        }
        public Bethrothal CreateBethrothal(Person headOfHouseoldToBe, Person spouseToBe, int year, bool echo = false)
        {
            //remove any existing bethrothals 
            Bethrothals.Where(b => b.HeadOfHouseholdToBe == headOfHouseoldToBe || b.SpouseToBe == spouseToBe).ToList()
                .ForEach(b => CancelBetrothal(b));
            var bethrothal = new Bethrothal()
            {
                HeadOfHouseholdToBe = headOfHouseoldToBe,
                SpouseToBe = spouseToBe,
                Year = year,
                world = this
            };
            headOfHouseoldToBe.Bethrothal = bethrothal;
            spouseToBe.Bethrothal = bethrothal;
            Bethrothals.RemoveAll(b => b.HeadOfHouseholdToBe == headOfHouseoldToBe || b.SpouseToBe == spouseToBe);
            Bethrothals.Add(bethrothal);

            if (echo && headOfHouseoldToBe.House == headOfHouseoldToBe.World.Player.House && headOfHouseoldToBe.World.Player.House.Lords.Last().GetCurrentHeirs().Contains(headOfHouseoldToBe))
            {
                Console.WriteLine("BETROTHAL: " + bethrothal.HeadOfHouseholdToBe.FullNameAndAge + " was BETROTHED to " + bethrothal.SpouseToBe.FullNameAndAge + " in " + year);
            }
            return bethrothal;
        }
        public void CancelBetrothal(Bethrothal bethrothal)
        {
            bethrothal.HeadOfHouseholdToBe.Bethrothal = null;
            bethrothal.SpouseToBe.Bethrothal = null;
            bethrothal.HeadOfHouseholdToBe = null;
            bethrothal.SpouseToBe = null;
            Bethrothals.Remove(bethrothal);
        }
    }

}
