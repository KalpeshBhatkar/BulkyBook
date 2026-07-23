using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Create")]
        public async Task<IActionResult> CreatePost(Category category)
        {
            if (!string.IsNullOrEmpty(category.Name) && !await _categoryService.IsCategoryNameUniqueAsync(category.Name))
            {
                ModelState.AddModelError("", "Category Name Already Exists.");
            }
            if (ModelState.IsValid)
            {
                await _categoryService.CreateCategoryAsync(category);
                TempData["success"] = "Category created successfully.";
                return RedirectToAction("Index");
            }
            return View();
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var category = await _categoryService.GetCategoryByIdAsync(id.Value);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Update")]
        public async Task<IActionResult> UpdatePost(Category category)
        {
            if (!string.IsNullOrEmpty(category.Name) && !await _categoryService.IsCategoryNameUniqueAsync(category.Name, category.Id))
            {
                ModelState.AddModelError("", "Category Name Already Exists.");
            }
            if (ModelState.IsValid)
            {
                await _categoryService.UpdateCategoryAsync(category);
                TempData["success"] = "Category updated successfully.";
                return RedirectToAction("Index");
            }
            return View();
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var category = await _categoryService.GetCategoryByIdAsync(id.Value);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            await _categoryService.DeleteCategoryAsync(id.Value);
            TempData["success"] = "Category deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
