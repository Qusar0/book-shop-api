using AutoMapper;
using BookShopAPI.Dto.Order;
using BookShopAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookShopAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly BookShopContext _db;
        private readonly IMapper _mapper;

        public OrderController(BookShopContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var userEmail = User.Identity.Name;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Order> ordersQuery = _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                    .ThenInclude(b => b.Author)
                .AsQueryable();

            if (userRole == "Customer")
            {
                var customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                if (customer == null)
                    return NotFound("Customer not found");

                ordersQuery = ordersQuery.Where(o => o.CustomerId == customer.CustomerId);
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var orderDtos = orders.Select(order => new OrderResponseDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = $"{order.Customer.FirstName} {order.Customer.LastName}",
                CustomerEmail = order.Customer.Email,
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                StatusId = order.StatusId,
                StatusName = order.Status.Name,
                TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice),
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    AuthorName = $"{oi.Book.Author.FirstName} {oi.Book.Author.LastName}",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            }).ToList();

            return Ok(orderDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var userEmail = User.Identity.Name;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var order = await _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                    .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound("Order not found");

            if (userRole == "Customer")
            {
                var customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                if (customer == null || order.CustomerId != customer.CustomerId)
                    return Forbid("You can only view your own orders");
            }

            var orderDto = new OrderResponseDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = $"{order.Customer.FirstName} {order.Customer.LastName}",
                CustomerEmail = order.Customer.Email,
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                StatusId = order.StatusId,
                StatusName = order.Status.Name,
                TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice),
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    AuthorName = $"{oi.Book.Author.FirstName} {oi.Book.Author.LastName}",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

            return Ok(orderDto);
        }

        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetOrdersByCustomer(int customerId)
        {
            var orders = await _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                    .ThenInclude(b => b.Author)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (!orders.Any())
                return NotFound("No orders found for this customer");

            var orderDtos = orders.Select(order => new OrderResponseDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = $"{order.Customer.FirstName} {order.Customer.LastName}",
                CustomerEmail = order.Customer.Email,
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                StatusId = order.StatusId,
                StatusName = order.Status.Name,
                TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice),
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    AuthorName = $"{oi.Book.Author.FirstName} {oi.Book.Author.LastName}",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            }).ToList();

            return Ok(orderDtos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto createOrderDto)
        {
            var userEmail = User.Identity.Name;
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            if (customer == null)
                return NotFound("Customer not found");

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var itemDto in createOrderDto.OrderItems)
            {
                var book = await _db.Books
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.BookId == itemDto.BookId);

                if (book == null)
                    return BadRequest($"Book with ID {itemDto.BookId} not found");

                if (book.IsAvailable == false)
                    return BadRequest($"Book '{book.Title}' is not available");

                if (book.StockQuantity < itemDto.Quantity)
                    return BadRequest($"Not enough stock for book '{book.Title}'. Available: {book.StockQuantity}");

                var orderItem = new OrderItem
                {
                    BookId = itemDto.BookId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = book.Price
                };

                orderItems.Add(orderItem);
                totalAmount += itemDto.Quantity * book.Price;

                book.StockQuantity -= itemDto.Quantity;
                if (book.StockQuantity == 0)
                    book.IsAvailable = false;
            }

            var order = new Order
            {
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = createOrderDto.ShippingAddress,
                StatusId = 1,
                OrderItems = orderItems
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            var createdOrder = await _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Status)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Book)
                    .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

            var orderResponse = new OrderResponseDto
            {
                OrderId = createdOrder.OrderId,
                CustomerId = createdOrder.CustomerId,
                CustomerName = $"{createdOrder.Customer.FirstName} {createdOrder.Customer.LastName}",
                CustomerEmail = createdOrder.Customer.Email,
                OrderDate = createdOrder.OrderDate,
                ShippingAddress = createdOrder.ShippingAddress,
                StatusId = createdOrder.StatusId,
                StatusName = createdOrder.Status.Name,
                TotalAmount = totalAmount,
                OrderItems = createdOrder.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    BookId = oi.BookId,
                    BookTitle = oi.Book.Title,
                    AuthorName = $"{oi.Book.Author.FirstName} {oi.Book.Author.LastName}",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, orderResponse);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userEmail = User.Identity.Name;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound("Order not found");

            if (userRole == "Customer")
            {
                var customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Email == userEmail);

                if (customer == null || order.CustomerId != customer.CustomerId)
                    return Forbid("You can only delete your own orders");

                if (order.StatusId != 1)
                    return BadRequest("You can only delete new orders");
            }

            foreach (var orderItem in order.OrderItems)
            {
                var book = await _db.Books.FindAsync(orderItem.BookId);
                if (book != null)
                {
                    book.StockQuantity += orderItem.Quantity;
                    if (book.IsAvailable == false)
                        book.IsAvailable = true;
                }
            }
            _db.OrderItems.RemoveRange(order.OrderItems);
            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Order deleted successfully" });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateDto)
        {
            var order = await _db.Orders
                .Include(o => o.Status)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound("Order not found");

            var status = await _db.Statuses.FindAsync(updateDto.StatusId);
            if (status == null)
                return BadRequest("Invalid status ID");

            order.StatusId = updateDto.StatusId;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Order status updated successfully",
                orderId = order.OrderId,
                newStatus = status.Name
            });
        }
    }
}
