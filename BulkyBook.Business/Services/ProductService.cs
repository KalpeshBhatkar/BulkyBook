using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync(bool includeCategories = false)
        {
            if (includeCategories)
            {
                return await _context.Products.Include(p => p.Category).AsNoTracking().ToListAsync();
            }
            else
            {
                return await _context.Products.AsNoTracking().ToListAsync();
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product category)
        {
            _context.Products.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateProductAsync(Product category)
        {
            _context.Products.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var category = await _context.Products.Where(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null)
            {
                throw new KeyNotFoundException($"Product with Id {id} not found.");
            }
            _context.Products.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
