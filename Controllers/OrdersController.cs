using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;
using HomevoAPI.Data;
using HomevoAPI.DTOs;
using HomevoAPI.Models;

namespace HomevoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // دالة مساعدة لتوليد اسم مرتب من الإيميل أو الـ Username
        private string GetFormattedDisplayName(string? email, string? fallbackName)
        {
            string source = !string.IsNullOrEmpty(email) ? email : fallbackName;
            if (string.IsNullOrEmpty(source)) return "Customer";

            // لو الإيميل موجود، ناخد الجزء اللي قبل علامة @
            if (source.Contains("@"))
            {
                source = source.Split('@')[0];
            }

            // استبدال النقاط أو الشرطات بمسافات وتحويل الحروف الأولى لكابيتال (زي Youssef Ragab)
            string cleaned = source.Replace(".", " ").Replace("_", " ").Replace("-", " ");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLower());
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder(CreateOrderDto createOrderDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (createOrderDto.Items == null || !createOrderDto.Items.Any())
            {
                return BadRequest("Order must contain at least one item.");
            }

            var order = new Order
            {
                UserId = userId,
                OrderNumber = "HV-" + DateTime.UtcNow.Year + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(),
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            decimal totalPrice = 0;
            var orderItemsResponse = new List<OrderItemResponseDto>();

            foreach (var item in createOrderDto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                if (product == null)
                {
                    return BadRequest($"Product with id {item.ProductId} not found.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Not enough stock for {product.Name}.");
                }

                product.StockQuantity -= item.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                order.OrderItems.Add(orderItem);

                totalPrice += product.Price * item.Quantity;

                orderItemsResponse.Add(new OrderItemResponseDto
                {
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }

            order.TotalPrice = totalPrice;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var currentUser = await _context.Users.FindAsync(userId);
            string displayName = GetFormattedDisplayName(currentUser?.Email, currentUser?.UserName);

            return Ok(new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                CustomerName = displayName,
                CustomerEmail = currentUser != null ? currentUser.Email : "",
                Items = orderItemsResponse
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userId == null)
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin") || userRole == "Admin";

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(o => o.UserId == userId);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderDtos = new List<OrderResponseDto>();

            foreach (var o in orders)
            {
                var user = await _context.Users.FindAsync(o.UserId);
                string displayName = GetFormattedDisplayName(user?.Email, user?.UserName);

                orderDtos.Add(new OrderResponseDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    CustomerName = displayName,
                    CustomerEmail = user != null ? user.Email : "No Email",
                    Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                    {
                        ProductName = oi.Product != null ? oi.Product.Name : "",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                });
            }

            return Ok(orderDtos);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrdersForAdmin()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderDtos = new List<OrderResponseDto>();

            foreach (var o in orders)
            {
                var user = await _context.Users.FindAsync(o.UserId);
                string displayName = GetFormattedDisplayName(user?.Email, user?.UserName);

                orderDtos.Add(new OrderResponseDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    CustomerName = displayName,
                    CustomerEmail = user != null ? user.Email : "No Email",
                    Items = o.OrderItems.Select(oi => new OrderItemResponseDto
                    {
                        ProductName = oi.Product != null ? oi.Product.Name : "",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                });
            }

            return Ok(orderDtos);
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest("Status is required.");
            }

            if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var parsedStatus))
            {
                return BadRequest($"Invalid order status: {dto.Status}");
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound($"Order with id {id} not found.");
            }

            order.Status = parsedStatus;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order status updated successfully.",
                id = order.Id,
                status = order.Status.ToString()
            });
        }
    }
}