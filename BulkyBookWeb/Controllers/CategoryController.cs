using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers
{
    public class CategoryController : Controller
    {

        public CategoryController()
        {
            
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
