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
        }
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}