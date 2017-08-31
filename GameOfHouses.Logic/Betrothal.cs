using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public class Bethrothal
    {
        public Guid Id { get; set; }
        public Person HeadOfHouseholdToBe { get; set; }
        public Person SpouseToBe { get; set; }
        public World world { get; set; }
        public int Year { get; set; }
        public Bethrothal Flatten()
        {
            return new Bethrothal()
            {
                Id = Id,
                HeadOfHouseholdToBe = new Person(new Random()) { Id = HeadOfHouseholdToBe.Id },
                SpouseToBe = new Person(new Random()) { Id = SpouseToBe.Id },
                world = new World(new Random()) { Id = world.Id },
                Year = Year
            };
        }
    }
}
