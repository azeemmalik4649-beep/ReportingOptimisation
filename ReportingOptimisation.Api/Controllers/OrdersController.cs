using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportingOptimisation.Api.Data;

namespace ReportingOptimisation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetOrderCount()
    {
        var count = await _db.Orders.CountAsync();
        return Ok(new { totalOrders = count });
    }

    // GET /api/orders/recent-bad
    // INTENTIONALLY BAD: this triggers the N+1 problem.
    [HttpGet("recent-bad")]
    public async Task<IActionResult> GetRecentOrdersBad()
    {
        var orders = await _db.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(20)
            .ToListAsync();

        var result = new List<object>();

        foreach (var order in orders)
        {
            var customer = await _db.Customers.FindAsync(order.CustomerId);

            result.Add(new
            {
                order.Id,
                order.OrderDate,
                order.Status,
                CustomerName = customer?.FullName
            });
        }

        return Ok(result);
    }
    [HttpGet("recent-good")]
    public async Task<IActionResult> GetRecentOrdersGood()
    {
        var result = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(20)
            .Select(o => new
            {
                o.Id,
                o.OrderDate,
                o.Status,
                CustomerName = o.Customer.FullName
            })
            .ToListAsync();

        return Ok(result);
    }
}