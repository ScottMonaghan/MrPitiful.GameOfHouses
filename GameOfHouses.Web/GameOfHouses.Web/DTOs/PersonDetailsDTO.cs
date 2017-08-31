using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameOfHouses.Logic;

namespace GameOfHouses.Web.DTOs
{
    public class BetrothalDTO
    {
        public BetrothalDTO() { }
        public BetrothalDTO(Bethrothal b)
        {
            Year = b.Year;
            HeadOfHouseholdToBe = new PersonDTO(b.HeadOfHouseholdToBe);
            SpouseToBe = new PersonDTO(b.SpouseToBe);
        }
        public int Year { get; set; }
        public PersonDTO HeadOfHouseholdToBe { get; set; }
        public PersonDTO SpouseToBe { get; set; }
    }
    /*public class HouseholdDTO
    {
        public HouseholdDTO() { }
        public HouseholdDTO(Household h)
        {
            Id = h.Id;
            HeadOfHousehold = new PersonDTO(h.HeadofHousehold);
        }
        public Guid Id { get; set; }
        public PersonDTO HeadOfHousehold { get; set; }
    }*/

    public class PersonDetailsDTO
    {
        public Guid Id { get; set; }
        public String Name { get; set; }
        public HouseDTO House { get; set; }
        public int Age { get; set; }
        public Sex Sex { get; set; }
        public LordshipDTO Residence { get; set; }
        public String FullNameAndAge { get; set; }
        public String FullNameAndAgeWithLinks { get; set; }
        public String Relation { get; set; }
        public string People { get; set; }
        public string Class { get; set; }
        public List<LordshipDTO> Lordships { get; set; }
        public PersonDTO Spouse { get; set; }
        public BetrothalDTO Betrothal { get; set; }
        public List<PersonDTO> Children { get; set; }
        public HouseholdDTO Household { get; set; }

    }
}