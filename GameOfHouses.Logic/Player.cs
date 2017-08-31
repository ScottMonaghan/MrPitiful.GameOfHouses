using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameOfHouses.Logic.Utility;
using System.Text.RegularExpressions;

namespace GameOfHouses.Logic
{
    public class PlayerInputResult
    {
        public string Output { get; set; }
    }

    public delegate void PlayerInputCommandFunc(string input);
    public delegate string PlayerInputCommandPromptFunc();

    public class PlayerInputCommand
    {
        public PlayerInputCommand(Player player)
        {
            Player = player;
            Input = "";
            //CallingCommand = null;
            CommandFunc = null;
            Result = new PlayerInputResult();
        }
        public Player Player { get; set; }
        public string Input { get; set; }
        //public PlayerInputCommand CallingCommand { get; set; }
        public PlayerInputCommandFunc CommandFunc { get; set; }
        public PlayerInputCommandPromptFunc PromptFunc { get; set; }
        public string GetPrompt() { return PromptFunc(); }
        public PlayerInputResult Result {get; set;}
        public void Execute()
        {
            //clear result
            Result = new PlayerInputResult();
            //execute function
            CommandFunc(Input);
        }
    }

    public class Player
    {
        public Player()
        {
            House = null;
            VasslesSummonedThisTurn = new List<House>();
            HouseLordshipsSummonedThisTurn = new List<Lordship>();
            AttackHistory = new List<Lordship>();
            PlayerType = PlayerType.AISubmissive;
            Id = Guid.NewGuid();
        }
        public Person Person {
            get {
                if (House != null && House.Lord != null)
                {
                    return House.Lord;
                } else
                {
                    return null;
                }
            }
        }
        public PlayerInputCommand LastCommand { get; set; }
        public PlayerInputCommand NextCommand { get; set; }
        public List<House> VasslesSummonedThisTurn { get; set; }
        public List<Lordship> HouseLordshipsSummonedThisTurn { get; set; }
        public bool ResettlementThisTurn { get; set; }
        public Guid Id { get; set; }
        public Game Game { get; set; }
        public House House { get; set; }
        public Delegate CurrentMenu { get; set; }
        public void SettleNewLordship(Lordship sourceLordship, Lordship targetLordship, Household lordsHouseHold, List<Household> peasantHouseholds)
        {
            if (sourceLordship.PlayerMoves.Count(p => p == this) < Constants.ALLOWED_MOVES_PER_YEAR)
            {
                Lordship.PopulateLordship(targetLordship, lordsHouseHold, peasantHouseholds);
                sourceLordship.PlayerMoves.Add(this);
            }
        }
        public PlayerType PlayerType { get; set; }
        public void DoPlayerTurn(Random rnd)
        {
            //VasslesSummonedThisTurn.Clear();
            //HouseLordshipsSummonedThisTurn.Clear();
            //ResettlementThisTurn = false;
            switch (PlayerType)
            {
                case PlayerType.Live:
                    DoLivePlayerTurn(rnd);
                    break;
                case PlayerType.AIAggressive:
                    DoAggressivePlayerTurn(rnd);
                    break;
                case PlayerType.AISubmissive:
                    DoSubmissivePlayerTurn(rnd);
                    break;
            }
            //House.History = "";
        }
        public void IncrementYear()
        {
            VasslesSummonedThisTurn.Clear();
            HouseLordshipsSummonedThisTurn.Clear();
            ResettlementThisTurn = false;
        }
        private void makeAndAcceptOffers()
        {
            if (!House.Vacant)
            {
                var people = House.Lord.People;
                var world = House.World;
                var nobleSubjects = House.Lordships.SelectMany(l => l.Households).SelectMany(h => h.Members).Where(m => m.Class == SocialClass.Noble).ToList();
                //receive response on all accepted or rejected proposals
                world.Proposals.Where(p => p.Sender == House && (p.Status == ProposalStatus.Accepted || p.Status == ProposalStatus.Rejected)).ToList().ForEach(
                    p => p.Status = ProposalStatus.ResponseReceived
                );
                //accept all proposals
                var incomingProposals = world.Proposals.Where(p => p.Receiver == House && p.Status == ProposalStatus.New).ToList();
                foreach (var proposal in incomingProposals)
                {
                    //only people for now
                    if (nobleSubjects.Intersect(proposal.RequestedPeople).Count() == proposal.RequestedPeople.Count())
                    {
                        proposal.Accept();
                    }
                    else
                    {
                        proposal.Reject();
                    }
                }
                //now see if you have any heirs to betroth
                var heirsLeftToBetroth = House.Lord.GetCurrentHeirs()
                    .Where(heir => heir.IsEligableForBetrothal() 
                    && heir.Household.Lordship != null && heir.Household.Lordship.Lord.House == House).ToList();
                if (House.Lord.IsEligableForBetrothal())
                {
                    //don't let the lord abdicate for love!
                    heirsLeftToBetroth.Add(House.Lord);
                }
                
                //first see if you have anyone compatible in your subjects
                var remainingEligibleNobleSubjects = House.Lordships
                    .SelectMany(l => l.Households)
                    .SelectMany(h => h.Members)
                    .Where(
                        m =>
                        m.Class == SocialClass.Noble
                        && m.People == people
                        && !heirsLeftToBetroth.Contains(m)
                        && m.IsEligableForBetrothal()
                        ).ToList();

                var unmatchedHeirs = new List<Person>();
                foreach (var heir in heirsLeftToBetroth)
                {
                    var spouseToBe = remainingEligibleNobleSubjects.Where(s => s.IsCompatibleForBetrothal(heir)).FirstOrDefault();
                    if (spouseToBe != null)
                    {
                        world.CreateBethrothal(heir, spouseToBe, world.Year);
                        remainingEligibleNobleSubjects.Remove(spouseToBe);
                    }
                    else
                    {
                        unmatchedHeirs.Add(heir);
                    }
                }
                //now see what we can get on the market for any unmatched heirs
                foreach (var heir in unmatchedHeirs)
                {

                    if (remainingEligibleNobleSubjects.Count() > 0)
                    {
                        var possibleSpouses = world.EligibleNobles.Where(s =>
                            //make sure there are no open proposals
                            !world.Proposals.SelectMany(p => p.OfferedPeople).Union(world.Proposals.SelectMany(p => p.RequestedPeople)).Contains(s)
                            && s.IsEligableForBetrothal()
                            && s.IsCompatibleForBetrothal(heir)
                            ).Take(20).ToList();

                        if (possibleSpouses.Count() > 0)
                        {
                            Person proposedSpouse = null;
                            possibleSpouses.Min(s2 => {
                                proposedSpouse = s2;
                                return Math.Abs(s2.Age - heir.Age);
                            });
                            if (proposedSpouse != null && proposedSpouse.Household.Lordship != null && proposedSpouse.Household.Lordship.Lord != null)
                            {
                                world.Proposals.Add(
                                    new Proposal()
                                    {
                                        Sender = House,
                                        Receiver = proposedSpouse.Household.Lordship.Lord.House,
                                        RequestedPeople = new List<Person>() { proposedSpouse }
                                    }
                                );
                            }
                        }
                    }
                }
                world.EligibleNobles = world.EligibleNobles.Union(remainingEligibleNobleSubjects).ToList();
            }

        }
        public void DoSubmissivePlayerTurn(Random rnd)
        {
            makeAndAcceptOffers();
        }
        public void DoAggressivePlayerTurn(Random rnd)
        {
            makeAndAcceptOffers();
            //attack
            if (!House.Vacant && House.Lord.Household!=null  && House.Lord.Household.Lordship !=null && House.Lord.Household.Lordship.Lord != null && House.Lord.Household.Lordship.Lord.House == House /*&& House.LordshipCandidates.Count() > 0*/)
            {

                var commandingLordship = House.Lord.Household.Lordship;
                var attackers = new List<Lordship>();
                attackers.AddRange(House.Lordships);
                HouseLordshipsSummonedThisTurn.AddRange(House.Lordships);
                attackers.AddRange(House.GetAllSubVassles().SelectMany(v => v.Lordships));
                VasslesSummonedThisTurn = House.Vassles.ToList();
                var attackableLordships = commandingLordship.GetAttackableLordships();
                if (attackableLordships.Count() > 0)
                {
                   var allies = commandingLordship.GetAllies();
                   var distancesToAttackableLordships = commandingLordship.GetShortestAvailableDistanceToLordship(allies.Union(attackableLordships).ToList());
                    var target =
                        attackableLordships
                        //don't attack anything you've attacked in the last 10 turns
                        .Where(t =>
                            !AttackHistory.Skip(Math.Max(0, AttackHistory.Count() - 10)).Contains(t)
                           && t.Lord!=null 
                            && t.Lord.House.Allegience == null
                            && t.Defenders.Count() < 0.66 * attackers.SelectMany(a => a.Army.Where(s => s.IsAlive)).Count()
                            )
                        .OrderBy(t => distancesToAttackableLordships[t])
                        .FirstOrDefault();

                    if (target == null)
                    {
                        //find the closest conquorable target to a capital
                        var conquorableLordships = attackableLordships.Where(t =>
                            !AttackHistory.Skip(Math.Max(0, AttackHistory.Count() - 10)).Contains(t)
                            //&& t.Lord.House.Allegience == null
                            && t.Defenders.Count() < 0.33 * attackers.SelectMany(a => a.Army.Where(s => s.IsAlive)).Count()
                        ).ToList();
                        if (conquorableLordships.Count() > 0)
                        {
                            var capitals = House.World.NobleHouses.Where(h => h.Seat!=null && h.Allegience == null && h.Lordships.Count() > 0).Select(h => h.Seat).ToList();
                            var closestDistance = double.PositiveInfinity;
                            target = conquorableLordships[0];

                            foreach (var lordship in conquorableLordships)
                            {
                                foreach (var capital in capitals)
                                {
                                    var distance = Math.Sqrt(Math.Pow(capital.MapX - lordship.MapX, 2) + Math.Pow(capital.MapY - lordship.MapY, 2));
                                    if (distance < closestDistance)
                                    {
                                        closestDistance = distance;
                                        target = lordship;
                                    }
                                }
                            }
                        }
                    }
                    if (target != null)
                    {
                        AttackHistory.Add(target);
                        Attack(this,commandingLordship, attackers, target,null,null, rnd, false,false,true,1,false);
                        while (NextCommand != null)
                        {
                            NextCommand.GetPrompt();
                            NextCommand.Execute();
                        }
                    }
                }
            }
        }
        public void ProposalMenu(Player player, Proposal proposal, PlayerInputCommand returnMenu, Random rnd)
        {
            var ProposalCommand = new PlayerInputCommand(player);
            player.NextCommand = ProposalCommand;
            ProposalCommand.PromptFunc = () =>{
                var prompt = "";
                prompt += proposal.GetDeatailsAsString() + "\n";
                prompt += "[F]ealty, [P]eople, [M]oney, [L]and, M[e]ssage, [S]ubmit, [C]ancel\n";
                return prompt;
            };
            ProposalCommand.CommandFunc = (proposalInput) =>
            {
                switch (proposalInput.ToLower())
                {
                    case "c":
                        player.NextCommand = returnMenu;
                        break;
                    case "f":
                        ProposalFealtyMenu(player, proposal, ProposalCommand, rnd);
                        break;
                    case "p":
                        ProposalPeopleMenu(player, proposal, ProposalCommand, rnd);
                        break;
                    case "m":
                        ProposalMoneyMenu(player, proposal, ProposalCommand, rnd);
                        break;
                    case "l":
                        ProposalLordshipMenu(player, proposal, ProposalCommand, rnd);
                        break;
                    case "e": //M[e]ssage
                        ProposalMessageMenu(player, proposal, ProposalCommand, rnd);
                        break;
                    case "s":
                        if (proposal.Type == ProposalType.Proposal)
                        {
                            player.Game.World.Proposals.Add(proposal);
                            ProposalCommand.Result.Output += "Proposal Submitted!\n";
                            player.NextCommand = returnMenu;
                        } else
                        {
                            //need invitation code
                            var invitationCodeCommand = new PlayerInputCommand(player);
                            player.NextCommand = invitationCodeCommand;
                            invitationCodeCommand.PromptFunc = () => {
                                var prompt = "";
                                prompt += "Enter the secret words that only your invitee may use to accept this invitation:\n";
                                return prompt;
                            };
                            invitationCodeCommand.CommandFunc = (input) =>
                            {
                                if (input != "")
                                {
                                    proposal.InvitationCode = input.ToLower().Trim();
                                    player.Game.World.Proposals.Add(proposal);
                                    ProposalCommand.Result.Output += "Proposal Submitted!\n";
                                    player.NextCommand = returnMenu;
                                }
                            };
                        }
                        break;

                }
            };
        }
        public void ProposalFealtyMenu(Player player, Proposal proposal, PlayerInputCommand returnMenu, Random rnd){
            var proposalFealtyCommand = new PlayerInputCommand(player);
            player.NextCommand = proposalFealtyCommand;
            proposalFealtyCommand.PromptFunc = () =>
            {
                var prompt = "";
                if (proposal.OfferedFealty)
                {
                    prompt += "Rescind [O]ffer of fealty, ";
                } else
                {
                    prompt += "Add [O]ffer of fealty, ";
                }
                if (proposal.RequestedFealty)
                {
                    prompt += "Rescind [R]equest of fealty ";
                } else
                {
                    prompt += "Add [R]equest of fealty, ";
                }
                prompt += "E[x]it \n";
                return prompt;
            };
            proposalFealtyCommand.CommandFunc = (input) =>
            {
                switch (input.ToLower())
                {
                    case "x":
                        player.NextCommand = returnMenu;
                        break;
                    case "o":
                        if (proposal.OfferedFealty)
                        {
                            proposal.OfferedFealty = false;
                        }
                        else
                        {
                            proposal.OfferedFealty = true;
                        }
                        player.NextCommand = returnMenu;
                        break;
                    case "r":
                        if (proposal.RequestedFealty)
                        {
                            proposal.RequestedFealty = false;
                        }
                        else { 
                            proposal.RequestedFealty = true;
                        }
                        player.NextCommand = returnMenu;
                        break;
                }
            };
        }
        public string PagedPersonSelectionPrompt(List<Person> persons, int page = 1, int pagelength = -1)
        {
            var prompt = "";
            if(pagelength == -1)
            {
                pagelength = persons.Count();
            }
           // var pagecount = Math.Ceiling((decimal)persons.Count() / pagelength);
            var startingIndex = (page - 1) * pagelength;
            var endingIndex = startingIndex + pagelength -1;
            if (startingIndex < persons.Count())
            {
                for (int i = startingIndex; i<=endingIndex && i<persons.Count(); i++)
                {
                    prompt += string.Format("{0}. {1}\n", i + 1, persons[i].FullNameAndAge);
                }
            }
            prompt += "Select a person, [N]ext page, [P]rev page, E[x]it\n";
            return prompt;
        }
        public void ProposalPeopleMenu(Player player, Proposal proposal, PlayerInputCommand returnMenu, Random rnd)
        {
            var proposalPeopleCommand = new PlayerInputCommand(player);
            player.NextCommand = proposalPeopleCommand;
            proposalPeopleCommand.PromptFunc = () =>
            {
                var prompt = "";
                prompt += "Update [O]ffered People"
                //only allow requested people for proposals, not invitations
                +((proposal.Type==ProposalType.Proposal)?", Update [R]equested People":"")
                +", E[x]it\n";
                return prompt;
            };
            proposalPeopleCommand.CommandFunc = (input) =>
            {
                switch (input.ToLower())
                {
                    case "x":
                        player.NextCommand = returnMenu;
                        break;
                    case "o":
                        {
                            var updateOfferedPeopleCommand = new PlayerInputCommand(player);
                            player.NextCommand = updateOfferedPeopleCommand;
                            updateOfferedPeopleCommand.PromptFunc = () =>
                            {
                                var prompt = "";
                                prompt += "[A]dd Offered People, [R]emove Offered People, E[x]it";
                                return prompt;
                            };
                            updateOfferedPeopleCommand.CommandFunc = (offeredLordshipsInput) =>
                            {
                                switch (offeredLordshipsInput.ToLower())
                                {
                                    case "x":
                                        player.NextCommand = returnMenu;
                                        break;
                                    case "a":
                                        {
                                        //add offered people
                                        var persons = House.Lordships
                                                .SelectMany(l => l.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Noble))
                                                .SelectMany(h => h.Members)
                                                .Except(proposal.OfferedPeople)
                                                .OrderBy(p => p.House.Name).ThenBy(p => p.Name)
                                                .ToList()
                                                ;

                                            var personSelectionCommand = new PlayerInputCommand(player);
                                            var page = 1;
                                            var pagelength = 10;
                                            player.NextCommand = personSelectionCommand;
                                            personSelectionCommand.PromptFunc = () =>
                                            {
                                                return PagedPersonSelectionPrompt(persons, page, pagelength);
                                            };
                                            personSelectionCommand.CommandFunc = (personSelectionInput) =>
                                            {
                                                switch (personSelectionInput.ToLower())
                                                {
                                                    case "x":
                                                        player.NextCommand = returnMenu;
                                                        break;
                                                    case "n":
                                                        if (page * pagelength <= persons.Count)
                                                        {
                                                            page++;
                                                        }
                                                        break;
                                                    case "p":
                                                        if ((page - 1) > 0)
                                                        {
                                                            page--;
                                                        }
                                                        break;
                                                    default:
                                                        var inputNumber = -1;
                                                        if (int.TryParse(personSelectionInput, out inputNumber) && inputNumber > 0 && inputNumber <= persons.Count)
                                                        {
                                                            proposal.OfferedPeople.Add(persons[inputNumber - 1]);
                                                            player.NextCommand = returnMenu;
                                                        }
                                                        break;
                                                }
                                            };
                                        }
                                        break;
                                    case "r":
                                        {
                                        //remove offered people
                                        var persons = proposal.OfferedPeople.ToList();

                                            var personSelectionCommand = new PlayerInputCommand(player);
                                            var page = 1;
                                            var pagelength = 10;
                                            player.NextCommand = personSelectionCommand;
                                            personSelectionCommand.PromptFunc = () =>
                                            {
                                                return PagedPersonSelectionPrompt(persons, page, pagelength);
                                            };
                                            personSelectionCommand.CommandFunc = (personSelectionInput) =>
                                        {
                                            switch (personSelectionInput.ToLower())
                                            {
                                                case "x":
                                                    player.NextCommand = returnMenu;
                                                    break;
                                                case "n":
                                                    if (page * pagelength <= persons.Count)
                                                    {
                                                        page++;
                                                    }
                                                    break;
                                                case "p":
                                                    if ((page - 1) > 0)
                                                    {
                                                        page--;
                                                    }
                                                    break;
                                                default:
                                                    var inputNumber = -1;
                                                    if (int.TryParse(personSelectionInput, out inputNumber) && inputNumber > 0 && inputNumber <= persons.Count)
                                                    {
                                                        proposal.OfferedPeople.Remove(persons[inputNumber - 1]);
                                                        player.NextCommand = returnMenu;
                                                    }
                                                    break;
                                            }
                                        };
                                        }
                                        break;
                                }
                            };
                        }
                        break;
                    case "r":
                        {
                            if (proposal.Type == ProposalType.Proposal) { 
                                var updateRequestedPeopleCommand = new PlayerInputCommand(player);
                                player.NextCommand = updateRequestedPeopleCommand;
                                updateRequestedPeopleCommand.PromptFunc = () =>
                                {
                                    var prompt = "";
                                    prompt += "[A]dd Requested People, [R]emove Requested People, E[x]it";
                                    return prompt;
                                };
                                updateRequestedPeopleCommand.CommandFunc = (offeredLordshipsInput) =>
                                {
                                    switch (offeredLordshipsInput.ToLower())
                                    {
                                        case "x":
                                            player.NextCommand = returnMenu;
                                            break;
                                        case "a":
                                            {
                                                //add requested people
                                                var persons = proposal.Receiver.Lordships
                                                        .SelectMany(l => l.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Noble))
                                                        .SelectMany(h => h.Members)
                                                        .Except(proposal.OfferedPeople)
                                                        .OrderBy(p => p.House.Name).ThenBy(p => p.Name)
                                                        .ToList()
                                                        ;

                                                var personSelectionCommand = new PlayerInputCommand(player);
                                                var page = 1;
                                                var pagelength = 10;
                                                player.NextCommand = personSelectionCommand;
                                                personSelectionCommand.PromptFunc = () =>
                                                {
                                                    return PagedPersonSelectionPrompt(persons, page, pagelength);
                                                };
                                                personSelectionCommand.CommandFunc = (personSelectionInput) =>
                                                {
                                                    switch (personSelectionInput.ToLower())
                                                    {
                                                        case "x":
                                                            player.NextCommand = returnMenu;
                                                            break;
                                                        case "n":
                                                            if (page * pagelength <= persons.Count)
                                                            {
                                                                page++;
                                                            }
                                                            break;
                                                        case "p":
                                                            if ((page - 1) > 0)
                                                            {
                                                                page--;
                                                            }
                                                            break;
                                                        default:
                                                            var inputNumber = -1;
                                                            if (int.TryParse(personSelectionInput, out inputNumber) && inputNumber > 0 && inputNumber <= persons.Count)
                                                            {
                                                                proposal.RequestedPeople.Add(persons[inputNumber - 1]);
                                                                player.NextCommand = returnMenu;
                                                            }
                                                            break;
                                                    }
                                                };
                                            }
                                            break;
                                        case "r":
                                            {
                                                //remove requested people
                                                var persons = proposal.RequestedPeople.ToList();

                                                var personSelectionCommand = new PlayerInputCommand(player);
                                                var page = 1;
                                                var pagelength = 10;
                                                player.NextCommand = personSelectionCommand;
                                                personSelectionCommand.PromptFunc = () =>
                                                {
                                                    return PagedPersonSelectionPrompt(persons, page, pagelength);
                                                };
                                                personSelectionCommand.CommandFunc = (personSelectionInput) =>
                                                {
                                                    switch (personSelectionInput.ToLower())
                                                    {
                                                        case "x":
                                                            player.NextCommand = returnMenu;
                                                            break;
                                                        case "n":
                                                            if (page * pagelength <= persons.Count)
                                                            {
                                                                page++;
                                                            }
                                                            break;
                                                        case "p":
                                                            if ((page - 1) > 0)
                                                            {
                                                                page--;
                                                            }
                                                            break;
                                                        default:
                                                            var inputNumber = -1;
                                                            if (int.TryParse(personSelectionInput, out inputNumber) && inputNumber > 0 && inputNumber <= persons.Count)
                                                            {
                                                                proposal.RequestedPeople.Remove(persons[inputNumber - 1]);
                                                                player.NextCommand = returnMenu;
                                                            }
                                                            break;
                                                    }
                                                };
                                            }
                                            break;
                                    }
                                };
                            }
                        }
                        break;
                }
            };
        }
        public void ProposalMoneyMenu(Player player, Proposal proposal, PlayerInputCommand returnMenu, Random rnd)
        {
            var proposalMoneyCommand = new PlayerInputCommand(player);
            player.NextCommand = proposalMoneyCommand;
            proposalMoneyCommand.PromptFunc = () =>
            {
                var prompt = "";
                prompt += "[O]ffer Money, [R]equest Money, E[x]it\n";                 
                return prompt;
            };
            proposalMoneyCommand.CommandFunc = (input) =>
            {
                var amount = -1;
                var moneyOffered = false;
                var moneyRequested = false;

                switch (input.ToLower())
                {
                    case "o":
                        moneyOffered = true;
                        break;
                    case "r":
                        moneyRequested = true;
                        break;
                    case "x":
                        player.NextCommand = returnMenu;
                        break;    
                }

                if (moneyOffered || moneyRequested)
                {
                    var setMoneyAmountCommand = new PlayerInputCommand(player);
                    player.NextCommand = setMoneyAmountCommand;
                    setMoneyAmountCommand.PromptFunc = () =>
                    {
                        var prompt = "";
                        prompt += "Enter an amount of money to " + (moneyOffered ? "offer" : "request") + " or E[x]it\n";
                        return prompt;
                    };
                    setMoneyAmountCommand.CommandFunc = (amountInput) =>
                    {
                        if (amountInput.ToLower() == "x")
                        {
                            player.NextCommand = returnMenu;    
                        } else if (int.TryParse(amountInput,out amount))
                        {
                            if (moneyOffered)
                            {
                                if(proposal.Sender.Wealth >= amount)
                                {
                                    proposal.OfferedMoney = amount;
                                    proposal.RequestedMoney = 0;
                                    player.NextCommand = returnMenu;
                                } else
                                {
                                    setMoneyAmountCommand.Result.Output += "Insufficient Funds.\n";
                                }
                            } else //money requested
                            {
                                proposal.RequestedMoney = amount;
                                proposal.OfferedMoney = 0;
                                player.NextCommand = returnMenu;
                            }
                        }
                    };
                }
            };
        }
        public void ProposalMessageMenu(Player player, Proposal proposal, PlayerInputCommand returnMenu, Random rnd)
        {
            var proposalMessageCommand = new PlayerInputCommand(player);
            player.NextCommand = proposalMessageCommand;
            proposalMessageCommand.PromptFunc = () =>
            {
                var prompt = "";
                prompt += "Enter Message:\n";
                return prompt;
            };
            proposalMessageCommand.CommandFunc = (input) =>
            {
                proposal.Message = input;
                player.NextCommand = returnMenu;
            };
        }
        public string GetLordshipSelectionPrompt(List<Lordship> lordships)
        {
            var prompt = "Select a lorship by number or e[x]it.\n";
            for (var i = 0; i < lordships.Count; i++)
            {
                prompt += (
                    string.Format(
                        "{0}. {1}({2},{3}) - {4} fighters\n",
                        i + 1,
                        lordships[i].Name,
                        lordships[i].MapX,
                        lordships[i].MapY,
                        lordships[i].Army.Count()
                        )
                    );
            }
            return prompt;
        }
        public Lordship LordshipSelectionMenu(Player player, List<Lordship> lordships, PlayerInputCommand returnMenu, Random rnd)
        {
            Lordship selectedLordship = null;
            var lordshipSelectionCommand = new PlayerInputCommand(player);
            player.NextCommand = lordshipSelectionCommand;
            lordships = lordships.OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
            lordshipSelectionCommand.PromptFunc = () =>
            {
                var prompt = "Select a lorship by number or e[x]it.\n";
                for (var i = 0; i < lordships.Count; i++)
                {
                    prompt += (
                        string.Format(
                            "{0}. {1}({2},{3}) - {4} fighters\n",
                            i + 1,
                            lordships[i].Name,
                            lordships[i].MapX,
                            lordships[i].MapY,
                            lordships[i].Army.Count()
                            )
                        );
                }
                return prompt;
            };
            lordshipSelectionCommand.CommandFunc = (lordshipSelectionCommandInput) =>
            {
                var selectedLordshipNumber = -1;
                int.TryParse(lordshipSelectionCommandInput, out selectedLordshipNumber);
                if (lordshipSelectionCommandInput.ToLower() == "x")
                {
                    player.NextCommand = returnMenu;
                }
                if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= lordships.Count)
                {
                    selectedLordship = lordships[selectedLordshipNumber - 1];
                }
            };

            return selectedLordship;
        }
        public void ProposalLordshipMenu(Player player, Proposal proposal, PlayerInputCommand returnMenu, Random rnd)
        {
            var proposalLordshipCommand = new PlayerInputCommand(player);
            player.NextCommand = proposalLordshipCommand;
            proposalLordshipCommand.PromptFunc = () =>
            {
                var prompt = "";
                prompt += "Update [O]ffered Lordships"
                //only allow requested lordships for proposals, not invitations
                + ((proposal.Type == ProposalType.Proposal) ? ", Update [R]equested Lordships":"")
                +", E[x]it\n";
                return prompt;
            };
            proposalLordshipCommand.CommandFunc = (input) =>
            {
                switch (input.ToLower()) {
                    case "x":
                        player.NextCommand = returnMenu;
                        break;
                    case "o":
                        var updateOfferedLordshipsCommand = new PlayerInputCommand(player);
                        player.NextCommand = updateOfferedLordshipsCommand;
                        updateOfferedLordshipsCommand.PromptFunc = () =>
                        {
                            var prompt = "";
                            prompt += "[A]dd Offered Lordships, [R]emove Offered Lordships, E[x]it";
                            return prompt;
                        };
                        updateOfferedLordshipsCommand.CommandFunc = (offeredLordshipsInput) =>
                        {
                            switch (offeredLordshipsInput.ToLower())
                            {
                                case "x":
                                    player.NextCommand = returnMenu;
                                    break;
                                case "a":
                                    {
                                        //add offered lordships
                                        var lordships = proposal.Sender.Lordships.Except(proposal.OfferedLordships).ToList();
                                        lordships = lordships.OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
                                        var lordshipSelectionCommand = new PlayerInputCommand(player);
                                        player.NextCommand = lordshipSelectionCommand;
                                        lordshipSelectionCommand.PromptFunc = () => { return GetLordshipSelectionPrompt(lordships); };
                                        lordshipSelectionCommand.CommandFunc = (lordshipSelectionCommandInput) =>
                                        {
                                            var selectedLordshipNumber = -1;
                                            int.TryParse(lordshipSelectionCommandInput, out selectedLordshipNumber);
                                            if (lordshipSelectionCommandInput.ToLower() == "x")
                                            {
                                                player.NextCommand = returnMenu;
                                            }
                                            if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= lordships.Count)
                                            {
                                                proposal.OfferedLordships.Add(lordships[selectedLordshipNumber - 1]);
                                                player.NextCommand = returnMenu;
                                            }
                                        };
                                    }
                                    break;
                                case "r":
                                    {
                                        //remove offered lordships
                                        var lordships = proposal.OfferedLordships.ToList();
                                        lordships = lordships.OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
                                        var lordshipSelectionCommand = new PlayerInputCommand(player);
                                        player.NextCommand = lordshipSelectionCommand;
                                        lordshipSelectionCommand.PromptFunc = () => { return GetLordshipSelectionPrompt(lordships); };
                                        lordshipSelectionCommand.CommandFunc = (lordshipSelectionCommandInput) =>
                                        {
                                            var selectedLordshipNumber = -1;
                                            int.TryParse(lordshipSelectionCommandInput, out selectedLordshipNumber);
                                            if (lordshipSelectionCommandInput.ToLower() == "x")
                                            {
                                                player.NextCommand = returnMenu;
                                            }
                                            if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= lordships.Count)
                                            {
                                                proposal.OfferedLordships.Remove(lordships[selectedLordshipNumber - 1]);
                                                player.NextCommand = returnMenu;
                                            }
                                        };
                                    }
                                    break;
                            }
                        };
                        break;
                    case "r":
                        if (proposal.Type == ProposalType.Proposal)
                        {
                            var updateRequestedLordshipsCommand = new PlayerInputCommand(player);
                            player.NextCommand = updateRequestedLordshipsCommand;
                            updateRequestedLordshipsCommand.PromptFunc = () =>
                            {
                                var prompt = "";
                                prompt += "[A]dd Requested Lordships, [R]emove Requested Lordships, E[x]it";
                                return prompt;
                            };
                            updateRequestedLordshipsCommand.CommandFunc = (offeredLordshipsInput) =>
                            {
                                switch (offeredLordshipsInput.ToLower())
                                {
                                    case "x":
                                        player.NextCommand = returnMenu;
                                        break;
                                    case "a":
                                        {
                                        //add requested lordships
                                        var lordships = proposal.Receiver.Lordships.Except(proposal.RequestedLordships).ToList();
                                            lordships = lordships.OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
                                            var lordshipSelectionCommand = new PlayerInputCommand(player);
                                            player.NextCommand = lordshipSelectionCommand;
                                            lordshipSelectionCommand.PromptFunc = () => { return GetLordshipSelectionPrompt(lordships); };
                                            lordshipSelectionCommand.CommandFunc = (lordshipSelectionCommandInput) =>
                                            {
                                                var selectedLordshipNumber = -1;
                                                int.TryParse(lordshipSelectionCommandInput, out selectedLordshipNumber);
                                                if (lordshipSelectionCommandInput.ToLower() == "x")
                                                {
                                                    player.NextCommand = returnMenu;
                                                }
                                                if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= lordships.Count)
                                                {
                                                    proposal.RequestedLordships.Add(lordships[selectedLordshipNumber - 1]);
                                                    player.NextCommand = returnMenu;
                                                }
                                            };
                                        }
                                        break;
                                    case "r":
                                        {
                                        //remove requested lordships
                                        var lordships = proposal.RequestedLordships.ToList();
                                            lordships = lordships.OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
                                            var lordshipSelectionCommand = new PlayerInputCommand(player);
                                            player.NextCommand = lordshipSelectionCommand;
                                            lordshipSelectionCommand.PromptFunc = () => { return GetLordshipSelectionPrompt(lordships); };
                                            lordshipSelectionCommand.CommandFunc = (lordshipSelectionCommandInput) =>
                                            {
                                                var selectedLordshipNumber = -1;
                                                int.TryParse(lordshipSelectionCommandInput, out selectedLordshipNumber);
                                                if (lordshipSelectionCommandInput.ToLower() == "x")
                                                {
                                                    player.NextCommand = returnMenu;
                                                }
                                                if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= lordships.Count)
                                                {
                                                    proposal.RequestedLordships.Remove(lordships[selectedLordshipNumber - 1]);
                                                    player.NextCommand = returnMenu;
                                                }
                                            };
                                        }
                                        break;
                                }
                            };
                        }
                        break;
                }
            };
        }
        public PlayerInputCommand NewPlayerMenu(Player player, PlayerInputCommand returnMenu, Random rnd)
        {
            var newPlayerCommand = new PlayerInputCommand(player);
            newPlayerCommand.PromptFunc = () =>{
                var prompt = "";
                prompt += "[I]nvade the new world, [A]ccept Invitation\n";
                return prompt;
            };
            newPlayerCommand.CommandFunc = (input) =>
            {
                switch (input.ToLower().Trim())
                {
                    case "i": {
                            player.NextCommand = InvasionMenu(player, newPlayerCommand, rnd);
                    }
                    break;
                    case "a": {
                            player.NextCommand = AcceptInvitationMenu(player, returnMenu, rnd);
                    } break;
                }
            };
            return newPlayerCommand;

        }
        public static PlayerInputCommand AcceptInvitationMenu(Player player, PlayerInputCommand returnMenu, Random rnd)
        {
            var acceptInvitationCommand = new PlayerInputCommand(player);
            acceptInvitationCommand.PromptFunc = () => {
                var prompt = "";
                prompt += "When you received your invitation, you were provided with secret words.  What are the secret words? (E[x]it)\n";
                return prompt;
            };
            acceptInvitationCommand.CommandFunc = (input) =>
            {
                if (input.ToLower().Trim() == "x")
                {
                    player.NextCommand = returnMenu;
                } else if (input.ToLower().Trim().Length > 0)
                {
                    var invitation = player.Game.World.Proposals.FirstOrDefault(p => p.Type == ProposalType.Invitation && p.InvitationCode.ToLower().Trim() == input.ToLower().Trim());
                    if (invitation != null)
                    {
                        var selectedProposal = invitation;
                        var proposalDetailCommand = new PlayerInputCommand(player);
                        proposalDetailCommand.PromptFunc = () =>
                        {
                            return selectedProposal.GetDeatailsAsString() + "\n"
                            + "[A]ccept, or E[x]it.";
                        };
                        player.NextCommand = proposalDetailCommand;
                        proposalDetailCommand.CommandFunc = (x) =>
                        {
                            var proposalDetailCommandInput = proposalDetailCommand.Input;
                            switch (proposalDetailCommandInput.ToLower())
                            {
                                case "a":
                                    {
                                        selectedProposal.Receiver = player.House;
                                        selectedProposal.Accept();
                                        player.NextCommand = returnMenu;
                                        if (player.Person.Lordships.FirstOrDefault(l => l.World == player.Game.World) != null)
                                        {
                                            proposalDetailCommand.Result.Output += ("Proposal accepted!");
                                            //move in!
                                            Lordship.PopulateLordship(
                                          newVillage: player.Person.Lordships.First(l => l.World == player.Game.World)
                                          , lordsHouseHold: player.Person.Household
                                          , peasantHouseholds: player.Person.Household.Lordship.Households.Where(h => h != player.Person.Household).ToList()
                                      );
                                            //remove lordships from the homeland
                                            player.Person.Lordships.RemoveAll(l => l.World != player.Game.World);

                                            player.Person.RefreshFullNameAndAge();

                                            //add house to world
                                            player.Game.World.AddHouse(player.House);

                                            //allow players full attacks 
                                            player.HouseLordshipsSummonedThisTurn.Clear();
                                            //set player to main menu
                                            var playerMenu = player.PlayerMenu(player, rnd, false);
                                            player.NextCommand = playerMenu;
                                        }
                                    }
                                    break;
                                case "x":
                                    {
                                        player.NextCommand = returnMenu;
                                    }
                                    break;
                            }
                        };

                    }
                }
            };
            return acceptInvitationCommand;
        }
        public static PlayerInputCommand InvasionMenu(Player player, PlayerInputCommand returnMenu, Random rnd)
        {
            var invasionCommand = new PlayerInputCommand(player);
            //player.NextCommand = invasionCommand;
            //for Novik show lordships on the east coast, for Sulthean show lorships on the west coast
            List<Lordship> invadableLordships;
            int invadableCoastXCoord = 0;

            if (player.Person.People == People.Norvik)
            {
                invadableCoastXCoord = Constants.MAP_WIDTH;
            }
            else
            {
                invadableCoastXCoord = 0;
            }

            invadableLordships = player.Game.World.Lordships.Where(l => l.MapX == invadableCoastXCoord).OrderBy(l => l.MapY).ToList();

            invasionCommand.PromptFunc = () =>
            {
                if (player.Person.Lordships.Count(l => l.World == player.Game.World) == 0)
                {

                    var prompt = "";


                    for (var i = 0; i < invadableLordships.Count(); i++)
                    {
                        prompt += (
                            string.Format(
                                 "{0}. {1}({2},{3}) - {4} defenders\n",
                                i + 1,
                                invadableLordships[i].Name,
                                invadableLordships[i].MapX,
                                invadableLordships[i].MapY,
                                invadableLordships[i].Defenders.Count()
                            )
                        );
                    }
                    prompt += (string.Format("You have {0} fighters. Enter the number of a lordship to invade or e[x]it.\n", player.Person.Household.Lordship.Army.Count()));

                    return prompt;
                } else
                {
                    //invasion successful
                    Lordship.PopulateLordship(
                      newVillage: player.Person.Lordships.First(l => l.World == player.Game.World)
                      , lordsHouseHold: player.Person.Household
                      , peasantHouseholds: player.Person.Household.Lordship.Households.Where(h => h != player.Person.Household).ToList()
                  );
                    //remove lordships from the homeland
                    player.Person.Lordships.RemoveAll(l => l.World != player.Game.World);

                    player.Person.RefreshFullNameAndAge();

                    //add house to world
                    player.Game.World.AddHouse(player.House);

                    //allow players full attacks 
                    player.HouseLordshipsSummonedThisTurn.Clear();
                    //set player to main menu
                    var playerMenu = player.PlayerMenu(player, rnd, false);
                    player.NextCommand = playerMenu;
                    return playerMenu.PromptFunc();
                }
            };
            invasionCommand.CommandFunc = (input) =>
            {
                int targetNumber = -1;
                int.TryParse(input, out targetNumber);

                if (targetNumber > 0 && targetNumber <= invadableLordships.Count())
                {
                    var target = invadableLordships[targetNumber - 1];
                    //var vassleSelectionCommand = new PlayerInputCommand(player);
                    Attack(
                        player: player
                        , commandingLordship: player.Person.Household.Lordship
                        , attackers: new List<Lordship>() { player.Person.Household.Lordship }
                        , returnMenu: invasionCommand
                        , rnd: rnd
                        , ignoreDistance: true
                        , target:target
                        , callingCommand: invasionCommand
                    );
                } else if (input.ToLower().Trim() == "x")
                {
                    player.NextCommand = returnMenu;
                }
            };
            return invasionCommand;
        }
        public void PersonMenu(Player player, Person person, PlayerInputCommand returnMenu, Random rnd)
        {
            //show details on when called from other menu
            //player.NextCommand.Result.Output += person.GetDetailsAsString() + "\n";
            var playerHasJuristiction = (person.Location.Lord != null && player.House == person.Location.Lord.House); 
            var personCommand = new PlayerInputCommand(player);
            player.NextCommand = personCommand;
            personCommand.PromptFunc = () => {
                var prompt = "";
                prompt += person.GetDetailsAsString() + "\n";
                prompt+=
                    "[D]etails, "
                    + ((person.Spouse == null && person.Bethrothal == null)?"[C]ompatible Foreign Nobles, ":"")
                    + (playerHasJuristiction?(person.Bethrothal == null?"[B]etroth, ":"[U]ndo Betrothal, ") + "[K]ill, ":"") 
                    + (playerHasJuristiction ?"":"[P]ropose Trade, ")
                    + "or E[x]it\n";
                return prompt;
            };
            personCommand.CommandFunc = (personCommandInput) =>
            {
                switch (personCommandInput.ToLower())
                {
                    case "x":
                        player.NextCommand = returnMenu;
                        break;
                    case "u":
                        {
                            if (playerHasJuristiction && person.Bethrothal != null)
                            {
                                var betrothal = person.Bethrothal;
                                var headOfHouseholdToBe = betrothal.HeadOfHouseholdToBe;
                                var spouseToBe = betrothal.SpouseToBe;
                                var record = string.Format(
                                    "Betrothal of {0} to {1} CANCELLED by {2}\n",
                                    betrothal.HeadOfHouseholdToBe.FullNameAndAge,
                                    betrothal.SpouseToBe.FullNameAndAge,
                                    player.House.Lord.FullNameAndAge
                                );
                                if (
                                    (headOfHouseholdToBe.IsHouseLord() || headOfHouseholdToBe.IsHouseHeir())
                                    || (spouseToBe.IsHouseLord() || spouseToBe.IsHouseHeir())
                                )
                                {
                                    betrothal.HeadOfHouseholdToBe.House.RecordHistory(record);
                                    if (betrothal.SpouseToBe.House != betrothal.HeadOfHouseholdToBe.House)
                                    {
                                        betrothal.SpouseToBe.House.RecordHistory(record);
                                    }
                                }
                                person.World.CancelBetrothal(person.Bethrothal);
                                personCommand.Result.Output += record;
                            }
                        }
                        break;
                    case "p":
                        {
                            if (!playerHasJuristiction)
                            {
                                var proposal = new Proposal()
                                {
                                    Sender = House,
                                    Receiver = person.Household.Lordship.Lord.House,
                                    RequestedPeople = new List<Person>() { person }
                                };
                                ProposalMenu(player, proposal, personCommand, rnd);
                            }
                        }
                        break;
                    case "c":
                        {
                            if (person.Spouse == null && person.Bethrothal == null)
                            {
                                var world = player.House.World;
                                var compatiblePersons =
                                    world.Population.Where(n =>
                                    n.Class == SocialClass.Noble &&
                                    n.People == person.People &&
                                    !world.Proposals.SelectMany(p => p.OfferedPeople).Union(world.Proposals.SelectMany(p => p.RequestedPeople)).Contains(n)
                                    && n.IsCompatibleForBetrothal(person)
                                    ).OrderBy(n => n.Age).ToList();

                                var compatibleCommand = new PlayerInputCommand(player);
                                player.NextCommand = compatibleCommand;
                                compatibleCommand.PromptFunc = () =>
                                {
                                    var prompt = "";
                                    prompt += ("Choose a person or E[x]it\n");
                                    for (var i = 0; i < compatiblePersons.Count(); i++)
                                    {
                                        prompt += (string.Format("{0}. {1}\n", i + 1, compatiblePersons[i].FullNameAndAge));
                                    }
                                    return prompt;
                                };
                                compatibleCommand.CommandFunc = (compatibleCommandInput) =>
                                {
                                    var compatiblePersonNumber = -1;
                                    Person compatiblePerson = null;
                                    int.TryParse(compatibleCommandInput, out compatiblePersonNumber);
                                    if (compatibleCommandInput.ToLower() == "x")
                                    {
                                        player.NextCommand = personCommand;
                                    }
                                    else if (compatiblePersonNumber > 0 && compatiblePersonNumber <= compatiblePersons.Count())
                                    {
                                        compatiblePerson = compatiblePersons[compatiblePersonNumber - 1];
                                        PersonMenu(player, compatiblePerson, returnMenu, rnd);
                                    }

                                };
                            }
                        }
                        break;
                    case "d":
                        personCommand.Result.Output += person.GetDetailsAsString() + "\n";
                        break;
                    case "k":
                        if (playerHasJuristiction)
                        {
                            var record = (person.FullNameAndAge + " was EXECUTED by " + player.House.Lord.FullNameAndAge + "\n");
                            if (person.IsHouseLord() || person.IsHouseHeir()) { 
                                person.House.RecordHistory(record);
                            }
                            personCommand.Result.Output += record;
                            person.Kill();
                            player.NextCommand = returnMenu;
                        }
                        break;
                    case "b":
                        if (playerHasJuristiction)
                        {
                            var eligibleSpouses =
                                person.Household.Lordship.Lord.House.Lordships
                                .SelectMany(
                                    l => l.Households.Where(
                                        h => h.HeadofHousehold.Class == SocialClass.Noble)
                                        .SelectMany(h => h.Members.Where(m => m.IsEligableForBetrothal() && m.IsCompatibleForBetrothal(person))
                                        )
                                    )
                                    .OrderByDescending(p => p.Age)
                                    .ThenBy(p => p.Name)
                                    .ToList();

                            var betrothalCommand = new PlayerInputCommand(player);
                            player.NextCommand = betrothalCommand;
                            betrothalCommand.PromptFunc = () =>
                            {
                                var prompt = "";
                                prompt += ("Choose a spouse for " + person.FullNameAndAge + ", or E[x]it\n");
                                for (var i = 0; i < eligibleSpouses.Count(); i++)
                                {
                                    prompt+=(string.Format("{0}. {1}\n", i + 1, eligibleSpouses[i].FullNameAndAge));
                                }

                                return prompt;
                            };
                            betrothalCommand.CommandFunc = (betrothalCommandInput) =>
                            {
                                var spouseNumber = -1;
                                Person spouse = null;
                                int.TryParse(betrothalCommandInput, out spouseNumber);
                                if(betrothalCommandInput.ToLower() == "x")
                                {
                                    player.NextCommand = personCommand;
                                } else if (spouseNumber > 0 && spouseNumber <= eligibleSpouses.Count())
                                {
                                    spouse = eligibleSpouses[spouseNumber - 1];
                                    var betrothal = person.World.CreateBethrothal(person, spouse, person.World.Year);
                                    var record = (string.Format("BETROTHAL: {0} was betrothed to {1}\n", person.FullNameAndAge, spouse.FullNameAndAge));
                                    if (person.IsHouseLord() || person.IsHouseHeir() || spouse.IsHouseLord() || spouse.IsHouseHeir())
                                    {
                                        person.House.RecordHistory(record);
                                        if (spouse.House != person.House)
                                        {
                                            spouse.House.RecordHistory(record);
                                        }
                                    }
                                    betrothalCommand.Result.Output += record;
                                    player.NextCommand = personCommand;
                                }
                            };
                        }
                        break;
                }

            };
    }
        public void HouseholdMenu(Player player, Household household, PlayerInputCommand returnMenu, Random rnd)
        {
            //var household = nobleHouseholds[householdNumber - 1];
            var householdMenuCommand = new PlayerInputCommand(player);
            var playerHasJuristiction = (household.Lordship.Lord != null && player.House == household.Lordship.Lord.House);
            //show house details when first called
            player.NextCommand = householdMenuCommand;
            householdMenuCommand.PromptFunc = () =>
            {
                var prompt = "";
                prompt += (household.GetDetailsAsString()) + "\n";
                prompt += "Household [D]etails, [M]ember Details, " + (playerHasJuristiction?"[R]esettle, [K]ill, [A]ttainder, ":"") + "or E[x]it\n";
                return prompt;
            };
            householdMenuCommand.CommandFunc = (householdMenuCommandInput) =>
            {
                switch (householdMenuCommandInput.ToLower())
                {
                    case "x":
                        player.NextCommand = returnMenu;
                        break;
                    case "a":
                        {
                            if (playerHasJuristiction) {
                                var message = "";
                                foreach (var member in household.Members)
                                {
                                    member.Class = SocialClass.Peasant;
                                    message += "ATTAINDER: " + member.FullNameAndAge + " was ATTAINED, stripped of all titles and nobility, by " + member.Household.Lordship.Lord.House.Lord.FullNameAndAge + "\n";
                                    if (member.IsHouseLord() || member.IsHouseHeir()) { 
                                        member.House.RecordHistory(message);
                                    }
                                    householdMenuCommand.Result.Output = message;
                                    player.NextCommand = returnMenu;
                                }
                            }
                        }
                        break;
                    case "d":
                        householdMenuCommand.Result.Output = (household.GetDetailsAsString()) + "\n";
                        break;
                    case "m":
                        var memberSelectionCommand = new PlayerInputCommand(player);
                        player.NextCommand = memberSelectionCommand;
                        memberSelectionCommand.PromptFunc = () =>
                        {
                            var prompt = "";
                            prompt += "Enter number for more details or e[x]it.\n";
                            for (var i = 0; i < household.Members.Count(); i++)
                            {
                                prompt+=(
                                    string.Format(
                                        "{0}. {1}\n",
                                        i + 1,
                                        household.Members[i].FullNameAndAge
                                        )
                                    );
                            }
                            return prompt;
                        };
                        memberSelectionCommand.CommandFunc = (memberSelectionCommandInput) => {
                            var memberNumber = -1;
                            int.TryParse(memberSelectionCommandInput, out memberNumber);
                            if (memberSelectionCommandInput.ToLower() == "x")
                            {
                                player.NextCommand = householdMenuCommand;
                            } else if (memberNumber >= 1 && memberNumber <= household.Members.Count())
                            {
                                PersonMenu(player, household.Members[memberNumber - 1], returnMenu, rnd);
                            }
                        };
                        break;
                    case "r":
                        if (playerHasJuristiction)
                        {
                            var subjectHouse = household.Lordship.Lord.House;
                            var resettleCommand = new PlayerInputCommand(player);
                            player.NextCommand = resettleCommand;
                            resettleCommand.PromptFunc = () =>
                            {
                                var prompt = "";
                                prompt += ("Select a lordship by number to resettle " + household.HeadofHousehold.FullNameAndAge) + "\n";
                                for (var i = 0; i < subjectHouse.Lordships.Count; i++)
                                {
                                    prompt += (
                                        string.Format(
                                            "{0}. {1}({2},{3})\n",
                                            i + 1,
                                            subjectHouse.Lordships[i].Name,
                                            subjectHouse.Lordships[i].MapX,
                                            subjectHouse.Lordships[i].MapY
                                            )
                                        );
                                }
                                return prompt;
                            };
                            resettleCommand.CommandFunc = (resettleCommandInput) =>
                            {
                                var selectedLordshipNumber = -1;
                                int.TryParse(resettleCommandInput, out selectedLordshipNumber);
                                if (resettleCommandInput.ToLower() == "x")
                                {
                                    player.NextCommand = householdMenuCommand;
                                }
                                else if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= subjectHouse.Lordships.Count)
                                {
                                    var newLordship = subjectHouse.Lordships[selectedLordshipNumber - 1];
                                    household.Resettle(newLordship);
                                    var output = (
                                        string.Format(
                                        "{0} RESETTLED in {1}",
                                        household.HeadofHousehold.FullNameAndAge,
                                        newLordship.Name
                                        ));
                                    if (household.HeadofHousehold.IsHouseLord() || household.HeadofHousehold.IsHouseHeir())
                                    {
                                        subjectHouse.RecordHistory(output);
                                    }
                                    resettleCommand.Result.Output = output;
                                    player.NextCommand = householdMenuCommand;
                                }
                            };
                        }
                        break;
                    case "k":
                        if (playerHasJuristiction)
                        {
                            var membersToKill = household.Members.ToList();
                            foreach (var person in membersToKill)
                            {
                                var record = (person.FullNameAndAge + " EXECUTED by " + player.House.Lord.FullNameAndAge + "\n");
                                if (person.IsHouseLord() || person.IsHouseHeir())
                                {
                                    person.House.RecordHistory(record);
                                }
                                householdMenuCommand.Result.Output += record;
                                person.Kill();
                            }
                            player.NextCommand = returnMenu;
                        }
                        break;
                }

            };
        }
        public void ChangeLordshipLordMenu(Player player, Lordship lordship, PlayerInputCommand returnMenu, Random rnd)
        {
            var possibleLords = lordship.Lord.House.Lordships
                .SelectMany(l=>l.Households.Where(h=>h.HeadofHousehold.Class == SocialClass.Noble))
                .SelectMany(h=>h.Members.Where(m=>m.Age >= Constants.AGE_OF_MAJORITY))
                .ToList();
            var changeLordshipMenuCommand = new PlayerInputCommand(player);
            player.NextCommand = changeLordshipMenuCommand;
            changeLordshipMenuCommand.PromptFunc = () =>
            {
                var prompt = "Enter number of new Lord of " + lordship.Name + ":\n";
                for (var i = 0; i < possibleLords.Count(); i++)
                {
                    prompt +=(
                        string.Format(
                            "{0}. {1}\n",
                            i + 1,
                            possibleLords[i].FullNameAndAge
                            )
                        );
                }
                return prompt;
            };
            changeLordshipMenuCommand.CommandFunc = (changeLordshipMenuCommandInput) =>
            {
                var memberNumber = -1;
                Person newLord = null;
                int.TryParse(changeLordshipMenuCommandInput, out memberNumber);
                if (changeLordshipMenuCommandInput.ToLower() == "x")
                {
                    player.NextCommand = returnMenu;
                } else if (memberNumber >= 1 && memberNumber <= possibleLords.Count())
                {
                    newLord = possibleLords[memberNumber - 1];
                    lordship.AddLord(newLord);
                    var lordsOldHousehold = newLord.Household;
                    Household lordsNewHousehold = null;
                    if (newLord.Household.HeadofHousehold == newLord)
                    {
                        lordsNewHousehold = lordsOldHousehold;
                    }
                    else
                    {
                        lordsNewHousehold = new Household();
                        lordsNewHousehold.AddMember(newLord);
                        lordsNewHousehold.HeadofHousehold = newLord;
                        lordsOldHousehold.Lordship.AddHousehold(lordsNewHousehold);
                    }
                    var record = string.Format("{0} was named Lord of {1}\n.", newLord.Name + " " + newLord.House.Name, lordship.Name);
                    newLord.House.RecordHistory(record);
                    changeLordshipMenuCommand.Result.Output = record;
                    player.NextCommand = returnMenu;
                }
            };
        }
        public void AddPeasantHouseHoldsMenu(Player player, Lordship subjectLordship, PlayerInputCommand returnMenu, Random rnd)
        {
            var lordshipSelectionCommand = new PlayerInputCommand(player);
            player.NextCommand = lordshipSelectionCommand;
            var otherLordships = subjectLordship.Lord.House.Lordships.Where(l => l != subjectLordship).OrderBy(l=>l.MapX).ThenBy(l=>l.MapX).ToList();
            lordshipSelectionCommand.PromptFunc = () => {
                var prompt = "Choose lordship to resettle from or e[x]it.\n";
                for (int i = 0; i < otherLordships.Count(); i++)
                {
                    prompt += (
                        string.Format(
                            "{0}. {1} ({2},{3}) ({4} peasant housholds)\n",
                            i + 1,
                            otherLordships[i].Name,
                            otherLordships[i].MapX,
                            otherLordships[i].MapY,
                            otherLordships[i].Households.Count(h => h.HeadofHousehold.Class == SocialClass.Peasant)
                            ));
                }
                return prompt;
            };
            lordshipSelectionCommand.CommandFunc = (lordshipSelectionInput) =>
            {
                var lordshipNumber = -1;
                int.TryParse(lordshipSelectionInput, out lordshipNumber);
                if (lordshipSelectionInput.ToLower() == "x")
                {
                    player.NextCommand = returnMenu;
                }
                else if (lordshipNumber >= 1 && lordshipNumber <= otherLordships.Count())
                {
                    var immigrantLordship = otherLordships[lordshipNumber - 1];
                    var availableHouseholds = immigrantLordship.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Peasant).ToList();
                    //ask how may households to move
                    var numberOfHouseholdsCommand = new PlayerInputCommand(player);
                    player.NextCommand = numberOfHouseholdsCommand;
                    numberOfHouseholdsCommand.PromptFunc = () =>
                    {
                        return string.Format(
                            "{0} has {1} households available. How many households would you like to resettle to {2}? (E[x]it)\n",
                            immigrantLordship.Name,
                            availableHouseholds.Count(),
                            subjectLordship.Name
                            );
                    };
                    numberOfHouseholdsCommand.CommandFunc = (numberOfHouseholdsCommandInput) => {
                        if (numberOfHouseholdsCommandInput.ToLower() == "x")
                        {
                            //return to lorship selection on cancel
                            player.NextCommand = lordshipSelectionCommand;
                        }
                        else
                        {
                            var numberOfHouseholds = -1;
                            int.TryParse(numberOfHouseholdsCommandInput, out numberOfHouseholds);
                            if (numberOfHouseholds > 0)
                            {
                                //choose random households to move
                                var immigrantHouseholds = new List<Household>();
                                while (availableHouseholds.Count() > 0 && immigrantHouseholds.Count() < numberOfHouseholds)
                                {
                                    var immigrantHousehold = availableHouseholds[rnd.Next(0, availableHouseholds.Count())];
                                    availableHouseholds.Remove(immigrantHousehold);
                                    immigrantHouseholds.Add(immigrantHousehold);
                                }
                                immigrantHouseholds.ForEach(h =>
                                {
                                    h.Resettle(subjectLordship);
                                });
                                numberOfHouseholdsCommand.Result.Output = (string.Format(
                                    "RESETTLE: {0} households RESETTLED in {1} from {2}.\n",
                                    immigrantHouseholds.Count(),
                                    subjectLordship.Name,
                                    immigrantLordship.Name
                                    ));
                                ResettlementThisTurn = true;
                                player.NextCommand = returnMenu;
                            }
                        }
                    };
                }
            };
        }
        public void ScoutMenu(Player player, Lordship subjectLordship, PlayerInputCommand returnMenu, Random rnd)
        {
            var scoutMenuCommand = new PlayerInputCommand(player);
            player.NextCommand = scoutMenuCommand;
            var visibleLordships = subjectLordship.GetVisibleLordships().OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
            scoutMenuCommand.PromptFunc = () =>
            {
                var prompt = "Select lordship for a report from your scouts or e[x]it:\n";
                for (var i = 0; i < visibleLordships.Count(); i++)
                {
                    prompt+=(
                        string.Format(
                             "{0}. {1}({2},{3}) - {4} defenders\n",
                            i + 1,
                            visibleLordships[i].Name,
                            visibleLordships[i].MapX,
                            visibleLordships[i].MapY,
                            visibleLordships[i].Defenders.Count()
                        )
                    );
                }
                return prompt;
            };
            scoutMenuCommand.CommandFunc = (scoutCommandInput) =>
            {
                var targetNumber = -1;
                Lordship lordshipToScout = null;
                int.TryParse(scoutCommandInput, out targetNumber);
                if (scoutCommandInput == "x")
                {
                    player.NextCommand = returnMenu;
                } else if (targetNumber > 0 && targetNumber <= visibleLordships.Count())
                {
                    lordshipToScout = visibleLordships[targetNumber - 1];
                    scoutMenuCommand.Result.Output = lordshipToScout.GetDetailsAsString() +"\n";
                    player.NextCommand = returnMenu;
                }
            };
        }
        public void AttackStagingMenu(Player player, Lordship subjectLordship, PlayerInputCommand callingMenu, PlayerInputCommand returnMenu, Random rnd)
        {
            if (player.ResettlementThisTurn)
            {
                callingMenu.Result.Output += ("No attacks can occur in a year after resettlement.\n");
            }
            else
            if (HouseLordshipsSummonedThisTurn.Contains(subjectLordship))
            {
                callingMenu.Result.Output += (subjectLordship.Name + " has already led the maximum nmber of attacks for this year.\n");
            }
            else
            {
                var abortAttack = false;
                var attackableLordships = subjectLordship.GetAttackableLordships().OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
                var targetNumber = -1;
                Lordship target = null;
                var availableVassles = player.House.Vassles.Where(v => !VasslesSummonedThisTurn.Contains(v)).ToList();
                var availableHouseLordships = House.Lordships.Where(l => l != subjectLordship && !HouseLordshipsSummonedThisTurn.Contains(l)).ToList();
                var summmonedVassles = new List<House>();
                var summmonedHouseLordships = new List<Lordship>();

                var targetSelectionCommand = new PlayerInputCommand(player);
                var vassleSelectionCommand = new PlayerInputCommand(player);
                var lordshipSelectionCommand = new PlayerInputCommand(player);
                Action<PlayerInputCommand> stageAttack = null;

                player.NextCommand = targetSelectionCommand;
                targetSelectionCommand.PromptFunc = () => {
                    var prompt = "";
                    for (var i = 0; i < attackableLordships.Count(); i++)
                    {
                        prompt += (
                            string.Format(
                                 "{0}. {1}({2},{3}) - {4} defenders\n",
                                i + 1,
                                attackableLordships[i].Name,
                                attackableLordships[i].MapX,
                                attackableLordships[i].MapY,
                                attackableLordships[i].Defenders.Count()
                            )
                        );
                    }
                    prompt+=("Enter the number of a lordship to attack or e[x]it.");
                    return prompt;
                };
                targetSelectionCommand.CommandFunc = (targetSelectionCommandInput) => {
                    //targetSelectionCommandInput = Console.ReadLine();
                    int.TryParse(targetSelectionCommandInput, out targetNumber);
                    if (targetSelectionCommandInput.ToLower() == "x") {
                        player.NextCommand = returnMenu;
                    } else if (targetNumber > 0 && targetNumber <= attackableLordships.Count()) {
                        target = attackableLordships[targetNumber - 1];
                        //var vassleSelectionCommand = new PlayerInputCommand(player);
                        if (availableVassles.Count() > 0)
                        {
                            player.NextCommand = vassleSelectionCommand;
                        } else if (availableHouseLordships.Count > 0)
                        {
                            player.NextCommand = lordshipSelectionCommand;
                        } else
                        {
                            stageAttack(targetSelectionCommand);
                        }
                    }
                };
                vassleSelectionCommand.PromptFunc = () => {
                    var prompt = "";
                    for (var i = 0; i < availableVassles.Count(); i++)
                    {
                        prompt += (
                            string.Format(
                                 "{0}. House {1}, {2} fighters\n",
                                i + 1,
                                availableVassles[i].Name,
                                availableVassles[i].Lordships.SelectMany(l => l.Army)
                                    .Union(availableVassles[i].GetAllSubVassles().SelectMany(v => v.Lordships.SelectMany(l => l.Army))).Count()
                            )
                        );
                    }
                    prompt+="Enter the number of a vassle to summon, [n]ext when done, summon [a]ll, or e[x]it.\n";
                    return prompt;
                };
                vassleSelectionCommand.CommandFunc = (vassleSelectionCommandInput) =>
                {
                    var vassleNumber = -1;
                    int.TryParse(vassleSelectionCommandInput, out vassleNumber);
                    if (vassleSelectionCommandInput.ToLower() == "x")
                    {
                        player.NextCommand = returnMenu;
                    }
                    else if (vassleSelectionCommandInput.ToLower() == "a")
                    {
                        summmonedVassles.AddRange(availableVassles);
                        availableVassles.Clear();
                    }
                    else if (vassleNumber > 0 && vassleNumber <= availableVassles.Count())
                    {
                        var summonedVassle = availableVassles[vassleNumber - 1];
                        summmonedVassles.Add(summonedVassle);
                        availableVassles.Remove(summonedVassle);
                    }
                    if (vassleSelectionCommandInput.ToLower() == "n" || availableVassles.Count() == 0)
                    {
                        if (availableHouseLordships.Count() > 0)
                        {
                            player.NextCommand = lordshipSelectionCommand;
                        } else
                        {
                            stageAttack(vassleSelectionCommand);
                        }
                    }
                };
                lordshipSelectionCommand.PromptFunc = () =>
                {
                    var prompt = "";
                    for (var i = 0; i < availableHouseLordships.Count(); i++)
                    {
                        prompt+=(
                            string.Format(
                                 "{0}. {1}({2},{3}) - {4} in army\n",
                                i + 1,
                                availableHouseLordships[i].Name,
                                availableHouseLordships[i].MapX,
                                availableHouseLordships[i].MapY,
                                availableHouseLordships[i].Army.Count()
                            )
                        );
                    }
                    prompt+="Enter the number of a lordship to summon, [n]ext when done, summon [a]ll, or e[x]it.";
                    return prompt;
                };
                lordshipSelectionCommand.CommandFunc = (houseLordshipCommandInput) =>{
                    var houseLordshipNumber = -1;
                    int.TryParse(houseLordshipCommandInput, out houseLordshipNumber);
                    if (houseLordshipCommandInput.ToLower() == "x") {
                        player.NextCommand = callingMenu;
                    }
                    if (houseLordshipCommandInput.ToLower() == "a")
                    {
                        summmonedHouseLordships.AddRange(availableHouseLordships);
                        availableHouseLordships.Clear();
                    }
                    if (houseLordshipNumber > 0 && houseLordshipNumber <= availableHouseLordships.Count())
                    {
                        var summondedHouseholdLordship = availableHouseLordships[houseLordshipNumber - 1];
                        summmonedHouseLordships.Add(summondedHouseholdLordship);
                        availableHouseLordships.Remove(summondedHouseholdLordship);
                    }
                    if (houseLordshipCommandInput.ToLower() == "n" || availableHouseLordships.Count() == 0)
                    {
                        stageAttack(lordshipSelectionCommand);
                    }
                };
                stageAttack = (callingCommand) => {
                    HouseLordshipsSummonedThisTurn.AddRange(summmonedHouseLordships);
                    HouseLordshipsSummonedThisTurn.Add(subjectLordship);
                    VasslesSummonedThisTurn.AddRange(summmonedVassles);
                    var attackers = new List<Lordship>();
                    attackers.Add(subjectLordship);
                    attackers.AddRange(summmonedHouseLordships);
                    attackers.AddRange(summmonedVassles.SelectMany(v => v.Lordships.Union(v.GetAllSubVassles().SelectMany(sv => sv.Lordships))));
                    Attack(player,subjectLordship, attackers, target, callingCommand, returnMenu,  rnd, true);
                };
            }

        }
        public void LordshipMenu(Player player, Lordship subjectLordship, PlayerInputCommand returnMenu, Random rnd)
        {
            //var subjectLordship = subjectHouse.Lordships[selectedLordshipNumber - 1];
            //var lordshipCommandInput = "";
            player.NextCommand.Result.Output += subjectLordship.GetDetailsAsString() +"\n";
            var lordshipCommand = new PlayerInputCommand(player);
            player.NextCommand = lordshipCommand;
            lordshipCommand.PromptFunc = () =>
            {
                return "[D]etails, [H]ouse, [W]orld, [M]ap, [A]ttack, [S]cout, Change [L]ord, Add [P]easant Households, E[x]it " + subjectLordship.Name + "\n";
            };
            lordshipCommand.CommandFunc = (lordshipCommandInput) =>
            {
                switch (lordshipCommandInput.ToLower())
                {
                    case "x":
                        //E[x]it
                        player.NextCommand = returnMenu;
                        break;
                    case "p":
                        //Add [P]easant Households
                        AddPeasantHouseHoldsMenu(player, subjectLordship, lordshipCommand, rnd);
                        break;
                    case "l":
                        ChangeLordshipLordMenu(player, subjectLordship, lordshipCommand, rnd);
                        break;
                    case "d":
                    case "details":
                        lordshipCommand.Result.Output = subjectLordship.GetDetailsAsString() + "\n";
                        break;
                    case "m":
                    case "map":
                            lordshipCommand.Result.Output = subjectLordship.GetMapOfKnownWorld() + "\n";
                        break;
                    case "s":
                            ScoutMenu(player, subjectLordship, lordshipCommand, rnd);
                        break;
                    case "a":
                    case "attack":
                        {
                            AttackStagingMenu(player, subjectLordship, lordshipCommand, lordshipCommand, rnd);
                        }
                        break;
                    case "w":
                        var world = player.House.World;
                        lordshipCommand.Result.Output =
                        world.GetMapAsString() + "\n"
                        + world.GetDetailsAsString() + "\n";
                        break;
                }
            };
        }
        public void SubjectMenu(Player player, House subjectHouse, PlayerInputCommand returnMenu, Random rnd)
        {
            var peopleSelectionCommand = new PlayerInputCommand(player);
            player.NextCommand = peopleSelectionCommand;
            peopleSelectionCommand.PromptFunc = () =>
            {
                var prompt = "";
                prompt +=
                    "Select a people:\n"
                    + "1. Kyltkled\n"
                    + "2. Norvik\n"
                    + "3. Sulthaen\n";
                return prompt;
                };
            peopleSelectionCommand.CommandFunc = (peopleSelectionInput) =>
            {
                People selectedPeople = People.Norvik;
                switch (peopleSelectionInput.ToLower())
                {
                    case "x":
                        player.NextCommand = returnMenu;
                        return;
                    case "1":
                        selectedPeople = People.Kyltcled;
                        break;
                    case "2":
                        selectedPeople = People.Norvik;
                        break;
                    case "3":
                        selectedPeople = People.Sulthaen;
                        break;
                }

                var subjectMenuCommand = new PlayerInputCommand(player);
                player.NextCommand = subjectMenuCommand;
                var nobleHouseholds = subjectHouse.Lordships
                    .SelectMany(l => l.Households.Where(h => h.Members.Count() > 0 && h.HeadofHousehold.Class == SocialClass.Noble && h.HeadofHousehold.People==selectedPeople))
                    .OrderBy(h => h.HeadofHousehold.House.Name)
                    .ThenBy(h => h.HeadofHousehold.Name)
                    .ToList();

                var pagelength = Constants.DEFAULT_PAGE_LENGTH;
                var page = 1;

                subjectMenuCommand.PromptFunc = () =>
                {
                    var startingIndex = (page - 1) * pagelength;
                    var endingIndex = startingIndex + pagelength - 1;
                    var prompt = "Noble households of House " + subjectHouse.Name + ":\n";
                    if (startingIndex < nobleHouseholds.Count())
                    {
                        for (var i = startingIndex; i<=endingIndex && i < nobleHouseholds.Count(); i++)
                        {
                            prompt += (
                                string.Format(
                                    "{0}. {1}\n",
                                    i + 1,
                                    nobleHouseholds[i].HeadofHousehold.FullNameAndAge
                                    )
                                );
                        }
                    }
                    prompt += "Enter Household Number for more details," + (player.House == subjectHouse ? "[A]ttain all, " : "") + "[N]ext Page, [P]rev Page, E[x]it\n";
                    return prompt;
                };
                subjectMenuCommand.CommandFunc = (householdCommandInput) =>
                {
                    int householdNumber = -1;
                    int.TryParse(householdCommandInput, out householdNumber);
                    if (householdCommandInput.ToLower() == "x")
                    {
                        player.NextCommand = returnMenu;
                    }
                    else if(householdCommandInput.ToLower() == "p" && page>1) //prev page
                    {
                        page--;
                    }
                    else if (householdCommandInput.ToLower() == "n")//next page
                    {
                        if (page <= nobleHouseholds.Count / pagelength)
                        {
                            page++;
                        }
                    }
                    else if (householdCommandInput.ToLower() == "a")
                    {
                        if (player.House == subjectHouse)
                        {
                            foreach (var household in nobleHouseholds)
                            {
                                foreach (var member in household.Members)
                                {
                                    member.Class = SocialClass.Peasant;
                                    var message = "ATTAINDER: " + member.FullNameAndAge + " was ATTAINED, stripped of all titles and nobility, by " + member.Household.Lordship.Lord.House.Lord.FullNameAndAge + "\n";
                                    if (member.IsHouseHeir() || member.IsHouseLord())
                                    {
                                        member.House.RecordHistory(message);
                                    }
                                    subjectMenuCommand.Result.Output += message;
                                    player.NextCommand = returnMenu;
                                }
                            }
                        } 
                    }
                    else if (householdNumber >= 1 && householdNumber <= nobleHouseholds.Count)
                    {
                        HouseholdMenu(player, nobleHouseholds[householdNumber - 1], returnMenu, rnd);
                    }

                };

            };
        }
        public void HouseMenu(Player player, House house, PlayerInputCommand CallingMenuCommand, Random rnd)
        {
            var isVassle = player.House.Vassles.Contains(house);
            var isAlly = player.House.GetAlliedHouses().Contains(house);

            var houseCommand = new PlayerInputCommand(player)
            {
                PromptFunc = ()=> {
                return house.GetDetailsAsString() 
                    + "\nOffer [P]roposal"
                    + (isAlly?", Send [F]ighters":"")
                    + ", [S]ubjects"
                    + (isVassle?", [R]elease Vassle":"")
                    + ", E[x]it\n"; }
            };
            player.NextCommand = houseCommand;
            houseCommand.CommandFunc = (input) =>
            {
                var houseCommandInput = houseCommand.Input;
                switch (houseCommandInput.ToLower())
                {
                    case "x":
                        {
                            player.NextCommand = CallingMenuCommand;
                        }
                        break;
                    case "p":
                        {
                            //house.AddVassle(player.House);
                            //var record = ("FEALTY: " + house.Lord.FullNameAndAge + " accepted FEALTRY from " + player.House.Lord.FullNameAndAge) + "\n";
                            //house.RecordHistory(record);
                            //player.House.RecordHistory(record);
                            //houseCommand.Result.Output += record;
                            var proposal = new Proposal() {
                                Sender = player.House,
                                Receiver = house
                            };
                            ProposalMenu(player, proposal, houseCommand, rnd);
                        }
                        break;
                    case "s":
                        {
                            SubjectMenu(player, house, houseCommand, rnd);
                        }
                        break;
                    case "r":
                        {
                            if (isVassle)
                            {
                                House.RemoveVassle((House)house);
                            }
                        }
                        break;
                    case "f":
                        {
                            if (isAlly) { 
                            var lordshipSelectionCommand = new PlayerInputCommand(player);
                            player.NextCommand = lordshipSelectionCommand;
                            var otherLordships = House.Lordships;//subjectLordship.Lord.House.Lordships.Where(l => l != subjectLordship).ToList();
                            lordshipSelectionCommand.PromptFunc = () =>
                            {
                                var prompt = "Choose lordship to resettle from or e[x]it.\n";
                                for (int i = 0; i < otherLordships.Count(); i++)
                                {
                                    prompt += (
                                        string.Format(
                                            "{0}. {1} ({2} peasant housholds)\n",
                                            i + 1,
                                            otherLordships[i].Name,
                                            otherLordships[i].Households.Count(h => h.HeadofHousehold.Class == SocialClass.Peasant)
                                            ));
                                }
                                return prompt;
                            };
                            lordshipSelectionCommand.CommandFunc = (lordshipSelectionInput) =>
                            {
                                var lordshipNumber = -1;
                                int.TryParse(lordshipSelectionInput, out lordshipNumber);
                                if (lordshipSelectionInput.ToLower() == "x")
                                {
                                    player.NextCommand = houseCommand;
                                }
                                else if (lordshipNumber >= 1 && lordshipNumber <= otherLordships.Count())
                                {
                                    var immigrantLordship = otherLordships[lordshipNumber - 1];
                                    var availableHouseholds = immigrantLordship.Households.Where(h => h.HeadofHousehold.Class == SocialClass.Peasant).ToList();
                                    //ask how may households to move
                                    var numberOfHouseholdsCommand = new PlayerInputCommand(player);
                                    player.NextCommand = numberOfHouseholdsCommand;
                                    numberOfHouseholdsCommand.PromptFunc = () =>
                                    {
                                        return string.Format(
                                            "{0} has {1} households available. How many households would you like to resettle to {2}? (E[x]it)\n",
                                            immigrantLordship.Name,
                                            availableHouseholds.Count(),
                                            house.Lord.Household.Lordship
                                            );
                                    };
                                    numberOfHouseholdsCommand.CommandFunc = (numberOfHouseholdsCommandInput) =>
                                    {
                                        if (numberOfHouseholdsCommandInput.ToLower() == "x")
                                        {
                                            //return to lorship selection on cancel
                                            player.NextCommand = houseCommand;
                                        }
                                        else
                                        {
                                            var numberOfHouseholds = -1;
                                            int.TryParse(numberOfHouseholdsCommandInput, out numberOfHouseholds);
                                            if (numberOfHouseholds > 0)
                                            {
                                                //choose random households to move
                                                var immigrantHouseholds = new List<Household>();
                                                while (availableHouseholds.Count() > 0 && immigrantHouseholds.Count() < numberOfHouseholds)
                                                {
                                                    var immigrantHousehold = availableHouseholds[rnd.Next(0, availableHouseholds.Count())];
                                                    availableHouseholds.Remove(immigrantHousehold);
                                                    immigrantHouseholds.Add(immigrantHousehold);
                                                }
                                                immigrantHouseholds.ForEach(h =>
                                                {
                                                    h.Resettle(house.Lord.Household.Lordship);
                                                });
                                                numberOfHouseholdsCommand.Result.Output = (string.Format(
                                                    "RESETTLE: {0} households RESETTLED in {1} from {2}.\n",
                                                    immigrantHouseholds.Count(),
                                                    house.Lord.Household.Lordship.Name,
                                                    immigrantLordship.Name
                                                    ));
                                                ResettlementThisTurn = true;
                                                player.NextCommand = CallingMenuCommand;
                                            }
                                        }
                                    };
                                }
                            };
                        }
                            break;

                        }
                }
            };
            //} while (houseCommandInput != "x");
        }
        public void VassleMenu(Player player, House subjectHouse, PlayerInputCommand CallingMenuCommand, Random rnd)
        {
            //select vassle
            var selectVassleCommand = new PlayerInputCommand(player);
            player.NextCommand = selectVassleCommand;
            selectVassleCommand.PromptFunc = () =>
            {
                var prompt = ("Select vassle or e[x]it.\n");
                for (var i = 0; i < subjectHouse.Vassles.Count(); i++)
                {
                    var vassle = subjectHouse.Vassles[i];
                    prompt += (
                        string.Format(
                            "{0}. House {1}, {2} fighters\n",
                            i + 1,
                            vassle.Name,
                            vassle.Lordships.SelectMany(l => l.Army).Count()
                            )
                        );
                }
                return prompt;
            };
            selectVassleCommand.CommandFunc = (input) => {
                var selectVassleCommandInput = selectVassleCommand.Input;
                var selectedVassleNumber = -1;
                if (selectVassleCommandInput.ToLower() == "x")
                {
                    player.NextCommand = CallingMenuCommand;
                } else
                {
                    int.TryParse(selectVassleCommandInput, out selectedVassleNumber);
                    if (selectedVassleNumber >= 1 && selectedVassleNumber <= subjectHouse.Vassles.Count())
                    {
                        var vassle = subjectHouse.Vassles[selectedVassleNumber - 1];
                        HouseMenu(player, vassle, CallingMenuCommand, rnd);
                    }

                }
            };
            //manipulate vassle
        }
        public void NobleHousesMenu(Player player, PlayerInputCommand playerCommand, Random rnd)
        {
            var nobleHousesPeopleCommand = new PlayerInputCommand(player);
            var world = player.House.World;
            player.NextCommand = nobleHousesPeopleCommand;

            nobleHousesPeopleCommand.PromptFunc = () =>
            {
                return
              ("1. Norvik") + "\n"
              + ("2. Sulthean") + "\n";
            };

            nobleHousesPeopleCommand.CommandFunc = (input) =>
            {
                //result = nobleHousesPeopleCommand.Result;
                var peopleSelection = input;
                var peopleSelectionNumber = -1;

                if (input.ToLower() == "x")
                {
                    //result.Output = playerMenuPrompt;
                    player.NextCommand = playerCommand;
                    return;
                }
                else if (int.TryParse(peopleSelection, out peopleSelectionNumber) && peopleSelectionNumber >= 1 && peopleSelectionNumber <= 2)
                {

                    var houseSelectionCommand = new PlayerInputCommand(player);
                    player.NextCommand = houseSelectionCommand;

                    var selectedPeople = People.Norvik;
                    if (peopleSelectionNumber == 2)
                    {
                        selectedPeople = People.Sulthaen;
                    }
                    var houses = world.NobleHouses.Where(h => h.Lord != null && h.Lord.People == selectedPeople).OrderBy(h => h.Name).ToList();
                    House selectedHouse = null;

                    houseSelectionCommand.PromptFunc = () =>
                    {
                        var prompt = "Select a house (or e[x]it):\n";
                        for (int i = 0; i < houses.Count(); i++)
                        {
                            prompt += ((i + 1) + ". " + houses[i].Name) + "\n";
                        }
                        return prompt;
                    };

                    houseSelectionCommand.CommandFunc = (x) =>
                    {
                        if (houseSelectionCommand.Input.ToLower() == "x")
                        {
                            player.NextCommand = playerCommand;
                        }
                        else
                        {
                            var selectedHouseNumber = -1;
                            int.TryParse(houseSelectionCommand.Input, out selectedHouseNumber);

                            if (selectedHouseNumber > 0 && selectedHouseNumber <= houses.Count())
                            {
                                selectedHouse = houses[selectedHouseNumber - 1];
                            }
                            HouseMenu(player, selectedHouse, playerCommand, rnd);
                        }
                    };
                }

            };
        }
        public void ProposalReviewMenu(Player player, List<Proposal> incomingProposals, PlayerInputCommand returnMenu, Random rnd)
        {
            var proposalCommand = new PlayerInputCommand(player);
            var proposalCommandInput = "";
            Proposal selectedProposal = null;
            player.NextCommand = proposalCommand;
            proposalCommand.PromptFunc = () =>
            {
                var prompt = ("Select a proposal to review or e[x]it:\n");
                for (int i = 0; i < incomingProposals.Count(); i++)
                {
                    if (incomingProposals[i].Sender.Lord != null)
                    {
                       prompt+=(i + 1 + ". " + incomingProposals[i].Sender.Lord.FullNameAndAge) + "\n";
                    }
                }
                return prompt;
            };

            proposalCommand.CommandFunc = (input) =>
            {
                proposalCommandInput = proposalCommand.Input;
                var proposalNumber = -1;
                if (proposalCommandInput.ToLower() == "x")
                {
                    player.NextCommand = returnMenu;
                }
                if (int.TryParse(proposalCommandInput, out proposalNumber) && proposalNumber > 0 && proposalNumber <= incomingProposals.Count())
                {
                    selectedProposal = incomingProposals[proposalNumber - 1];
                    var proposalDetailCommand = new PlayerInputCommand(player);
                    proposalDetailCommand.PromptFunc = () =>
                    {
                        return selectedProposal.GetDeatailsAsString() + "\n"
                        + "[A]ccept, [R]eject, or E[x]it.";
                    };
                    player.NextCommand = proposalDetailCommand;
                    proposalDetailCommand.CommandFunc = (x) =>
                    {
                        var proposalDetailCommandInput = proposalDetailCommand.Input;
                        switch (proposalDetailCommandInput.ToLower())
                        {
                            case "a":
                                {
                                    selectedProposal.Accept();
                                    incomingProposals.Remove(selectedProposal);
                                    proposalDetailCommand.Result.Output += ("Proposal accepted!");
                                    player.NextCommand = proposalCommand; 
                                }
                                break;
                            case "r":
                                {
                                    selectedProposal.Reject();
                                    incomingProposals.Remove(selectedProposal);
                                    proposalDetailCommand.Result.Output += ("Proposal rejected!");
                                    player.NextCommand = proposalCommand;
                                }
                                break;
                            case "x":
                                {
                                    player.NextCommand = proposalCommand;
                                }
                                break;
                        }
                    };
                }
            };
        }
        public PlayerInputCommand PlayerMenu(Player player, Random rnd, bool autoSetToNextCommand = true)
        {
            //get variables for menu 
            var world = player.House.World;
            var subjectHouse = player.House;
            var incomingProposals = world.Proposals.Where(p => p.Receiver == House && p.Status == ProposalStatus.New).ToList();

            //set up command
            var playerCommand = new PlayerInputCommand(player);
            player.NextCommand = playerCommand;
            playerCommand.PromptFunc = () => {
                return 
                    ("Year: " + world.Year) + "\n"
                    + (subjectHouse.Lord.Name + " " + subjectHouse.Name + " (" + subjectHouse.FightersAvailable.Count() + " fighters): Incoming [P]roposals(" + incomingProposals.Count() + "), [N]oble Houses, [H]istory, [D]etails, [S]ubjects, [M]ap, [L]ordships, [I]nvite, E[x]it " + subjectHouse.Name)
                    + "\n";
            };
            //var result = player.NextCommand.Result;
            //var playerMenuInput = player.NextCommand.Input;
            playerCommand.CommandFunc = (input) =>
            {
                switch (input)
                {
                    default:
                        //result.Output = playerCommand.Prompt;
                        break;
                    case "newplayer":
                        player.Game.NewPlayer("", "", Sex.Female, People.Norvik, PlayerType.Live, rnd);
                        break;
                    case "x":
                        player.NextCommand = null;
                        break;
                    case "n":
                        NobleHousesMenu(player, playerCommand, rnd);
                        break;
                    case "p":
                        {
                            ProposalReviewMenu(player, incomingProposals, playerCommand, rnd);
                        }
                        break;
                    case "w":
                        playerCommand.Result.Output =
                        world.GetMapAsString() + "\n"
                        + world.GetDetailsAsString() + "\n";
                        break;
                    case "v":
                        VassleMenu(player, subjectHouse, playerCommand, rnd);
                        break;
                    case "d":
                        {
                            playerCommand.Result.Output = subjectHouse.GetDetailsAsString() + "\n";
                        }
                        break;
                    case "h":
                        {
                            foreach (var recordedYear in subjectHouse.History)
                            {
                                playerCommand.Result.Output +=
                                "\n" + recordedYear.Key + "\n"
                                + recordedYear.Value + "\n";
                            }
                        }
                        break;
                    case "s":
                        {
                            SubjectMenu(player, subjectHouse, playerCommand, rnd);
                        }
                        break;
                    case "m":
                        {
                            if (subjectHouse.Seat != null)
                            {
                                playerCommand.Result.Output = subjectHouse.Seat.GetMapOfKnownWorld();
                            }
                        }
                        break;
                    case "l":
                        {
                            var lordshipSelectionCommand = new PlayerInputCommand(player);
                            player.NextCommand = lordshipSelectionCommand;
                            var lordships = subjectHouse.Lordships.OrderBy(l => l.MapX).ThenBy(l => l.MapY).ToList();
                            lordshipSelectionCommand.PromptFunc = () =>
                            {
                                var prompt = "Select a lorship by number or e[x]it.\n";
                                for (var i = 0; i < lordships.Count; i++)
                                {
                                    prompt += (
                                        string.Format(
                                            "{0}. {1}({2},{3}) - {4} fighters\n",
                                            i + 1,
                                            lordships[i].Name,
                                            lordships[i].MapX,
                                            lordships[i].MapY,
                                            lordships[i].Army.Count()
                                            )
                                        );
                                }
                                return prompt;
                            };
                            lordshipSelectionCommand.CommandFunc = (lordshipSelectionCommandInput) =>
                            {
                                var selectedLordshipNumber = -1;
                                int.TryParse(lordshipSelectionCommandInput, out selectedLordshipNumber);
                                if (lordshipSelectionCommandInput.ToLower() == "x")
                                {
                                    player.NextCommand = playerCommand;
                                }
                                if (selectedLordshipNumber >= 1 && selectedLordshipNumber <= lordships.Count)
                                {
                                    LordshipMenu(player, lordships[selectedLordshipNumber - 1], playerCommand, rnd);
                                }
                            };
                        }
                        break;
                    case "i": //invitation
                        {
                            var invitation = new Proposal() {
                                Type = ProposalType.Invitation
                                , Sender = player.House
                            };

                            ProposalMenu(player, invitation, playerCommand, rnd);
                        }
                        break;
                }
            };
            return playerCommand;
        }
        public void DoLivePlayerTurn(Random rnd)
        {
            if (NextCommand == null)
            {
                PlayerMenu(this, rnd);
                NextCommand = new PlayerInputCommand(this);
                NextCommand.CommandFunc = (x) =>
                {
                    PlayerMenu(this, rnd);
                };
                NextCommand.Execute();
                LastCommand = NextCommand;
            }
            while (NextCommand != null)
            {
                if (LastCommand != null)
                {
                    Console.Write(LastCommand.Result.Output);
                }
                Console.Write(NextCommand.GetPrompt());
                NextCommand.Input = Console.ReadLine();
                LastCommand = NextCommand;
                NextCommand.Execute();
            }
            
        }
        public List<Lordship> AttackHistory { get; set; }
        public Player Flatten()
        {
            return new Player()
            {
                AttackHistory = AttackHistory.Select(a => new Lordship(new Random()) { Id = a.Id }).ToList(),
                Game = new Game() { Id = Game.Id },
                House = new House() { Id = House.Id },
                HouseLordshipsSummonedThisTurn = HouseLordshipsSummonedThisTurn.Select(l => new Lordship(new Random()) { Id = l.Id }).ToList(),
                Id = Id,
                PlayerType = PlayerType,
                ResettlementThisTurn = ResettlementThisTurn,
                VasslesSummonedThisTurn = VasslesSummonedThisTurn.Select(v => new House() { Id = v.Id }).ToList()
            };
        }
    }
}
