using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public class Household
    {
        public Household()
        {
            Members = new List<Person>();
            Lordship = null;
            HouseholdClass = SocialClass.Peasant;
        }
        //The members of the family
        //public Person HouseFounder { get; set; }
        public Guid Id { get; set; }
        public Person HeadofHousehold { get; set; }
        public List<Person> Members { get; set; }
        //Where the family currently lives
        public Lordship Lordship { get; set; }
        public SocialClass HouseholdClass { get; set; }
        public void AddMember(Person newMember)
        {
            if (!Members.Contains(newMember))
            {
                if (newMember.Household != null)
                {
                    newMember.Household.RemoveMember(newMember);
                }
                Members.Add(newMember);
                newMember.MoveToLocation(Lordship);
                newMember.Household = this;
            }
        }
        public void RemoveMember(Person oldMember)
        {
            Members.Remove(oldMember);
            oldMember.Household = null;
            if (Members.Count == 0)
            {
                if (Lordship != null)
                {
                    Lordship.Households.Remove(this);
                    Lordship = null;
                }
            }
            else if (oldMember == HeadofHousehold)
            {
                //make eldest head of household
                HeadofHousehold = Members.OrderBy(x => x.Age).Last();
            }
        }
        public string GetDetailsAsString()
        {
            var retStr = "";
            retStr += "Class:" + HouseholdClass + "\n"
            + "Lordship: " + Lordship.Name + "\n"
            + "Head of Household: " + HeadofHousehold.Name + "\n"
            + "Members:" + "\n";
            foreach (var member in Members)
            {
                retStr += "\t" + member.FullNameAndAge + "\n";
            }
            return retStr;
        }
        public static Household CreateMarriageHousehold(Person headOfHousehold, Person spouse, bool echo = false)
        {
            Household marriageHousehold;
            var headsOldHousehold = headOfHousehold.Household;
            var headsOldHouse = headOfHousehold.House;
            var spousesOldHousehold = spouse.Household;
            var spousesOldHouse = spouse.House;
            House newHouse = null;
            var world = headsOldHouse.World;

            //create new house unless headOfHousehold is lord
            if (headOfHousehold.Class == SocialClass.Noble && headsOldHouse.Player != null && headOfHousehold != headsOldHouse.Lord)
            {

                newHouse = new House() { Name = headOfHousehold.Name + headsOldHouse.Name.ToLower(), Symbol = headOfHousehold.Name[0] };
                var game = headsOldHouse.Player.Game;
                newHouse.AddMember(headOfHousehold);
                newHouse.AddLord(headOfHousehold);
                var newHousePlayerType = PlayerType.AISubmissive;
                if (headsOldHouse.Player.PlayerType == PlayerType.Live && headsOldHouse.Player.PlayerType == PlayerType.AIAggressive)
                {
                    newHousePlayerType = PlayerType.AIAggressive;
                }
                newHouse.AddPlayer(new Player() { PlayerType = newHousePlayerType });
                world.AddHouse(newHouse);
                game.AddPlayer(newHouse.Player);
                headsOldHouse.AddVassle(newHouse);
                var foundingRecord = "HOUSE FOUNDING: " + headOfHousehold.FullNameAndAge + " FOUNDED House " + newHouse.Name + " in " + world.Year + "\n";
                newHouse.RecordHistory(foundingRecord);
                headsOldHouse.RecordHistory(foundingRecord);
            }

            if (headOfHousehold.Class == SocialClass.Noble)
            {
                var news = "MARRIAGE: " + headOfHousehold.FullNameAndAge + " MARRIED " + spouse.FullNameAndAge + " in " + headOfHousehold.World.Year + "\n";
                if (newHouse != null) { newHouse.RecordHistory(news); }
                if (headsOldHouse != null && headOfHousehold.House.Player != null)
                {
                    var year = headsOldHouse.World.Year;
                    headsOldHouse.RecordHistory(news);
                }
                if (spousesOldHouse != null && spouse.House.Player != null)
                {
                    var year = spousesOldHouse.World.Year;
                    spousesOldHouse.RecordHistory(news);
                }
            }
            if (headsOldHousehold != null && headsOldHousehold.HeadofHousehold == headOfHousehold)
            {
                marriageHousehold = headOfHousehold.Household;
            }
            else
            {
                marriageHousehold = new Household()
                {
                    HeadofHousehold = headOfHousehold
                };
                if (headsOldHousehold != null)
                {
                    marriageHousehold.HouseholdClass = headsOldHousehold.HouseholdClass;
                    //headsOldHousehold.Lordship.AddHousehold(marriageHousehold);
                    //marriageHousehold.Lordship = headsOldHousehold.Lordship;
                }
            }
            marriageHousehold.AddMember(headOfHousehold);
            marriageHousehold.AddMember(spouse);
            headOfHousehold.House.AddMember(spouse);
            if (marriageHousehold.Lordship == null && headsOldHousehold != null && headsOldHousehold.Lordship !=null)
            {
                headsOldHousehold.Lordship.AddHousehold(marriageHousehold);
            }

            //add head's minor children to household
            var headsMinorChildren = headOfHousehold.Children.Where(x => x.Age < 18 & x.IsAlive).ToList();
            if (headsMinorChildren.Count() > 0)
            {
                headsMinorChildren.ForEach(x => {
                    marriageHousehold.AddMember(x);
                });
            }

            //add spouse's minor children to household
            var spousesMinorChildren = spouse.Children.Where(x => x.Age < 18 & x.IsAlive).ToList();
            if (spousesMinorChildren.Count() > 0)
            {
                spousesMinorChildren.ForEach(x => {
                    marriageHousehold.AddMember(x);
                    //kids house doesn't change
                });
            }
            headOfHousehold.Spouse = spouse;
            spouse.Spouse = headOfHousehold;
            return marriageHousehold;
        }
        public void Resettle(Lordship newLordship)
        {
            newLordship.AddHousehold(this);
            //move all members that are present in the oldLordship  
        }
    }
}

