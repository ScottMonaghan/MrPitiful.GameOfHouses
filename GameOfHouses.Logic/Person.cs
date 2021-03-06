﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public class Person
    {
        private Random _rnd;
        private string _cachedFullNameAndAge;
        private int _lastCachedFullNameAndAgeRefesh;
        private House _house;
        private Household _household;
        private string _name;

        public Person(Random rnd)
        {
            _lastCachedFullNameAndAgeRefesh = -999;
            _rnd = rnd;
            Id = Guid.NewGuid();
            Household = new Household();
            BirthPlace = null;
            Sex = (Sex)rnd.Next(0, 2);
            Name = GenerateName(_rnd, Sex);
            Age = 0;
            IsAlive = true;
            Children = new List<Person>();
            Spouse = null;
            if (Age >= Constants.AGE_OF_MAJORITY && Age < Constants.AGE_OF_RETIREMENT)
            {
                Profession = Profession.Peasant;
            }
            else
            {
                Profession = Profession.Dependant;
            }
            Lordships = new List<Lordship>();
            HouseLordships = new List<House>();
            Ancestors = new List<Person>();
        }
        public Guid Id { get; set; }
        public People People { get; set; }
        public World World { get; set; }
        public SocialClass Class { get; set; }
        public List<Person> Ancestors { get; set; }
        public House House { get { return _house; } set { _lastCachedFullNameAndAgeRefesh = -1; _house = value; } }
        public Household Household { get { return _household; } set { _lastCachedFullNameAndAgeRefesh = -1; _household = value; } }
        public int BirthYear { get; set; }
        public Lordship BirthPlace { get; set; }
        public string Name { get { return _name; } set {_lastCachedFullNameAndAgeRefesh = -1; _name = value; } }
        public void RefreshFullNameAndAge()
        {
                string fullNameAndAge = "";
                fullNameAndAge += this.Name;
                if (House != null)
                {
                    fullNameAndAge += " " + House.Name;
                }
                if (this.HouseLordships.Count > 0)
                {
                    if (Sex == Sex.Male)
                    {
                        fullNameAndAge += ", Lord of House " + HouseLordships[0].Name;
                    }
                    else
                    {
                        fullNameAndAge += ", Lady of House " + HouseLordships[0].Name;
                    }
                    if (HouseLordships.Count > 1)
                    {
                        for (int i = 1; i < HouseLordships.Count(); i++)
                        {
                            if (i == HouseLordships.Count - 1)
                            {
                                fullNameAndAge += ", and ";
                            }
                            else
                            {
                                fullNameAndAge += ", ";
                            }
                            fullNameAndAge += HouseLordships[i].Name;
                        }
                    }

                }
                if (this.Lordships.Count > 0)
                {
                    if (Sex == Sex.Male)
                    {
                        fullNameAndAge += ", Lord of " + Lordships[0].Name;
                    }
                    else
                    {
                        fullNameAndAge += ", Lady of " + Lordships[0].Name;
                    }
                    if (Lordships.Count > 1)
                    {
                        for (int i = 1; i < Lordships.Count(); i++)
                        {
                            if (i == Lordships.Count - 1)
                            {
                                fullNameAndAge += ", and ";
                            }
                            else
                            {
                                fullNameAndAge += ", ";
                            }
                            fullNameAndAge += Lordships[i].Name;
                        }
                    }
                }
                if (World != null)
                {
                    foreach (var house in World.NobleHouses)
                    {
                        if (!house.Vacant)
                        {
                            var houseHeirs = house.Lords.Last().GetCurrentHeirs();
                            if (houseHeirs.Contains(this))
                            {
                                fullNameAndAge += string.Format(", heir of House {1}", houseHeirs.IndexOf(this) + 1, house.Name);
                            }
                        }
                    }
                    //foreach (var lordship in World.Lordships)
                    //{
                    //    if (!lordship.Vacant)
                    //    {
                    //        var lordshipHeirs = lordship.GetOrderOfSuccession(1);
                    //        if (lordshipHeirs.Contains(this))
                    //        {
                    //            fullNameAndAge += string.Format(", heir of {1}", lordshipHeirs.IndexOf(this) + 1, lordship.Name);
                    //        }
                    //    }
                    //}
                }
                if (this.Father != null && this.Mother != null)
                {
                    if (this.Sex == Sex.Male)
                    {
                        fullNameAndAge += ", son of ";
                    }
                    else
                    {
                        fullNameAndAge += ", daughter of ";
                    }
                    fullNameAndAge += this.Father.Name + " " + Father.House.Name + " and " + this.Mother.Name + " " + Mother.House.Name;
                }
                if (Household != null && Household.Lordship != null)
                {
                    fullNameAndAge += ", residing in " + Household.Lordship.Name;
                }
                fullNameAndAge += ", age " + this.Age * Constants.AGE_MULTIPLIER;
                _cachedFullNameAndAge =  fullNameAndAge;
            

        }
        public string FullNameAndAge
        {
            get {
                if (_lastCachedFullNameAndAgeRefesh != World.Year)
                {
                    RefreshFullNameAndAge();
                    _lastCachedFullNameAndAgeRefesh = World.Year;
                }
                return _cachedFullNameAndAge;
            }
        }
        public Profession Profession { get; set; }
        public List<Person> Children { get; set; }
        public Person Father { get; set; }
        public Person Mother { get; set; }
        public Person Spouse { get; set; }
        public int Age { get; set; }
        public bool IsAlive { get; set; }
        public Sex Sex { get; set; }
        public double Income
        {
            get
            {
                double income = 0;
               //only peasants in prime produce income
               if (Age>=Constants.AGE_OF_MAJORITY && Age<=Constants.AGE_OF_RETIREMENT && Class == SocialClass.Peasant)
                {
                    income = Constants.PEASANT_INCOME;
                }
                // +/- 50%
                return Math.Round(income + (income * (_rnd.NextDouble() * 0.5) * _rnd.Next(-1, 2)), 2);
            }
        }
        public double Cost
        {
            get
            {
                double cost = 0;
                if (Class == SocialClass.Noble)
                {
                    cost = Constants.NOBLE_COST;
                } else if (Age >= Constants.AGE_OF_MAJORITY && Age <= Constants.AGE_OF_RETIREMENT && Class == SocialClass.Peasant)
                {
                    cost = Constants.PEASANT_COST;
                } else
                {
                    cost = Constants.DEPENDENT_COST;
                }
                return cost;
            }
        }
        public List<Lordship> Lordships { get; set; }
        public List<House> HouseLordships { get; set; }
        public void IncrementYear()
        {
            if (IsAlive)
            {
                //Death Check
                if (_rnd.Next(1, Constants.AGE_OF_RETIREMENT *2) < Age && _rnd.Next(1, Constants.AGE_OF_RETIREMENT * 2) < Age && _rnd.Next(1, Constants.AGE_OF_RETIREMENT * 2) < Age && _rnd.Next(1, Constants.AGE_OF_RETIREMENT * 2) < Age)
                {
                    if (House.Player != null && (IsHouseLord() || IsHouseHeir()))
                    {
                        House.RecordHistory("DEATH: " + FullNameAndAge + " DIED in " + World.Year + "\n");
                    }
                    Kill();
                }
                else
                {
                    Age++;
                    //Childbirth Check
                    //Must be female
                    //Can only happen between 18 and 50
                    //every year between 18 and 50 they have a percentage change of a childbirth check
                    //and then they roll a die between 18 and 51 and must roll larger than their age
                    if (
                        Sex == Sex.Female
                        && Age >= Constants.AGE_OF_MAJORITY
                        && Spouse != null
                        && Spouse.IsAlive
                        && _rnd.NextDouble() <= Constants.CHILDBEARING_CHANCE_IN_PRIME
                        && _rnd.Next(Constants.AGE_OF_MAJORITY, Constants.AGE_OF_RETIREMENT+1) > Age
                        )
                    {
                        var childsMother = this;
                        var childsFather = Spouse;
                        var newborn = new Person(_rnd)
                        {
                            Household = childsMother.Household,
                            BirthYear = World.Year,
                            BirthPlace = childsMother.Household.Lordship,
                            Father = childsFather,
                            Mother = childsMother,
                            Profession = Profession.Dependant,
                            Class = childsMother.Class,
                            People = childsMother.People
                        };
                        if (childsMother.Profession == Profession.Noble)
                        {
                            newborn.Profession = Profession.Noble;
                        }
                        newborn.Ancestors.Add(childsFather);
                        newborn.Ancestors.Add(childsMother);
                        newborn.Ancestors.AddRange(childsFather.Ancestors);
                        newborn.Ancestors.AddRange(childsMother.Ancestors);
                        Children.Add(newborn);
                        Spouse.Children.Add(newborn);
                        Household.AddMember(newborn);
                        House.AddMember(newborn);
                        World.AddPerson(newborn);
                        if (House.Player != null && newborn.House.Lord!=null && (newborn.House.Lord.Children.Contains(newborn) || newborn.IsHouseLord()||newborn.IsHouseHeir()))
                        {       
                            House.RecordHistory("BIRTH: " + newborn.FullNameAndAge + " WAS BORN in " + World.Year + "\n");
                        }
                        var sovreign = House.GetSovreign();
                        /*
                        if (sovreign.Player!= null && sovreign.Player.PlayerType == PlayerType.Live && sovreign.Lord != null && sovreign.Lord.GetCurrentHeirs().Contains(newborn))
                        {
                            Console.WriteLine(String.Format("REJOICE! A new heir, a {0}, was born to {1} and {2}.\n What name do you give {3}?"
                                , newborn.Sex == Sex.Male ? "son" : "daughter",
                                Household.HeadofHousehold.FullNameAndAge,
                                Household.HeadofHousehold.Spouse.FullNameAndAge, newborn.Sex == Sex.Male ? "him" : "her"));
                            newborn.Name = Console.ReadLine();
                            Console.WriteLine("The kingdom rejoices at the news of the new heir, " + newborn.FullNameAndAge);
                            Console.WriteLine("Enter to continue...");
                            Console.ReadLine();
                        }
                        */
                    }
                }
            }
        }
        public bool IsEligibleForMarriage()
        {
            return (
                IsAlive
                && (Spouse == null || !Spouse.IsAlive)
                && Age >= Constants.AGE_OF_MAJORITY
                && Age < Constants.AGE_OF_RETIREMENT
                );
        }
        public bool IsEligableForBetrothal()
        {
            return (
                IsAlive
                && (Bethrothal == null || !Bethrothal.HeadOfHouseholdToBe.IsAlive || !Bethrothal.SpouseToBe.IsAlive)
                && (Spouse == null || !Spouse.IsAlive)
                && Age < Constants.AGE_OF_RETIREMENT
                );
        }
        public bool IsCompatible(Person potentialSpouse)
        {
            var me = this;
            //I can't get married if I'm not eligible for marriage
            if (!me.IsEligibleForMarriage() || !potentialSpouse.IsEligibleForMarriage())
            {
                return false;
            }
            //I can't get married if either of us are dead
            if (!me.IsAlive || !potentialSpouse.IsAlive)
            {
                return false;
            }
            //not compatible if differnt peoples
            if (me.People != potentialSpouse.People)
            {
                return false;
            }
            //not compatible if differnt class
            if (me.Class != potentialSpouse.Class)
            {
                return false;
            }
            //must be opposite sex
            if (me.Sex == potentialSpouse.Sex)
            {
                return false;
            }
            //nobles can't share an ancestor

            if (me.Class == SocialClass.Noble && me.Ancestors.Intersect(potentialSpouse.Ancestors).Count() > 0)
            {
                return false;
            }

            //peasants can't share a grandparent
            if (me.Class == SocialClass.Peasant && me.GetAncestors(2).Intersect(potentialSpouse.GetAncestors(2)).Count() > 0)
            {
                return false;
            }
            //still here?  That means we're compatible!
            return true;
        }
        public bool IsCompatibleForBetrothal(Person potentialSpouse)
        {
            var me = this;
            //I can't get married if I'm not eligible for betrothal
            if (!me.IsEligableForBetrothal() || !potentialSpouse.IsEligableForBetrothal())
            {
                return false;
            }
            //I can't get married if either of us are dead
            if (!me.IsAlive || !potentialSpouse.IsAlive)
            {
                return false;
            }
            //not compatible if differnt peoples
            if (me.People != potentialSpouse.People)
            {
                return false;
            }
            //not compatible if differnt class
            if (me.Household != null && potentialSpouse.Household != null && me.Household.HouseholdClass != potentialSpouse.Household.HouseholdClass)
            {
                return false;
            }
            //not compatible if age difference is more than MAX_BETHROTHAL_AGE_DIFFERENCE
            if (Math.Abs(me.Age - potentialSpouse.Age) > Constants.MAX_BETROTHAL_AGE_DIFFERENCE)
            {
                return false;
            }
            //must be opposite sex
            if (me.Sex == potentialSpouse.Sex)
            {
                return false;
            }
            //nobles can't share an ancestor

            //if (me.Class == SocialClass.Noble && me.Ancestors.Intersect(potentialSpouse.Ancestors).Count() > 0)
            //{
            //    return false;
            //}

            //peasants can't share a grandparent
            if (/*me.Class == SocialClass.Peasant &&*/ me.GetAncestors(2).Intersect(potentialSpouse.GetAncestors(2)).Count() > 0)
            {
                return false;
            }
            //still here?  That means we're compatible!
            return true;
        }
        public static string GenerateName(Random rnd, Sex sex)
        {
            string generatedName = "";
            string[] names;
            if (sex == Sex.Male)
            {
                names = Constants.MALE_NAMES.Split(',');
            }
            else
            {
                names = Constants.FEMALE_NAMES.Split(',');
            }
            generatedName = names[rnd.Next(0, names.Count())];
            return generatedName;
        }
        public List<Person> GetAncestors(int maxGenerations = Constants.GENERATIONS_WITHOUT_SHARED_ANCESTOR)
        {
            var ancestors = new List<Person>();
            if (maxGenerations > 0 && Father != null)
            {
                ancestors.Add(Father);
                ancestors.AddRange(Father.GetAncestors(maxGenerations - 1).Where(x => !ancestors.Contains(x)));
            }
            if (maxGenerations > 0 && Mother != null)
            {
                ancestors.Add(Father);
                ancestors.AddRange(Mother.GetAncestors(maxGenerations - 1).Where(x => !ancestors.Contains(x)));
            }
            return ancestors;
        }
        public List<Person> GetHeirs()
        {
            var heirs = new List<Person>();
            var ChildrenOrderedByAge = Children.OrderBy(child => child.BirthYear);
            foreach (var child in ChildrenOrderedByAge)
            {
                if (!heirs.Contains(child)) { heirs.Add(child); }
                heirs.AddRange(child.GetHeirs());
            }
            return heirs;
        }
        public List<Person> GetCurrentHeirs()
        {
            var heirs = new List<Person>();
            var ChildrenOrderedByAge = Children.OrderBy(child => child.BirthYear);
            var heirBranchIdentified = false;
            foreach (var child in ChildrenOrderedByAge)
            {
                if (!heirBranchIdentified)
                {
                    if (child.IsAlive)
                    {
                        heirs.Add(child);
                    }
                    heirs.AddRange(child.GetCurrentHeirs());
                    if (heirs.Count() > 0)
                    {
                        heirBranchIdentified = true;
                    }
                }
            }
            return heirs;
        }
        public static void CreateMarriages(List<Person> unmarriedPeople, Random rnd, bool echo = false)
        {
            var unmarriedMenInPrime = unmarriedPeople.Where(x => x.Sex == Sex.Male).OrderBy(x => x.Age).ToList();
            var unmarriedWomenInPrime = unmarriedPeople.Where(x => x.Sex == Sex.Female).OrderBy(x => x.Age).ToList();
            while (unmarriedMenInPrime.Count() > 0 && unmarriedWomenInPrime.Count() > 0)
            {
                var groom = unmarriedMenInPrime[0];
                var bride = unmarriedWomenInPrime.FirstOrDefault(x => x.IsCompatible(groom));
                if (bride != null)
                {
                    groom.Spouse = bride;
                    bride.Spouse = groom;
                    Person headOfHousehold;
                    Person spouse;
                    if (rnd.Next(0, 2) < 1)
                    {
                        headOfHousehold = bride;
                        spouse = groom;
                    }
                    else
                    {
                        headOfHousehold = groom;
                        spouse = bride;
                    }

                    Household.CreateMarriageHousehold(headOfHousehold, spouse);
                }
                unmarriedMenInPrime.Remove(groom);
                unmarriedWomenInPrime.Remove(bride);
            }
        }
        public Bethrothal Bethrothal { get; set; }
        public string GetDetailsAsString()
        {
            string retString = "";

            retString += "Name: " + FullNameAndAge + "\n";
            retString += "People: " + People + "\n";
            retString += "Class: " + Class + "\n";
            if (Spouse != null && Spouse.IsAlive)
            {
                retString += "Spouse: " + Spouse.FullNameAndAge + "\n";
            }
            if (Bethrothal != null)
            {
                retString += "Betrothal:\n"
                + "\tBetrothal Year:" + Bethrothal.Year + "\n"
                + "\tHead of Household to Be:" + Bethrothal.HeadOfHouseholdToBe.FullNameAndAge + "\n"
                + "\tSpouse to Be: " + Bethrothal.SpouseToBe.FullNameAndAge + "\n";
            }
            if (Children.Count() > 0)
            {
                retString += "Children:\n";
                foreach (var child in Children.OrderByDescending(c => c.Age))
                {
                    retString += "\t" + child.FullNameAndAge + "\n";
                }
            }
            if (Household != null)
            {
                retString += "Head of Household:" + Household.HeadofHousehold.FullNameAndAge + "\n";
            }

            return retString;
        }
        public Lordship Location { get { if (Household != null) { return Household.Lordship; } else return null; } }
        //public int NumberOfMovesThisYear { get; set; }
        public void Kill()
        {
            IsAlive = false;
            if (Household != null)
            {
                Household.RemoveMember(this);
            }
        }
        public bool IsHouseHeir()
        {
            if (House.Lord != null)
            {
                return House.Lord.GetCurrentHeirs().Contains(this);
            } else
            {
                return false;
            }
        }
        public bool IsHouseLord()
        {
            return House.Lord == this;
        }
        public Person Flatten()
        {
            return new Person(_rnd) {
                Age = Age,
                Ancestors = Ancestors.Select(a=>new Person(_rnd) { Id = a.Id}).ToList(),
                Bethrothal = new Bethrothal() { Id = Bethrothal.Id},
                BirthPlace = new Lordship(_rnd) { Id = BirthPlace.Id},
                BirthYear = BirthYear,
                Children = Children.Select(c=>new Person(_rnd) { Id = c.Id}).ToList(),
                Class = Class,
                Father = new Person(_rnd) { Id = Father.Id},
                Mother = new Person(_rnd) { Id = Mother.Id},
                House = new House() { Id = House.Id},
                Household = new Household() { Id = Household.Id},
                HouseLordships = HouseLordships.Select(hl=>new House() { Id = hl.Id}).ToList(), 
                Id = Id, 
                Sex = Sex, 
                IsAlive = IsAlive, 
                Profession = Profession, 
                Lordships = Lordships.Select(l=>new Lordship(_rnd) { Id = l.Id}).ToList(),
                Name = Name, 
                People = People, 
                Spouse = new Person(_rnd) { Id = Spouse.Id}, 
                World = new World(_rnd) { Id = World.Id}
            };
        }
    }

}
