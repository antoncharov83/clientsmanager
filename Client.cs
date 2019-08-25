using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientsManager
{
    public class Client
    {
        public Client() { }
        public Client(string name, string contacts, string status, string court, string number, string lawer, string result, DateTime date)
        {
            Name = name;
            Contacts = contacts;
            Status = status;
            Court = court;
            Number = number;
            Lawer = lawer;
            Result = result;
            DateCourt = date;
        }    

        public string Name { get; set; }
        public string Contacts { get; set; }
        public string Status { get; set; }
        public string Court { get; set; }
        public string Number { get; set; }
        public string Lawer { get; set; }
        public string Result { get; set; }
        public DateTime DateCourt { get; set; }
    }
}
