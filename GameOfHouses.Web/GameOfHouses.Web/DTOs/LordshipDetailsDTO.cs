using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameOfHouses.Web.DTOs
{
    public class LordshipDetailsDTO : LordshipDTO
    {
        public int FoundingYear { get; set; }
        public PersonDTO Lord { get; set; }
        public List<PersonDTO> OrderOfSuccession { get; set; }
        public int Army { get; set; }
        public List<LordshipDetailsDTO> OccupyingLordsAndArmies { get; set; }
        public int EligibleForConscription { get; set; }
        public LordshipDTO LocationOfLordAndArmy { get; set; }
        public int NobleHouseholds {get;set;}
        public int PeasantHouseholds { get; set; }
        public int Defenders { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
    }
}