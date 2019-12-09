using System;
using System.Collections.Generic;
using System.Text;

namespace PollyLab.Helpers.Contracts
{
    public class Customer
    {
        public Guid CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}
