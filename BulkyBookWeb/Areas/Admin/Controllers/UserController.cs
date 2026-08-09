using BulkyBook.Business.Services;
using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    public class UserController : Controller
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(IApplicationUserService applicationUserService, UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _applicationUserService = applicationUserService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> RoleManagement(string userId)
        {
            var user = await _applicationUserService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            RoleManagementVM RoleVm = new()
            {
                ApplicationUser = user,
                RoleList = _roleManager.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Name
                })
            };
            RoleVm.ApplicationUser.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            return View(RoleVm);
        }

        [HttpPost]
        public async Task<IActionResult> RoleManagement(RoleManagementVM roleManagementVM)
        {
            var user = await _applicationUserService.GetUserByIdAsync(roleManagementVM.ApplicationUser.Id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }
            string oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            if (roleManagementVM.ApplicationUser.Role != oldRole)
            {
                await _userManager.RemoveFromRoleAsync(user, oldRole);
                await _userManager.AddToRoleAsync(user, roleManagementVM.ApplicationUser.Role);
            }
            TempData["success"] = "Role has been updated";
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> ChangePassword(string userId)
        {
            var user = await _applicationUserService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            AdminChangePasswordVM adminChangePasswordVM = new()
            {
                UserEmail = user.Email,
                UserId = user.Id
            };

            return View(adminChangePasswordVM);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordVM adminChangePasswordVM)
        {
            if (!ModelState.IsValid)
            {
                return View(adminChangePasswordVM);
            }

            var user = await _applicationUserService.GetUserByIdAsync(adminChangePasswordVM.UserId);
            if (user == null)
            {
                return NotFound();
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, adminChangePasswordVM.NewPassword);

            if (result.Succeeded)
            {
                TempData["success"] = $"Password for {user.Email} has been changed successfully.";
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            adminChangePasswordVM.UserEmail = user.Email;
            return View(adminChangePasswordVM);

        }
        #region API Calls
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var users = await _applicationUserService.GetAllUsersAsync();
            foreach (var user in users)
            {
                user.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            }
            return Json(new { data = users });
        }

        [HttpPost]
        //[AllowAnonymous]
        public async Task<IActionResult> LockUnlock([FromBody] string userId)
        {
            var user = await _applicationUserService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }
            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
                return Json(new { success = true, message = "User unlocked successfully" });
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(1000));
                return Json(new { success = true, message = "User locked successfully" });
            }
        }

        #endregion
    }
}
