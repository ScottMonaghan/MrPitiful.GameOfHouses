using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public enum ProposalType
    {
        Proposal = 0,
        Invitation = 1
    }
    public enum Sex
    {
        Female = 0,
        Male = 1
    }
    public enum Profession
    {
        Dependant = 0,
        Peasant = 1,
        Noble = 2,
        Soldier = 3
    }
    public enum SocialClass
    {
        Noble = 0,
        Peasant = 1
    }
    public enum People
    {
        Sulthaen = 0,
        Norvik = 1,
        Kyltcled = 2
    }
    public enum PlayerType
    {
        Live = 0,
        AIAggressive = 1,
        AISubmissive = 2
    }
}
