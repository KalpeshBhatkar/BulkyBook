using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<OrderHeader> CreateOrderAsync(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Add(orderHeader);
            await _context.SaveChangesAsync();

            return orderHeader;
        }
        public async Task<IEnumerable<OrderHeader>> GetAllOrderAsync(string? userId = null, string? status = null, bool includeUser = false, bool includeDetails = false)
        {
            var query = _context.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(o => o.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(o => o.OrderDetails).ThenInclude(od => od.Product);
            }
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(u => u.ApplicationUserId == userId);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(u => u.OrderStatus == status);
            }
            return await query.ToListAsync();
        }
        public async Task<OrderHeader?> GetOrderByIdAsync(int id, bool includeUser = false, bool includeDetails = false)
        {
            var query = _context.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(o => o.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(o => o.OrderDetails).ThenInclude(od => od.Product);
            }
            return await query.FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
