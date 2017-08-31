using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
//using System.Web.Http;
using System.Web;
using System.Web.Http;
using GameOfHouses.Logic;
using GameOfHouses.Web.DTOs;
using System.Text;
using static GameOfHouses.Web.Utility.Helpers;


namespace GameOfHouses.Web.Controllers
{
    

    [RoutePrefix("api/player")]
    public class PlayerController : ApiController
    {

        [Route("")]
        public List<PlayerDTO> Get()
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            return game.Players.Select(p => new PlayerDTO(p)).ToList();
        }

        [HttpGet, Route("IncrementYear/{yearsToIncrement}")]
        public void IncrementYear(int yearsToIncrement = 1)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var world = game.World;
            var rnd = game.Random;
            //     while (world.Year< 500)
            //        {
            var playersLeftToTakeTurn = game.Players.Where(p => p.House.Lordships.Count() > 0 && p.PlayerType != PlayerType.Live).ToList();
            while (playersLeftToTakeTurn.Count() > 0)
            {
                var nextPlayer = playersLeftToTakeTurn[rnd.Next(0, playersLeftToTakeTurn.Count())];
                //Console.WriteLine(playersLeftToTakeTurn.Count() + " players remaining.");
                playersLeftToTakeTurn.Remove(nextPlayer);
                nextPlayer.DoPlayerTurn(rnd);
            }
            GameOfHouses.Logic.Utility.IncrementYear(world, rnd, yearsToIncrement);
            //        }
        }
        [HttpGet, Route("NewPlayer/{id:Guid?}")]
        public PlayerDTO NewPlayer(Guid? id = null)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            return new PlayerDTO(game.NewPlayer("", "", Sex.Female, People.Norvik, PlayerType.Live, game.Random, id));
        }

        [Route("{id}")]
        public PlayerDTO Get(Guid id)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var player = game.Players.Where(p => p.Id == id).FirstOrDefault();
            if (player != null)
                return new DTOs.PlayerDTO(player);
            else
            {
                return null;
            }
        }

        [HttpGet, Route("GetPrompt/{id}")]
        public IHttpActionResult GetPrompt(Guid id)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var player = game.Players.Where(p => p.Id == id).FirstOrDefault();
            if (player.NextCommand == null)
            {
                player.NextCommand = new PlayerInputCommand(player);
                player.NextCommand.CommandFunc = (x) =>
                {
                    player.PlayerMenu(player, game.Random);
                };
                player.PlayerMenu(player,game.Random);
            }
            return Ok(new PlayerInputResult() { Output = player.NextCommand.GetPrompt() });
        }
        [HttpGet, Route("HasRightOfPassage/{id}/{mapX}/{mapY}")]
        public Boolean HasRightOfPassage(Guid id, int mapX, int mapY)
        {
            var hasRightOfPassage = false;

            var game = (Game)HttpContext.Current.Application["Game"];
            var player = game.Players.Where(p => p.Id == id).FirstOrDefault();
            var lordship = game.World.Lordships.FirstOrDefault(l => l.MapX == mapX && l.MapY == mapY);
            if (player != null && lordship != null)
            {
                hasRightOfPassage = player.Person.Household.Lordship == lordship || player.House.GetPassibleLordships().Contains(lordship);
            }
            return hasRightOfPassage;
        }

        [HttpGet, Route("GetHousehold/{playerId}")]
        public HouseholdDTO GetHousehold(Guid playerId)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var player = game.Players.Where(p => p.Id == playerId).FirstOrDefault();
            if (player != null)
            {
                return new HouseholdDTO(player.Person.Household);
            }else
            {
                return null;
            }
        }

        [HttpGet, Route("ExecuteCommand/{id}/{input}")]
        public PlayerInputResult ExcecuteCommand(Guid id, string input)
        {
            var game = (Game)HttpContext.Current.Application["Game"];
            var player = game.Players.Where(p => p.Id == id).FirstOrDefault();
            var command = player.NextCommand;
            player.LastCommand = command;
            command.Input = input;
            command.Execute();
            return command.Result;
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