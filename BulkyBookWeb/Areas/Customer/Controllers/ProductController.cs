using BulkyBook.Business.Services;
using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        public async Task<IActionResult> Index()
        {
            return View();
        }
        

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Create")]
        public async Task<IActionResult> CreatePost(Product category)
        {
            if (!string.IsNullOrEmpty(category.Title))
            {
                ModelState.AddModelError("", "Product Name Already Exists.");
            }
            if (ModelState.IsValid)
            {
                await _productService.CreateProductAsync(category);
                TempData["success"] = "Product created successfully.";
                return RedirectToAction("Index");
            }
            return View();
        }



        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllProductsAsync(true);
            return Json(new { data = products });
        }
        #endregion
    }
}
