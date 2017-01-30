using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.MechanicsExperiments.DotNet
{
    public static class Utility
    {
        public static string GetName(Sex sex, Random rnd)
        {
            var maleNames = Constants.MALE_NAMES.Split(',');
            var femaleNames = Constants.FEMALE_NAMES.Split(',');
            if (sex == Sex.Male)
            {
                return maleNames[rnd.Next(0, maleNames.Count())];
            }
            else
            {
                return femaleNames[rnd.Next(0, femaleNames.Count())];
            }
        }
    }
}
