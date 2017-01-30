using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameOfHouses.Web.DTOs
{
    public class HouseDetailsDTO:HouseDTO
    {
        public string Lord { get; set; }
        public string Allegience { get; set; }
        public List<LordshipDTO> Lordships { get; set; }
        public List<string> Vassles { get; set; }
        public List<string> OrderOfSuccession { get; set; }
    }
}