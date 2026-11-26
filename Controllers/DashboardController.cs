using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomevoAPI.Data;
using HomevoAPI.DTOs;
using HomevoAPI.Models;

namespace HomevoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalPrice);
            var totalOrders = await _context.Orders.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalCustomers = await _context.Users.CountAsync();

            return Ok(new DashboardSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalProducts = totalProducts,
                TotalCustomers = totalCustomers
            });
        }

        [HttpGet("recent-orders")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetRecentOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            var orderDtos = orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                TotalPrice = o.TotalPrice,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt,
                Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    ProductName = oi.Product != null ? oi.Product.Name : "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            });

            return Ok(orderDtos);
        }

        [HttpGet("order-status-chart")]
        public async Task<ActionResult<IEnumerable<OrderStatusStatDto>>> GetOrderStatusChart()
        {
            var stats = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusStatDto
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(stats);
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<LowStockProductDto>>> GetLowStockProducts()
        {
            var lowStockProducts = await _context.Products
                .Where(p => p.StockQuantity <= 5)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StockQuantity = p.StockQuantity
                })
                .ToListAsync();

            return Ok(lowStockProducts);
        }
    }
}
