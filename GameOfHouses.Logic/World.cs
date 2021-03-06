﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
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
            EligibleNobles = new List<Person>();
            Proposals = new List<Proposal>();
            _rnd = rnd;
        }
        public Guid Id { get; set; }
        public Game Game { get; set; }
        public List<Proposal> Proposals { get; set; }
        public int Year { get; set; }
        public List<Lordship> Lordships { get; set; }
        public List<Person> Population { get; set; }
        public List<House> NobleHouses { get; set; }
        public List<Bethrothal> Bethrothals
        {
            get; set;
        }
        public List<Person> EligibleNobles { get; set; }
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
            var livingPopulation = Population.Where(p => p.IsAlive).ToList();
            var nobleHouses = NobleHouses.ToList();
            var lordships = Lordships.ToList();
            EligibleNobles.RemoveAll(p => !p.IsEligableForBetrothal());
            Proposals.RemoveAll(p => p.Status == ProposalStatus.ResponseReceived);
            livingPopulation.ForEach(person => person.IncrementYear());
            nobleHouses.ForEach(house => house.IncrementYear(_rnd));
            lordships.ForEach(lordship => lordship.IncrementYear());
            Person.CreateMarriages(EligibleNobles.Where(n => !Proposals.SelectMany(p => p.RequestedPeople).Contains(n)).ToList(), _rnd);
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
                        else if (lordshipOnMap.Lord!= null && !lordshipOnMap.Lord.House.Vacant && lordshipOnMap.Lord.House.GetSovreign().Lord!=null && lordshipOnMap.Lord.House.GetSovreign().Lord.Household != null && lordshipOnMap == lordshipOnMap.Lord.House.GetSovreign().Lord.Household.Lordship)
                        {
                            map += "*" + lordshipOnMap.Lords.Last().House.GetSovreign().Symbol + "*";
                        } else
                        {
                            map += " " + lordshipOnMap.Lords.Last().House.GetSovreign().Symbol + " ";
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
            //retString += world.GetMapAsString();
            //retString += ("Dane Noble Houses: " + world.NobleHouses.Count(x => !x.Vacant && x.Lords.Last().People == People.Dane) + "\n");
            //retString += ("Saxon Noble Houses: " + world.NobleHouses.Count(x => !x.Vacant && x.Lords.Last().People == People.Saxon) + "\n");
            retString += "Sovreigns: " + world.NobleHouses.Where(h=>h.Lordships.Count()>0).Select(h => h.GetSovreign()).Distinct().Count() + "\n";
            retString += ("Total Population: " + world.Population.Count(x => x.IsAlive)) + "\n";
            //retString += ("Total Saxon Population: " + world.Population.Count(x => x.IsAlive && x.People == People.Saxon)) + "\n";
            //retString += ("Total Dane Population: " + world.Population.Count(x => x.IsAlive && x.People == People.Dane)) + "\n";
            //retString += ("Total Noble Households: " + world.Lordships.Sum(x => x.Households.Count(y => y.HouseholdClass == SocialClass.Noble))) + "\n";
            //retString += ("Total Peasant Households: " + world.Lordships.Sum(x => x.Households.Count(y => y.HouseholdClass == SocialClass.Peasant))) + "\n";
            //retString += ("------------------------") + "\n";
            return retString;
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

            //if (echo && headOfHouseoldToBe.House == headOfHouseoldToBe.World.Player.House && headOfHouseoldToBe.World.Player.House.Lords.Last().GetCurrentHeirs().Contains(headOfHouseoldToBe))
            //{

            var news = "BETROTHAL: " + bethrothal.HeadOfHouseholdToBe.FullNameAndAge + " was BETROTHED to " + bethrothal.SpouseToBe.FullNameAndAge + " in " + year +"\n";
            if (headOfHouseoldToBe.House.Player != null && (headOfHouseoldToBe.IsHouseHeir() || headOfHouseoldToBe.IsHouseLord()))
            {
                var world = headOfHouseoldToBe.House.World;
                headOfHouseoldToBe.House.RecordHistory(news);
            }
            if (spouseToBe.House.Player != null && (spouseToBe.IsHouseHeir() || spouseToBe.IsHouseLord()))
            {
                var world = spouseToBe.House.World;
                spouseToBe.House.RecordHistory(news);
            }
            if (echo)
            {
                Console.Write(news);
            }
            //}
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
        public World Flatten()
        {
            return new World(_rnd)
            {
                Id = this.Id,
                Bethrothals = Bethrothals.Select(b => b.Flatten()).ToList(),
                EligibleNobles = EligibleNobles.Select(p => new Person(_rnd) { Id = p.Id }).ToList(),
                Game = new Game {Id = Game.Id},
                Lordships = Lordships.Select(l => l.Flatten()).ToList(),
                NobleHouses = NobleHouses.Select(h => h.Flatten()).ToList(),
                Population = Population.Select(p => p.Flatten()).ToList(),
                Proposals = Proposals.Select(p => p.Flatten()).ToList(),
                Year = Year
            };
        }
    }

}
