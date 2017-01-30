using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameOfHouses.Logic;

namespace GameOfHouses.Web.Utility
{
    public static class Helpers
    {
        public static string GetHouseLink(House h)
        {
            return string.Format("<a href='#/house/{0}'>{1}</a>", h.Id, h.Name);
        }
        public static string GetPersonLink(Person p)
        {
            return string.Format("<a href='#/person/{0}'>{1} {2}</a>", p.Id, p.Name, p.House.Name);
        }
        public static string GetLordshipLink(Lordship l)
        {
            return string.Format("<a href='#/lordship/{0}'>{1}</a>", l.Id,l.Name);
        }
        public static string GetFullNameAndAgeWithLinks(Person p)
        {
            string fullNameAndAge = "";
            fullNameAndAge += GetPersonLink(p);
            if (p.HouseLordships.Count > 0)
            {
                if (p.Sex == Sex.Male)
                {
                    fullNameAndAge += ", Lord of House " + GetHouseLink(p.HouseLordships[0]);
                }
                else
                {
                    fullNameAndAge += ", Lady of House " + GetHouseLink(p.HouseLordships[0]);
                }
                if (p.HouseLordships.Count > 1)
                {
                    for (int i = 1; i < p.HouseLordships.Count(); i++)
                    {
                        if (i == p.HouseLordships.Count - 1)
                        {
                            fullNameAndAge += ", and ";
                        }
                        else
                        {
                            fullNameAndAge += ", ";
                        }
                        fullNameAndAge += GetHouseLink(p.HouseLordships[i]);
                    }
                }

            }
            if (p.Lordships.Count > 0)
            {
                if (p.Sex == Sex.Male)
                {
                    fullNameAndAge += ", Lord of " + GetLordshipLink(p.Lordships[0]);
                }
                else
                {
                    fullNameAndAge += ", Lady of " + GetLordshipLink(p.Lordships[0]);
                }
                if (p.Lordships.Count > 1)
                {
                    for (int i = 1; i < p.Lordships.Count(); i++)
                    {
                        if (i == p.Lordships.Count - 1)
                        {
                            fullNameAndAge += ", and ";
                        }
                        else
                        {
                            fullNameAndAge += ", ";
                        }
                        fullNameAndAge += GetLordshipLink(p.Lordships[i]);
                    }
                }
            }
            if (p.World != null)
            {
                foreach (var house in p.World.NobleHouses)
                {
                    var houseHeirs = house.Lords.Last().GetCurrentHeirs();
                    if (houseHeirs.Contains(p))
                    {
                        fullNameAndAge += string.Format(", heir of House {0}", GetHouseLink(house));
                    }
                }
                foreach (var lordship in p.World.Lordships)
                {
                    if (!lordship.Vacant)
                    {
                        var lordshipHeirs = lordship.GetOrderOfSuccession(1);
                        if (lordshipHeirs.Contains(p))
                        {
                            fullNameAndAge += string.Format(", heir of {0}", GetLordshipLink(lordship));
                        }
                    }
                }
            }
            if (p.Father != null && p.Mother != null)
            {
                if (p.Sex == Sex.Male)
                {
                    fullNameAndAge += ", son of ";
                }
                else
                {
                    fullNameAndAge += ", daughter of ";
                }
                fullNameAndAge += GetPersonLink(p.Father) + " and " + GetPersonLink(p.Mother);
            }
            if (p.Household != null && p.Household.Lordship != null)
            {
                fullNameAndAge += ", residing in " + GetLordshipLink(p.Household.Lordship);
            }
            fullNameAndAge += ", age " + p.Age;
            return fullNameAndAge;
        }

    }
}