using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameOfHouses.Logic;

namespace GameOfHouses.Web.DTOs
{
    public class HouseholdDTO
    {
        public HouseholdDTO(Household household)
        {
            Id = household.Id;
            Members = household.Members.Select(m => new PersonDTO(m)).ToList();
            HeadOfHousehold = new PersonDTO(household.HeadofHousehold);
            Lordship = new LordshipDTO(household.Lordship);
        }
        public Guid Id { get; set; }
        public List<PersonDTO> Members { get; set; }
        public PersonDTO HeadOfHousehold { get; set; }
        public LordshipDTO Lordship { get; set; }
    }
}