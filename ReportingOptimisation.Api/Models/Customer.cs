namespace ReportingOptimisation.Api.Models;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string City { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    // Navigation property: ek customer ke multiple orders ho sakte hain
    public List<Order> Orders { get; set; } = new();
}
