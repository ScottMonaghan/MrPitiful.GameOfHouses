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
        }
        public Guid Id { get; set; }
        public ProposalStatus Status { get; set; }
        public House Sender { get; set; }
        public House Receiver { get; set; }
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
                var world = Sender.World;
                //accept proposal
                Status = ProposalStatus.Accepted;

                //only people for now
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
                var message =
                "PROPOSAL ACCEPTED: \n"
                + "\tSender: " + Sender.Lord.FullNameAndAge + "\n"
                + "\tReciever: " + Receiver.Lord.FullNameAndAge + "\n"
                + "\tOffered People\n";
                OfferedPeople.ForEach(p => message += "\t\t" + p.FullNameAndAge + "\n");
                message += "\tRequested People\n";
                RequestedPeople.ForEach(p => message += "\t\t" + p.FullNameAndAge + "\n");
                Receiver.RecordHistory(message);
                Sender.RecordHistory(message);
            }
        }
        public void Reject()
        {
            //reject proposal
            var world = Sender.World;
            Status = ProposalStatus.Rejected;
            var message =
            "PROPOSAL REJECTED: \n"
            + "\tSender: " + Sender.Lord.FullNameAndAge + "\n"
            + "\tReciever: " + Receiver.Lord.FullNameAndAge + "\n"
            + "\tOffered People\n";
            OfferedPeople.ForEach(p => message += "\t\t" + p.FullNameAndAge + "\n");
            message += "\tRequested People\n";
            RequestedPeople.ForEach(p => message += "\t\t" + p.FullNameAndAge + "\n");
            Receiver.RecordHistory(message);
            Sender.RecordHistory(message);
        }
    }
}
