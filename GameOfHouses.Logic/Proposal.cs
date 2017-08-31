using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameOfHouses.Logic
{
    public enum ProposalStatus
    {
        New = 0,
        Accepted = 1,
        Rejected = 2,
        ResponseReceived =3 
    }
    public class Proposal
    {
        public Proposal()
        {
            Id = Guid.NewGuid();
            Status = ProposalStatus.New;
            OfferedPeople = new List<Person>();
            OfferedLordships = new List<Lordship>();
            OfferedMoney = 0;
            RequestedPeople = new List<Person>();
            RequestedLordships = new List<Lordship>();
            RequestedMoney = 0;
            Message = "";
            Type = ProposalType.Proposal;
        }
        public Guid Id { get; set; }
        public ProposalType Type { get; set; }
        public ProposalStatus Status { get; set; }
        public House Sender { get; set; }
        public House Receiver { get; set; }
        public string InvitationCode { get; set; }
        public bool OfferedFealty { get; set; }
        public List<Person> OfferedPeople { get; set; }
        public List<Lordship> OfferedLordships { get; set; }
        public double OfferedMoney { get; set; }
        public bool RequestedFealty { get; set; }
        public List<Person> RequestedPeople { get; set; }
        public List<Lordship> RequestedLordships { get; set; }
        public double RequestedMoney { get; set; }
        public string Message { get; set; }
        public void Accept()
        {
            if (Sender.Lord != null && Receiver.Lord != null)
            {
                var valid = true;
                //first verify
                //money
                if(Sender.Wealth < OfferedMoney)
                {
                    valid = false;
                    Message += "Sender has insufficient funds.\n";
                }
                if(Receiver.Wealth < RequestedMoney)
                {
                    valid = false;
                    Message += "Receiver has insufficient funds.\n";
                }
                //people
                foreach(var person in OfferedPeople)
                {
                    if (!person.IsAlive)
                    {
                        Message += person.FullNameAndAge + " is dead\n";
                        valid = false;
                    }
                    if (person.Household.Lordship.Lord.House != Sender)
                    {
                        Message += string.Format("House {0} does not have juristiction over {1}\n",Sender.Name, person.FullNameAndAge);
                        valid = false;
                    }
                }
                foreach (var person in RequestedPeople)
                {
                    if (!person.IsAlive)
                    {
                        Message += person.FullNameAndAge + " is dead\n";
                        valid = false;
                    }
                    if (person.Household.Lordship.Lord.House != Receiver)
                    {
                        Message += string.Format("House {0} does not have juristiction over {1}\n", Receiver.Name, person.FullNameAndAge);
                        valid = false;
                    }
                }
                //lordships
                foreach (var lordship in OfferedLordships)
                {
                    if(lordship.Lord.House != Sender)
                    {
                        Message += string.Format("House {0} does not have juristiction over {1}\n", Sender.Name, lordship.Name);
                        valid = false;
                    }
                }
                foreach (var lordship in RequestedLordships)
                {
                    if (lordship.Lord.House != Receiver)
                    {
                        Message += string.Format("House {0} does not have juristiction over {1}\n", Receiver.Name, lordship.Name);
                        valid = false;
                    }
                }
                if (!valid)
                {
                    Reject();
                }
                else
                {
                    //accept proposal
                    Status = ProposalStatus.Accepted;

                    //money
                    Sender.Wealth -= OfferedMoney;
                    Receiver.Wealth += OfferedMoney;

                    Sender.Wealth += RequestedMoney;
                    Receiver.Wealth -= RequestedMoney;

                    //fealty
                    if (OfferedFealty)
                    {
                        Receiver.AddVassle(Sender);
                    }
                    else if (RequestedFealty)
                    {
                        Sender.AddVassle(Receiver);
                    }

                    //lordships
                    foreach (var lordship in OfferedLordships)
                    {
                        lordship.AddLord(Receiver.Lord);
                    }
                    foreach (var lordship in RequestedLordships)
                    {
                        lordship.AddLord(Sender.Lord);
                    }

                    var world = Sender.World;

                    //people
                    foreach (var person in OfferedPeople)
                    {
                        Receiver.Lord.Household.AddMember(person);
                        world.EligibleNobles.Remove(person);
                    }
                    foreach (var person in RequestedPeople)
                    {
                        Sender.Lord.Household.AddMember(person);
                        world.EligibleNobles.Remove(person);
                    }
                    Message +=
                    "PROPOSAL ACCEPTED: \n"
                    + GetDeatailsAsString();
                    Receiver.RecordHistory(Message);
                    Sender.RecordHistory(Message);
                    world.Proposals.Remove(this);
                }
            }
        }
        public void Reject()
        {
            //reject proposal
            var world = Sender.World;
            Status = ProposalStatus.Rejected;
            Message +=
            "PROPOSAL REJECTED: \n"
            + GetDeatailsAsString();
            Receiver.RecordHistory(Message);
            Sender.RecordHistory(Message);
            world.Proposals.Remove(this);
        }
        public string GetDeatailsAsString()
        {
            var output = "";
            if (Type == ProposalType.Proposal)
            {
                output += "PROPOSAL: \n";
            } else
            {
                output += "INVITATION: \n";
            }
            output +="\tSender: " + (Sender.Lord != null ? Sender.Lord.FullNameAndAge : "Unknown") + "\n";
            if (Type == ProposalType.Proposal)
            {
                output += "\tReciever: " + Receiver.Lord.FullNameAndAge + "\n";
            }
            if (Message.Length > 0)
            {
                output += "\tMessage:\n"
                + "\t" + Message + "\n";
            }
            if (OfferedFealty)
            {
                output += "\tFealty offered.\n";
            }
            if (RequestedFealty)
            {
                output += "\tFealty requested.\n";
            }
            if (OfferedMoney > 0)
            {
                output += "\tOffered Money:" + String.Format("{0:n0}", OfferedMoney) + "\n";
            }
            if (RequestedMoney > 0)
            {
                output += "\tRequested Money:" + String.Format("{0:n0}", RequestedMoney) + "\n";
            }
            if (OfferedPeople.Count() > 0)
            {
                output += "\tOffered People\n";
                OfferedPeople.ForEach(p => output += "\t\t" + p.FullNameAndAge + "\n");
            }
            if (RequestedPeople.Count() > 0)
            {
                output += "\tRequested People\n";
                RequestedPeople.ForEach(p => output += "\t\t" + p.FullNameAndAge + "\n");
            }
            if (OfferedLordships.Count() > 0)
            {
                output += "\tOffered Lordships\n";
                OfferedLordships.ForEach(l => output += "\t\t" + l.Name + "\n");
            }
            if (RequestedLordships.Count() > 0)
            {
                output += "\tRequested Lordships\n";
                RequestedLordships.ForEach(l => output += "\t\t" + l.Name + "\n");
            }
            return output;
        }
        public Proposal Flatten()
        {
            return new Proposal()
            {
                Id = Id, 
                Message = Message, 
                OfferedFealty = OfferedFealty, 
                OfferedLordships = OfferedLordships.Select(l=>new Lordship(new Random()) { Id = l.Id}).ToList(),
                OfferedMoney = OfferedMoney, 
                OfferedPeople = OfferedPeople.Select(p=>new Person(new Random()) { Id = p.Id}).ToList(),
                Receiver = new House() { Id = Receiver.Id}, 
                RequestedFealty = RequestedFealty, 
                RequestedLordships = RequestedLordships.Select(l=>new Lordship(new Random()) { Id = l.Id}).ToList(),
                RequestedMoney = RequestedMoney, 
                RequestedPeople = RequestedPeople.Select(p=>new Person(new Random()) { Id = p.Id}).ToList(),
                Sender = new House() { Id = Sender.Id},
                Status = Status
            };
        }
    }
}
