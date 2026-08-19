using Bogus;
using ReportingOptimisation.Api.Models;

namespace ReportingOptimisation.Api.Data;

public static class SeedData
{
    // Called once at startup if the DB is empty.
    // Volume matters here: N+1 problems and bad execution plans don't show
    // themselves with 20 rows — we need real scale to see and measure the pain.
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Customers.Any()) return; // already seeded, don't duplicate

        var customerFaker = new Faker<Customer>()
            .RuleFor(c => c.FullName, f => f.Name.FullName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.City, f => f.Address.City())
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2));

        var customers = customerFaker.Generate(2000);
        db.Customers.AddRange(customers);
        db.SaveChanges();

        var categories = new[] { "Electronics", "Books", "Clothing", "Home", "Sports" };
        var productFaker = new Faker<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Category, f => f.PickRandom(categories))
            .RuleFor(p => p.Price, f => f.Random.Decimal(5, 500));

        var products = productFaker.Generate(300);
        db.Products.AddRange(products);
        db.SaveChanges();

        var customerIds = customers.Select(c => c.Id).ToList();
        var productList = products;
        var statuses = new[] { "Pending", "Shipped", "Delivered", "Cancelled" };
        var rnd = new Random();

        // Generate ~12,000 orders with 1-4 items each => ~30k order items
        var orders = new List<Order>();
        for (int i = 0; i < 12000; i++)
        {
            var order = new Order
            {
                CustomerId = customerIds[rnd.Next(customerIds.Count)],
                OrderDate = DateTime.UtcNow.AddDays(-rnd.Next(0, 730)),
                Status = statuses[rnd.Next(statuses.Length)]
            };

            int itemCount = rnd.Next(1, 5);
            for (int j = 0; j < itemCount; j++)
            {
                var product = productList[rnd.Next(productList.Count)];
                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = rnd.Next(1, 6),
                    UnitPriceAtPurchase = product.Price
                });
            }

            orders.Add(order);

            // Batch save every 1000 to avoid one giant change-tracker graph
            if (orders.Count % 1000 == 0)
            {
                db.Orders.AddRange(orders);
                db.SaveChanges();
                orders.Clear();
            }
        }

        if (orders.Count > 0)
        {
            db.Orders.AddRange(orders);
            db.SaveChanges();
        }
    }
}
