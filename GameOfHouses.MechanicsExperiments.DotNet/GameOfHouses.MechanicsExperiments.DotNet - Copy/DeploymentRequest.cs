using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public class DeploymentRequest
    {
        public House RequestingHouse { get; set; }
        public Lordship RequestedArmy { get; set; }
        public Lordship Destination { get; set; }
        public int NumberOfTroops { get; set; }
    }
}
