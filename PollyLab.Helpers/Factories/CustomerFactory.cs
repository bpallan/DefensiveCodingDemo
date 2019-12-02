using System;
using System.Collections.Generic;
using System.Text;
using Faker;
using PollyLab.Helpers.Contracts;

namespace PollyLab.Helpers.Factories
{
    public class CustomerFactory
    {
        public static Queue<Customer> CreateCustomerQueue(int numberToCreate)
        {
            var result  = new Queue<Customer>();
            for (int i = 0; i < numberToCreate; i++)
            {
                result.Enqueue(new Customer()
                {
                    CustomerId = Guid.NewGuid(),
                    FirstName = Name.First(),
                    LastName = Name.Last(),
                    Email = Internet.Email()
                });
            }

            return result;
        }
    }
}
