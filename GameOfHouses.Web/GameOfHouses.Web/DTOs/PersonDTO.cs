using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameOfHouses.Logic;
using static GameOfHouses.Web.Utility.Helpers;

namespace GameOfHouses.Web.DTOs
{
    public class PersonDTO
    {
        public PersonDTO() { }
        public PersonDTO(Person p, bool flattenChildObjects = false)
        {
            Id = p.Id;
            Name = p.Name;
            Age = p.Age;
            Sex = p.Sex;
            if (flattenChildObjects)
            {
                if (p.House != null)
                {
                    House = new HouseDTO()
                    {
                        Id = p.House.Id,
                        Name = p.House.Name
                    };
                }
                if (p.Household != null)
                {
                    Residence = new LordshipDTO()
                    {
                        Id = p.Household.Lordship.Id,
                        Name = p.Household.Lordship.Name
                    };
                }
            }
            else
            {
                if (p.House != null) { House = new HouseDTO(p.House); }
                if (p.Household != null) { Residence = new LordshipDTO(p.Household.Lordship); }
            }
            FullNameAndAge = p.FullNameAndAge;
            //FullNameAndAgeWithLinks = GetFullNameAndAgeWithLinks(p);
        }
        public Guid Id { get; set; }
        public String Name { get; set; }
        public HouseDTO House { get; set; }
        public int Age { get; set; }
        public Sex Sex { get; set; }
        public LordshipDTO Residence { get; set; }
        public String FullNameAndAge { get; set; }
        //public String FullNameAndAgeWithLinks { get; set; }
        public String Relation { get; set; }
    }
}