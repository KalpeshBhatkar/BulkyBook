using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BulkyBook.Business.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ApplicationDbContext _context;

        public ShoppingCartService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<ShoppingCart?> GetCartByIdAsync(int cartId)
        {
            return await _context.ShoppingCarts.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == cartId);
        }

        public async Task<IEnumerable<ShoppingCart>> GetUserCartItemsAsync(string userId)
        {
            return await _context.ShoppingCarts.Include(c => c.Product).Where(c => c.ApplicationUserId == userId).ToListAsync();
        }

        public async Task<int> GetCartCountAsync(string userId)
        {
            return await _context.ShoppingCarts.Where(c => c.ApplicationUserId == userId).SumAsync(c => c.Count);
        }

        public async Task<ShoppingCart> AddToCartAsync(ShoppingCart cart)
        {
            var existingItem = await _context.ShoppingCarts.Include(c => c.Product).FirstOrDefaultAsync(c => c.ApplicationUserId == cart.ApplicationUserId && c.ProductId == cart.ProductId);

            if (existingItem != null)
            {
                existingItem.Count += cart.Count;
                await _context.SaveChangesAsync();
                return existingItem;
            }
            else
            {
                await _context.ShoppingCarts.AddAsync(cart);
                await _context.SaveChangesAsync();
                return cart;
            }
        }

        public async Task UpdateCartAsync(ShoppingCart cart)
        {
            if(cart.Count <= 0)
            {
                _context.ShoppingCarts.Remove(cart);
            }
            else
            {
                _context.ShoppingCarts.Update(cart);
            }
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartAsync(string userId)
        {
            var cartItems =  await _context.ShoppingCarts.Include(c => c.Product).Where(c => c.ApplicationUserId == userId).ToListAsync();

            if (cartItems.Any())
            {
                _context.ShoppingCarts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
        }
    }
}
