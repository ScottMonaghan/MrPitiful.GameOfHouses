using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public class House
    {
        public House()
        {
            Id = Guid.NewGuid();
            Lords = new List<Person>();
            Members = new List<Person>();
            //Soldiers = new List<Person>();
            AvailableUnmarriedMembers = new List<Person>();
            Vassles = new List<House>();
            AssessedIncome = new List<AssessedIncome>();
            Recruitments = 0;
            Player = null;
            DeploymentRequests = new List<DeploymentRequest>();
            History = new Dictionary<int, string>();
        }
        //public List<Person> Soldiers { get; set; }
        public Guid Id { get; set; }
        public Dictionary<int,string> History { get; set; }
        public void RecordHistory(string record)
        {
            if (Lord != null && Lord.Class == SocialClass.Noble)
            {
                var year = World.Year;
                if (!History.ContainsKey(year))
                {
                    History[year] = string.Empty;
                }
                History[year] += record;
            }
        }
        public House GetSovreign()
        {
            var sovreign = this;
            while (sovreign.Allegience != null)
            {
                sovreign = sovreign.Allegience;
            }
            return sovreign;
        }
        public int Recruitments { get; set; }
        public Player Player { get; set; }
        public void AddPlayer(Player player)
        {
            Player = player;
            player.House = this;
        }
        public Lordship Seat { get; set; }
        public List<Lordship> Lordships
        {
            get
            {
                return World.Lordships.Where(l => l.Lord!=null && l.Lord.House == this).ToList();
            }
        }
        public House Allegience { get; set; }
        public List<House> Vassles { get; set; }
        public World World { get; set; }
        public char Symbol { get; set; }
        public String Name { get; set; }
        public bool Vacant { get; set; }
        public List<Person> Lords { get; set; }
        public Person Lord
        {
            get
            {
                if (Lords.Count() > 0)
                {
                    return Lords.Last();
                }
                else
                {
                    return null;
                }
            }
        }
        public List<Person> Members { get; set; }
        public List<Person> AvailableUnmarriedMembers { get; set; }
        public void AddMember(Person newMember)
        {
            if (!Members.Contains(newMember))
            {
                if (newMember.House != null)
                {
                    newMember.House.RemoveMember(newMember);
                }
                Members.Add(newMember);
                newMember.House = this;
            }
        }
        public void RemoveMember(Person oldMember)
        {
            if (Members.Contains(oldMember))
            {
                if (oldMember.House != null)
                {
                    oldMember.House = null;
                }
                Members.Remove(oldMember);
            }
        }
        public List<Person> GetOrderOfSuccession(int depth)
        {
            var successionList = new List<Person>();
            var lordIndex = Lords.Count - 1;
            while (successionList.Count() < depth && lordIndex >= 0)
            {
                var lordsHeirs = Lords[lordIndex].GetHeirs().Where(heir => heir.IsAlive);
                foreach (var heir in lordsHeirs)
                {
                    if (!successionList.Contains(heir) && !Lords.Contains(heir))
                    {
                        successionList.Add(heir);
                    }
                }
                lordIndex--;
            }
            return successionList.Take(depth).ToList();
        }
        public void IncrementYear(Random rnd)
        {
            if (!Vacant)
            {
                var incumbentLord = Lords.Last();
                bool echo = (incumbentLord.House.Player !=null && incumbentLord.House.Player.PlayerType == PlayerType.Live);
                //CollectTaxes();
                //RecruitSoldiers(rnd);
                if (!incumbentLord.IsAlive)
                {
                    //The lord is Dead!  Long live the lord!

                    //if (echo)
                    //{
                    if (Player != null)
                    {
                        RecordHistory(string.Format("DEATH OF HOUSE LORD: In {0} {1} DIED.\n", World.Year, incumbentLord.FullNameAndAge));
                    }
                        //}

                    Person heir = null;
                    var orderOfSuccession = GetOrderOfSuccession(1);
                    if (orderOfSuccession.Count > 0)
                    {
                        heir = orderOfSuccession[0];
                    }
                    if (heir != null)
                    {
                        AddLord(heir);
                        //echo = echo || heir.House.Player.PlayerType == PlayerType.Live;
                        //if (echo)
                        //{
                        if (Player != null)
                        {
                            RecordHistory(heir.FullNameAndAge + " INHERITED the Lordship of House " + Name + " from " + incumbentLord.FullNameAndAge + " in " + World.Year + "\n");
                        }
                        //set up additional houses for incumbant lords other children
                        //var lordsOtherChildren = incumbentLord.Children.Where(c => c != heir || !heir.Ancestors.Contains(c));
                        

                            //}
                    }
                    else
                    {
                        Vacant = true;
                        //release vassles
                        foreach (var vassle in Vassles.ToList())
                        {
                            RemoveVassle(vassle);
                        }
                            //if (echo)
                            //{
                            if (Player != null)
                        {
                            RecordHistory("END OF HOUSE: The Lordship of House " + Name + " is vacant.\n");
                        }
                            //}
                    }
                }
                if (Lord.House != this)
                {
                    //HOUSE ABSORBED
                    //if (echo)
                    //{
                    var houseLossNews="END OF HOUSE! House " + Name + " was absorbed by " + Lord.House.Name + " in " + World.Year+"\n";
                    if (Player != null)
                    {
                       RecordHistory(houseLossNews);
                    }
                    var newHouse = Lord.House;
                    if (Lord.House.Player != null)
                    {
                        Lord.House.RecordHistory(houseLossNews);
                        var oldHousePlayer = Player;
                        var newHousePlayer = newHouse.Player;
                        if (oldHousePlayer.PlayerType == PlayerType.Live && newHousePlayer.PlayerType != PlayerType.Live)
                        {
                            newHouse.AddPlayer(oldHousePlayer);
                            Player = null;
                        }
                    }
                    //}
                    //add all members to new house
                    foreach (var absorbedMember in incumbentLord.House.Members.ToList())
                    {
                        Lord.House.AddMember(absorbedMember);
                    }
                    foreach (var vassle in Vassles.ToList())
                    {
                        if (vassle != newHouse)
                        {
                            newHouse.AddVassle(vassle);
                        } else
                        {
                            RemoveVassle(vassle);
                        }
                    }
                    Lords.Clear();
                    Vacant = true;
                }
            }
        }
        public void AddLord(Person newLord)
        {
            if (!newLord.HouseLordships.Contains(this))
            {
                newLord.HouseLordships.Add(this);
            }
            if (newLord.Household != null && newLord != newLord.Household.HeadofHousehold)
            {
                var newHousehold = new Household()
                {
                    HeadofHousehold = newLord
                };
                newHousehold.AddMember(newLord);
                if (newLord.Spouse != null && newLord.Spouse.IsAlive)
                {
                    newHousehold.AddMember(newLord.Spouse);
                    var spousesMinorChildren = newLord.Spouse.Children.Where(x => x.Age < Constants.AGE_OF_MAJORITY);
                    foreach (var child in spousesMinorChildren)
                    {
                        newHousehold.AddMember(child);
                    }
                }
                var lordsMinorChildren = newLord.Children.Where(x => x.Age < Constants.AGE_OF_MAJORITY);
                foreach (var child in lordsMinorChildren)
                {
                    newHousehold.AddMember(child);
                }
            }
            if (Seat != null && (newLord.Household.Lordship == null || newLord.Household.Lordship != Seat))
            {
                Seat.AddHousehold(newLord.Household);
            }
            Lords.Add(newLord);
        }
        public List<House> GetAllSubVassles()
        {
            var subVassles = new List<House>();
            foreach (var vassle in Vassles)
            {
                subVassles.Add(vassle);
                subVassles.AddRange(vassle.GetAllSubVassles());
            }
            return subVassles;
        }
        public string GetDetailsAsString()
        {
            var retString = World.GetMapAsString(this);
            retString += ("House: " + Name + '\n');
            var incumentlord = Lords.Last();
            retString += ("Lord: " + incumentlord.FullNameAndAge + '\n');
            retString += ("Total Noble Households: " + World.Lordships.Sum(x => x.Households.Count(y => y.HeadofHousehold.Class == SocialClass.Noble && y.HeadofHousehold.House == this))) + "\n";
            retString += ("Total Peasant Households: " + World.Lordships.Where(ls => !ls.Vacant && ls.Lords.Last().House == this).Sum(x => x.Households.Count(y => y.HeadofHousehold.Class == SocialClass.Peasant))) + "\n";
            retString += ("Wealth: " + Wealth.ToString("0.00") + '\n');
            var lastYear = World.Year - 1;
            var lastYearsIncome = AssessedIncome.FirstOrDefault(x => x.Year == lastYear);
            if (lastYearsIncome != null)
            {
                retString += lastYear + " Income: " + lastYearsIncome.Income.ToString("0.00") + "\n";
            }
            retString += "Lordships: " + World.Lordships.Where(t => !t.Vacant && t.Lords.Last().House == this).Count() + "\n";
            //retString += "Soldiers: " + Soldiers.Count(s => s.IsAlive) + "\n";
            retString += ("Order of Succession:" + '\n');
            var orderOfSuccession = GetOrderOfSuccession(10);
            for (int i = 0; i < orderOfSuccession.Count(); i++)
            {
                retString += ((i + 1) + ": " + orderOfSuccession[i].FullNameAndAge + '\n');
            }
            if (Allegience != null)
            {
                retString += "Allegience: " + Allegience.Name + "\n";
            }
            retString += "Vassles:\n";
            foreach (var vassle in Vassles)
            {
                retString += "\t" + vassle.Name + "(" + vassle.GetAllSubVassles().Count() + " sub-vassles)\n";
            }
            return retString;
        }
        public void AddVassle(House vassle)
        {
            if (!Vassles.Contains(vassle))
            {
                if (vassle.Allegience != null)
                {
                    vassle.Allegience.RemoveVassle(vassle);
                }
                Vassles.Add(vassle);
                //send all vassle armies home
                //vassle.Lordships.ForEach(l => l.AddOccupyingLordAndArmy(l));
                vassle.Allegience = this;
            }
        }
        public void RemoveVassle(House vassle)
        {
            if (Vassles.Contains(vassle))
            {
                if (vassle.Allegience != null)
                {
                    vassle.Allegience = null;
                }
                Vassles.Remove(vassle);
            }
        }
        public List<Person> GetIndespensibleMembers(int numberOfPossibleHeirsPerGeneration = Constants.NUMBER_OF_POSSIBLE_HEIRS_PER_GENERATION)
        {
            //var indespensibleMembers = new List<Person>();
            ////get house heirs
            //var livingHeirs = Lords[0].GetHeirs().Where(member =>
            //    member.IsAlive
            //    &&
            //    (member.Father != null && member.Mother != null)
            //    && (
            //        member.Father.Children.Where(child => child.IsAlive).OrderByDescending(child => child.Age).ToList().IndexOf(member) < numberOfPossibleHeirsPerGeneration
            //        || member.Mother.Children.Where(child => child.IsAlive).OrderByDescending(child => child.Age).ToList().IndexOf(member) < numberOfPossibleHeirsPerGeneration
            //    )
            //).ToList();
            //// get heirs of each house lordship
            //var houseLordshipLords = Members.Where(m => m.Lordships.Count > 0).ToList();
            //var lordshipHeirs = new List<Person>();
            //houseLordshipLords.ForEach(lordshipLord =>
            //    lordshipHeirs.Union(
            //        lordshipLord.GetHeirs().Where(member =>
            //            member.IsAlive
            //            &&
            //            (member.Father != null && member.Mother != null)
            //            && (
            //                member.Father.Children.Where(child => child.IsAlive).OrderByDescending(child => child.Age).ToList().IndexOf(member) < numberOfPossibleHeirsPerGeneration
            //                || member.Mother.Children.Where(child => child.IsAlive).OrderByDescending(child => child.Age).ToList().IndexOf(member) < numberOfPossibleHeirsPerGeneration
            //            )
            //        )
            //    )
            //);
            //return livingHeirs.Union(houseLordshipLords).Union(lordshipHeirs).ToList();
            return Members.Where(m=>
                (
                    m.Father != null
                    && m.Father.Children.Where(c => c.IsAlive).OrderByDescending(c => c.Age).ToList().IndexOf(m) + 1 <= numberOfPossibleHeirsPerGeneration
                )
                ||
                (
                    m.Mother != null
                    && m.Mother.Children.Where(c => c.IsAlive).OrderByDescending(c => c.Age).ToList().IndexOf(m) + 1 <= numberOfPossibleHeirsPerGeneration
                )
            ).ToList();
        }
        public List<Person> GetDispensibleMembers()
        {
            //return Members.Except(GetPossibleHeirs()).ToList();
            return Members.Except(GetIndespensibleMembers()).ToList();
        }
        public double Wealth { get; set; }
        public List<Lordship> GetLordships()
        {
            return World.Lordships.Where(lordship => !lordship.Vacant && lordship.Lords.Last().House == this).ToList();
        }
        //public void CollectTaxes()
        //{
        //    //set tax year for tax collection
        //    var previousTaxYear = World.Year - 1;
        //    double houseIncome = 0;

        //    //first collect assessed taxes from previous year from each lordship under house control
        //    var houseLordships = GetLordships();
        //    foreach (var houseLordship in houseLordships)
        //    {
        //        var assessedLordshipIncome = houseLordship.AssessedIncome.FirstOrDefault(income => income.Year == previousTaxYear);
        //        if (assessedLordshipIncome != null)
        //        {
        //            //move ALL income from lordship wealth to house coffers (the house lord controls all wealth for the house)
        //            houseLordship.Wealth -= assessedLordshipIncome.Income;
        //            houseIncome += assessedLordshipIncome.Income;
        //        }
        //    }
        //    //next collect assessed taxes from all vassle houses
        //    foreach (var vassle in Vassles)
        //    {
        //        var assessedVassleIncome = vassle.AssessedIncome.FirstOrDefault(income => income.Year == previousTaxYear);
        //        if (assessedVassleIncome != null)
        //        {
        //            //tax vassle income at tax rate
        //            var tax = assessedVassleIncome.Income * Constants.MIN_TAX_RATE;
        //            vassle.Wealth -= tax;
        //            houseIncome += tax;
        //        }
        //    }
        //    //add income to wealth
        //    Wealth += houseIncome;
        //    //add assessment for taxes
        //    AssessedIncome.Add(new AssessedIncome() { Year = World.Year, Income = houseIncome });
        //}
        public List<AssessedIncome> AssessedIncome { get; set; }
        public List<Person> GetPotentialSettlerLords()
        {
            var house = this;
            var potentialSettlerLords = house.Members.Where(x =>
            x.Lordships.Count() == 0
            && x.Household != null
            && x.Household.HeadofHousehold == x
            && x.GetHeirs().Count(heir => heir.House != house) == 0
            ).ToList();
            return potentialSettlerLords;

        }
        public List<DeploymentRequest> DeploymentRequests { get; set; }
        public List<Person> FightersAvailable {
            get {
                return Lordships.SelectMany(l => l.Army).Union(GetAllSubVassles().SelectMany(v => v.Lordships.SelectMany(l => l.Army))).ToList();
            }
        }
        public List<Person> LordshipCandidates
        {
            get
            {
                return Members.Where(m =>
                                m.IsAlive
                                && m.Age >= Constants.AGE_OF_MAJORITY
                                //&& m.Lordships.Count() == 0
                                && (m.Household.HeadofHousehold == m || m.Spouse == null)
                            ).OrderBy(m => m.Name).ToList();
            }
        } 
    }
}
