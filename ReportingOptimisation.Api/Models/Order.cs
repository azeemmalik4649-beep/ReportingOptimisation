namespace ReportingOptimisation.Api.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = default!; // Pending, Shipped, Delivered, Cancelled

    public List<OrderItem> Items { get; set; } = new();
}
