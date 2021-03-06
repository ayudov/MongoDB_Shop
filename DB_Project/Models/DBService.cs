﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DB_Project.Models
{
    public class DBService
    {
        IMongoCollection<Item> Items { get; set; }
        IMongoCollection<Customer> Customers { get; set; }
        IMongoCollection<Cart> Carts { get; set; }
        IMongoCollection<Order> Orders { get; set; }
        IMongoCollection<Seller> Sellers { get; set; }

        public DBService()
        {
            var client = new MongoClient("mongodb+srv://admin:admin_admin@cluster0.iipqw.mongodb.net/test?retryWrites=true&w=majority");
            var database = client.GetDatabase("Project");
            // обращаемся к коллекции Products
            Items = database.GetCollection<Item>("Item");
            Customers = database.GetCollection<Customer>("Customer");
            Carts = database.GetCollection<Cart>("Cart");
            Orders = database.GetCollection<Order>("Order");
            Sellers = database.GetCollection<Seller>("Seller");
        }

        public Item[] GetItems()
        {
            return Items.Find(new BsonDocument()).ToList().ToArray();
        }

        public Customer[] GetCustomers()
        {
            return Customers.Find(new BsonDocument()).ToList().ToArray();
        }

        public Cart[] GetCarts()
        {
            return Carts.Find(new BsonDocument()).ToList().ToArray();
        }

        public Order[] GetOrders()
        {
            return Orders.Find(new BsonDocument()).ToList().ToArray();
        }

        public Seller[] GetSellers()
        {
            return Sellers.Find(new BsonDocument()).ToList().ToArray();
        }

        public Seller GetSeller(BsonValue id)
        {
            var filter_id = Builders<Seller>.Filter.Eq("_id", id);

            return Sellers.Find(filter_id).FirstOrDefault();
        }

        public Item GetItem(BsonValue id)
        {
            var filter_id = Builders<Item>.Filter.Eq("_id", id);

            return Items.Find(filter_id).FirstOrDefault();
        }

        public Customer GetCustomer(BsonValue id)
        {
            var filter_id = Builders<Customer>.Filter.Eq("_id", id);

            return Customers.Find(filter_id).FirstOrDefault();
        }

        public Cart GetCart(BsonValue id)
        {
            var filter_id = Builders<Cart>.Filter.Eq("_id", id);

            return Carts.Find(filter_id).FirstOrDefault();
        }

        public Order GetOrder(BsonValue id)
        {
            var filter_id = Builders<Order>.Filter.Eq("_id", id);

            return Orders.Find(filter_id).FirstOrDefault();
        }

        public void DeleteItem(string id)
        {
            var filter_id = Builders<Item>.Filter.Eq("_id", ObjectId.Parse(id));

            Items.DeleteOne(filter_id);
        }

        public void PostItem(Item item)
        {
            Items.InsertOne(item);
        }

        public async Task<bool> FillWithFakeData()
        {
            var rand = new Random();

            var customerNames = new string[] { "Alex", "Joe", "Andrew", "Donald", "Jacob", "William", "Matthew", "Logan" };
            var phoneNumbers = new string[] { "+3871246761", "+83764736283", "+847618726378", "+3263467532", "+5646352653", "+7657635732", "+36123612313", "+124432434234" };

            var customers = new List<Customer>();
            for (int i = 0; i < customerNames.Length; i++)
                customers.Add(new Customer { Name = customerNames[i], Phone = phoneNumbers[i] });

            await Customers.InsertManyAsync(customers);

            var itemNames = new string[] { "Washing machine", "Grill", "iPhone", "Apples", "Laptop", "Table", "Chips", "Books" };

            var items = new List<Item>();
            for (int i = 0; i < itemNames.Length; i++)
                items.Add(new Item { Name = itemNames[i], Count = rand.Next(1, 15), Price = rand.NextDouble() * 150 });

            await Items.InsertManyAsync(items);

            var sellerNames = new string[] { "ROZETKA", "Allo", "Foxtrot", "Eldorado", "Comfy" };
            var sellerAddress = new string[] { "Polyova St, 5", "Stecenko St, 24", "Oleny Teligy St, 68", "Obolon Ave, 25", "Peremogy Ave, 54" };

            var sellers = new List<Seller>();
            for (int i = 0; i < sellerNames.Length; i++)
                sellers.Add(new Seller { Name = sellerNames[i], Address = sellerAddress[i], isActive = i % 3 != 2});

            await Sellers.InsertManyAsync(sellers);

            return true;

        }

        public async Task<bool> FillWithReferences()
        {
            var rand = new Random();

            var customers = Customers.Find(new BsonDocument()).ToList();
            var items = Items.Find(new BsonDocument()).ToList();
            var sellers = Sellers.Find(new BsonDocument()).ToList();

            var carts = new List<Cart>();

            for (int i = 0; i < 5; i++)
            {
                var itemRefs = GetRandomItems(items, rand);

                var customerRandom = new Customer();

                do
                {
                    customerRandom = customers[rand.Next(0, customers.Count)];
                } while (carts.Any(c => c.Customer.Id.Equals(customerRandom.Id)));

                carts.Add(new Cart { Customer = new MongoDBRef("Customer", customerRandom.Id), Items = itemRefs });
            }

            await Carts.InsertManyAsync(carts);

            var orders = new List<Order>();

            for (int i = 0; i < 5; i++)
            {
                var itemRefs = GetRandomItems(items, rand);

                orders.Add(new Order { Customer = new MongoDBRef("Customer", customers[rand.Next(0, customers.Count)].Id), Items = itemRefs, IsCompleted = i % 3 == 0, DateTimeStamp = RandomDay(rand) });
            }

            await Orders.InsertManyAsync(orders);

            carts = Carts.Find(new BsonDocument()).ToList();
            foreach(var customer in customers)
            {
                var cartRandom = carts.Where(c => c.Customer.Id.Equals(customer.Id)).SingleOrDefault();
                if (cartRandom == null)
                    continue;

                customer.Cart = new MongoDBRef("Cart", cartRandom.Id);

                var filter = Builders<Customer>.Filter.Eq(x => x.Id, customer.Id);
                await Customers.ReplaceOneAsync(filter, customer, new ReplaceOptions() { IsUpsert = false });
            }

            sellers[0].Items = items.Where((item, index) => index == 0 || index == 1).Select(item => new MongoDBRef("Item", item.Id)).ToArray();
            await UpdateSeller(sellers[0]);
            items[0].Seller = new MongoDBRef("Seller", sellers[0].Id);
            await UpdateItem(items[0]);
            items[1].Seller = new MongoDBRef("Seller", sellers[0].Id);
            await UpdateItem(items[1]);
            sellers[1].Items = items.Where((item, index) => index == 2 || index == 3).Select(item => new MongoDBRef("Item", item.Id)).ToArray();
            await UpdateSeller(sellers[1]);
            items[2].Seller = new MongoDBRef("Seller", sellers[1].Id);
            await UpdateItem(items[2]);
            items[3].Seller = new MongoDBRef("Seller", sellers[1].Id);
            await UpdateItem(items[3]);
            sellers[2].Items = items.Where((item, index) => index == 4 || index == 5).Select(item => new MongoDBRef("Item", item.Id)).ToArray();
            await UpdateSeller(sellers[2]);
            items[4].Seller = new MongoDBRef("Seller", sellers[2].Id);
            await UpdateItem(items[4]);
            items[5].Seller = new MongoDBRef("Seller", sellers[2].Id);
            await UpdateItem(items[5]);
            sellers[3].Items = items.Where((item, index) => index == 6).Select(item => new MongoDBRef("Item", item.Id)).ToArray();
            await UpdateSeller(sellers[3]);
            items[6].Seller = new MongoDBRef("Seller", sellers[3].Id);
            await UpdateItem(items[6]);
            sellers[4].Items = items.Where((item, index) => index == 7).Select(item => new MongoDBRef("Item", item.Id)).ToArray();
            await UpdateSeller(sellers[4]);
            items[7].Seller = new MongoDBRef("Seller", sellers[4].Id);
            await UpdateItem(items[7]);

            return true;
        }

        private MongoDBRef[] GetRandomItems(List<Item> items, Random rand)
        {
            var itemRandomList = new List<Item>();
            var randomCount = rand.Next(0, items.Count);
            for (int j = 0; j < randomCount; j++)
            {
                var pickedItem = items[rand.Next(0, items.Count)];
                if (!itemRandomList.Any(irl => irl.Id.Equals(pickedItem.Id)))
                    itemRandomList.Add(pickedItem);
            }

            var itemRefs = itemRandomList.Select(irl => new MongoDBRef("Item", irl.Id)).ToArray();

            return itemRefs;
        }

        private DateTime RandomDay(Random rand)
        {
            DateTime start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(rand.Next(range));
        }

        private async Task UpdateSeller(Seller seller)
        {
            var filter = Builders<Seller>.Filter.Eq(x => x.Id, seller.Id);
            await Sellers.ReplaceOneAsync(filter, seller, new ReplaceOptions() { IsUpsert = false });
        }

        private async Task UpdateItem(Item item)
        {
            var filter = Builders<Item>.Filter.Eq(x => x.Id, item.Id);
            await Items.ReplaceOneAsync(filter, item, new ReplaceOptions() { IsUpsert = false });
        }
    }
}
