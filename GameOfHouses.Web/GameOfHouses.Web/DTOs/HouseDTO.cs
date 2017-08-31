using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameOfHouses.Logic;

namespace GameOfHouses.Web.DTOs
{
    public class HouseDTO
    {
        public HouseDTO() { }
        public HouseDTO(House h)
        {
            //var nobleHouses = h.World.NobleHouses;
            Id = h.Id;
            Name = h.Name;
            if (h.Allegience != null) {
                Allegience = new HouseDTO(h.Allegience);
                Sovreign = new HouseDTO(h.GetSovreign());
            }

            /*
            CrestNumber =
                nobleHouses.Contains(h) ?
                nobleHouses.IndexOf(h).ToString("0000")
                : "";
          */
        }
        public HouseDTO Allegience { get; set; }
        public HouseDTO Sovreign { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        //public string CrestNumber { get; set; }
    }

}