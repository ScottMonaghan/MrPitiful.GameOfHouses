using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameOfHouses.Web.DTOs
{
    public class IntroPlayerDTO : PersonDTO
    {
        public IntroPlayerDTO()
        {
            Heirs = new List<PersonDTO>();
        }
        public PersonDTO Father;
        public List<PersonDTO> Heirs;
    }

}