﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DB_Project.Models.ViewModels
{
    public class Customer
    {
        public Customer(DB_Project.Models.Customer customer)
        {
            Id = customer.Id.ToString();
            Name = customer.Name;
            Phone = customer.Phone;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
    }
}
