using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GameOfHouses.Logic;
using static GameOfHouses.Web.Utility.Helpers;

namespace GameOfHouses.Web.DTOs
{
    public class PlayerDTO
    {
        public PlayerDTO() { }
        public PlayerDTO(Player p)
        {
            Id = p.Id;
            GameId = p.Game.Id;
            House = p.House!=null?new HouseDTO(p.House):null;
            PlayerType = p.PlayerType;
            Person = p.Person!=null?new PersonDTO(p.Person):null;
        }
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public HouseDTO House { get; set; }
        public PlayerType PlayerType { get; set; }
        public PersonDTO Person {get; set;}
    }
}