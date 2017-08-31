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
    

    [RoutePrefix("api/lordship")]
    public class LordshipController : ApiController
    {
        // GET api/<controller>

        [Route("GetLordshipDetails/{id}")]
        public LordshipDetailsDTO GetLordshipDetails(Guid id)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var lordship = game.Players[0].House.World.Lordships.SingleOrDefault(l => l.Id == id);
            if (lordship != null)
            {
                return new LordshipDetailsDTO() {
                    Army = lordship.Army.Count(p => p.IsAlive),
                    Defenders = lordship.Defenders.Count(),
                    //EligibleForConscription = lordship.EligibleForConscription.Count(),
                    FoundingYear = lordship.FoundingYear,
                    Id = lordship.Id,
                    LocationOfLordAndArmy = new LordshipDTO() { Id = lordship.LocationOfLordAndArmy.Id, Name = lordship.LocationOfLordAndArmy.Name },
                    Lord = new PersonDTO() {
                        Id = lordship.Lord.Id,
                        Name = lordship.Lord.Name,
                        FullNameAndAge = lordship.Lord.FullNameAndAge,
                        //FullNameAndAgeWithLinks = GetFullNameAndAgeWithLinks(lordship.Lord),
                        House = new HouseDTO()
                        {
                            Id = lordship.Lord.House.Id,
                            Name = lordship.Lord.House.Name,
                            //CrestNumber = lordship.World.NobleHouses.IndexOf(lordship.Lord.House).ToString("0000")
                        }
                    },
                    MapX = lordship.MapX,
                    MapY = lordship.MapY,
                    Name = lordship.Name,
                    NobleHouseholds = lordship.Households.Count(h => h.HeadofHousehold.Class == SocialClass.Noble),
                    OccupyingLordsAndArmies = lordship.OccupyingLordsAndArmies.Select(
                        army =>
                        new LordshipDetailsDTO()
                        {
                            Id = army.Id,
                            Name = army.Name,
                            Army = army.Army.Count(p => p.IsAlive)
                        }
                        ).ToList(),
                    OrderOfSuccession = lordship.GetOrderOfSuccession(10).Select(
                         p=>
                         new PersonDTO()
                         {
                             Id = p.Id,
                             Name = p.Name,
                             FullNameAndAge = p.FullNameAndAge,
                             //FullNameAndAgeWithLinks = GetFullNameAndAgeWithLinks(p)
                         }
                        ).ToList(),
                     PeasantHouseholds = lordship.Households.Count(h=>h.HeadofHousehold.Class == SocialClass.Peasant)
                };
            }   else
            {
                return null;
            }            
        }

        [HttpGet]
        public List<LordshipDTO> Get()
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            return game.World.Lordships.Select(l => new LordshipDTO(l)).ToList();
        }
        // GET api/<controller>/5
        [HttpGet, Route("{id}")]
        public LordshipDTO Get(Guid id)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var lordship = game.World.Lordships.FirstOrDefault(l => l.Id == id);
            if (lordship != null)
            {
                return new LordshipDTO(lordship);
            } else
            {
                return null;
            }
        }

        [HttpGet, Route("GetByCoordinates/{mapX}/{mapY}")]
        public LordshipDTO GetByCoordinates(int mapX, int mapY)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var lordship = game.World.Lordships.FirstOrDefault(l => l.MapX == mapX && l.MapY==mapY);
            if (lordship != null)
            {
                return new LordshipDTO(lordship);
            }
            else
            {
                return null;
            }
        }

        [HttpGet, Route("GetHouseholds/{id}")]
        public List<HouseholdDTO> GetHouseholds(Guid id)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var lordship = game.World.Lordships.FirstOrDefault(l=>l.Id == id);
            if (lordship != null)
            {
                return lordship.Households.Select(h=>new HouseholdDTO(h)).ToList();
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