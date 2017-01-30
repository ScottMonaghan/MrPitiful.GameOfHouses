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
            var nobleHouses = h.World.NobleHouses;
            Id = h.Id;
            Name = h.Name;
            CrestNumber =
                nobleHouses.Contains(h) ?
                nobleHouses.IndexOf(h).ToString("0000")
                : "";
        }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string CrestNumber { get; set; }
    }

}