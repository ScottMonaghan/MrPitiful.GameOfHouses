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
        public PersonDTO(Person p)
        {
            Id = p.Id;
            Name = p.Name;
            House = new HouseDTO(p.House);
            Age = p.Age;
            Sex = p.Sex;
            Residence = p.Household.Lordship.Name;
            FullNameAndAge = p.FullNameAndAge;
            FullNameAndAgeWithLinks = GetFullNameAndAgeWithLinks(p);
        }
        public Guid Id { get; set; }
        public String Name { get; set; }
        public HouseDTO House { get; set; }
        public int Age { get; set; }
        public Sex Sex { get; set; }
        public String Residence { get; set; }
        public String FullNameAndAge { get; set; }
        public String FullNameAndAgeWithLinks { get; set; }
        public String Relation { get; set; }
    }
}