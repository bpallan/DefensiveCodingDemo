using System;
using System.Collections.Generic;
using System.Text;
using Faker;
using PollyLab.Helpers.Contracts;

namespace PollyLab.Helpers.Factories
{
    public class CustomerFactory
    {
        public static IEnumerable<Customer> CreateCustomers(int numberToCreate)
        {
            for (int i = 0; i < numberToCreate; i++)
            {
                yield return new Customer()
                {
                    CustomerId = Guid.NewGuid(),
                    FirstName = Name.First(),
                    LastName = Name.Last(),
                    Email = Internet.Email()
                };
            }
        }
    }
}
