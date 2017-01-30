using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using GameOfHouses.Logic;
using GameOfHouses.Web.DTOs;
using static GameOfHouses.Web.Utility.Helpers;

namespace GameOfHouses.Web.Controllers
{
    

    [RoutePrefix("api/house")]
    public class HouseController : ApiController
    {
        // GET api/<controller>

        [Route("GetHouseDetails/{id}")]
        public HouseDetailsDTO GetHouseDetails(Guid id)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var house = game.Players[0].House.World.NobleHouses.SingleOrDefault(x => x.Id == id);
            var h = new HouseDetailsDTO();
            if (h != null)
            {
                h.Id = house.Id;
                h.Name = house.Name;
                h.CrestNumber = house.World.NobleHouses.IndexOf(house).ToString("0000");
                h.Lord = GetFullNameAndAgeWithLinks(house.Lord);
                h.Allegience = (house.Allegience != null) ? house.Allegience.Name : "Sovreign";
                h.Lordships = house.Lordships.OrderBy(name => name).Select(l => new LordshipDTO() { Id = l.Id, Name = l.Name }).ToList();
                h.Vassles = house.Vassles.Select(v => v.Name).OrderBy(name => name).ToList();
                h.OrderOfSuccession = house.GetOrderOfSuccession(10).Select(p => GetFullNameAndAgeWithLinks(p)).ToList();
                return h;                
            }   else
            {
                return null;
            }            
        }

        // GET api/<controller>/5
        public HouseDTO Get(Guid id)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var house = game.Players[0].House.World.NobleHouses.SingleOrDefault(h => h.Id == id);
            if (house != null)
            {
                return new HouseDTO()
                {
                    Id = house.Id,
                    Name = house.Name,
                    CrestNumber = house.World.NobleHouses.IndexOf(house).ToString("0000")
                };
            }
            else
            {
                return null;
            }
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}