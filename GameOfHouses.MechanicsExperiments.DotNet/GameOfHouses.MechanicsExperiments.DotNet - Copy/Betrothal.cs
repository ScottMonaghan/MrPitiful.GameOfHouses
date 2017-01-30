using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public class Bethrothal
    {
        public Person HeadOfHouseholdToBe { get; set; }
        public Person SpouseToBe { get; set; }
        public World world { get; set; }
        public int Year { get; set; }
    }
}
