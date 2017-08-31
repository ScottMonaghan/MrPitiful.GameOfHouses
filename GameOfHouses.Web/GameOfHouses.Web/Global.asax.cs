using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using GameOfHouses.Logic;
using static GameOfHouses.Logic.Utility;
namespace GameOfHouses.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //create in-memory placeholder for game for now
            var rnd = new Random();
            Application["Game"] = InitializeWorld(new World(rnd), rnd);
            var game = (Game)Application["Game"];
            //game.NewPlayer("Scott", "Monaghan", Sex.Male, People.Norvik, PlayerType.Live, rnd);
            //game.NewPlayer("Emily", "Andersson", Sex.Female, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Michael", "Corley", Sex.Male, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Joel", "LeFave", Sex.Male, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Rachel", "Mulready", Sex.Female, People.Norvik, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Tim", "Randall", Sex.Male, People.Sulthaen, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Tom", "Brady", Sex.Male, People.Sulthaen, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Gisele", "Bundchen", Sex.Female, People.Sulthaen, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Robert", "Kraft", Sex.Male, People.Sulthaen, PlayerType.AIAggressive, rnd);
            //game.NewPlayer("Susan", "Mars", Sex.Female, People.Sulthaen, PlayerType.AIAggressive, rnd);

        }
    }
}
