using BulkyBook.Business.Services;
using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        public ProductController(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }
        public async Task<IActionResult> Index()
        {
            return View();
        }


        public async Task<IActionResult> Upsert()
        {
            IEnumerable<SelectListItem> categoryList = (await _categoryService.GetAllCategoriesAsync())
                .Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
            ViewData["categoryList"] = categoryList;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Upsert")]
        public async Task<IActionResult> UpsertPost(Product category)
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
