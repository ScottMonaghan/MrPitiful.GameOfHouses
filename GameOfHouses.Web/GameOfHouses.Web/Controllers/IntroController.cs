using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using GameOfHouses.Logic;
using GameOfHouses.Web.DTOs;
using static GameOfHouses.Logic.Utility;

namespace GameOfHouses.Web.Controllers
{

    [RoutePrefix("api/intro")]
    public class IntroController : ApiController
    {
        // GET api/<controller>

        [Route("GetIntroPlayer/{name}/{house}/{sex}")]
        public IntroPlayerDTO Get(string name, string House, Sex sex)
        {
            var Game = (Game)HttpContext.Current.Application["Game"];
            var introPlayerLord =
                Game.Players
                .FirstOrDefault(p => p.PlayerType == PlayerType.Live)
                .House
                .Lord;
            introPlayerLord.Name = name;
            introPlayerLord.House.Name = House;
            introPlayerLord.Sex = sex;

            introPlayerLord.Spouse.Sex = introPlayerLord.Sex == Sex.Male ? Sex.Female : Sex.Male;

            var introPlayerDTO = new IntroPlayerDTO() {
                Id = introPlayerLord.Id,
                Relation = "Self",
                Name = introPlayerLord.Name,
                House = new HouseDTO() { Id = introPlayerLord.House.Id, Name = introPlayerLord.House.Name },
                Age = introPlayerLord.Age,
                Sex = introPlayerLord.Sex,
                FullNameAndAge = introPlayerLord.FullNameAndAge,
                Residence = introPlayerLord.Household.Lordship.Name,
                Father = introPlayerLord.Father != null ? new PersonDTO() {
                    Id = introPlayerLord.Father.Id,
                    Name = introPlayerLord.Father.Name,
                    House = new HouseDTO() { Id = introPlayerLord.House.Id, Name = introPlayerLord.House.Name },
                    Age = introPlayerLord.Age,
                    Sex = introPlayerLord.Father.Sex,
                    FullNameAndAge = introPlayerLord.FullNameAndAge,
                    Residence = null,
                    Relation = "father"
                } : null,
                Heirs = introPlayerLord.GetCurrentHeirs().Select(h => {
                    return new PersonDTO()
                    {
                        House = new HouseDTO() { Id = h.House.Id, Name = h.House.Name },
                        Name = h.Name,
                        Id = h.Id,
                        Age = h.Age,
                        Sex = h.Sex,
                        FullNameAndAge = h.FullNameAndAge,
                        Residence = h.Household.Lordship.Name,
                        Relation = GetDecendentsRelatioinshipToAncestor(introPlayerLord, h)
                    };
                }).ToList()
            };
            
            return introPlayerDTO;            
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
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