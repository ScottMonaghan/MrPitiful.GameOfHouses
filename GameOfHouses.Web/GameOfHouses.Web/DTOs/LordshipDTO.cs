using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameOfHouses.Logic;

namespace GameOfHouses.Web.DTOs
{
    public class LordshipDTO
    {
        public LordshipDTO() { }
        public LordshipDTO(Lordship l)
        {
            Id = l.Id;
            Name = l.Name;
            MapX = l.MapX;
            MapY = l.MapY;
            if (l.Lord != null)
            {
                Lord = new PersonDTO(l.Lord, true);
                Sovreign = new HouseDTO(l.Lord.House.GetSovreign());
            }
            Defenders = l.Defenders.Count();
        }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public PersonDTO Lord { get; set; }
        public HouseDTO Sovreign { get; set; }
        public int Defenders { get; set; }

    }
}