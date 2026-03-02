using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GradingSystem.Models;

namespace GradingSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInfo(string firstName, string lastName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = firstName;
            user.LastName = lastName;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Информацията е обновена!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Новата парола не съвпада!";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
                TempData["Success"] = "Паролата е сменена!";
            else
                TempData["Error"] = "Грешна текуща парола!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePicture(IFormFile picture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (picture != null && picture.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{user.Id}{Path.GetExtension(picture.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await picture.CopyToAsync(stream);

                user.ProfilePicture = $"/uploads/profiles/{fileName}";
                await _userManager.UpdateAsync(user);
                TempData["Success"] = "Снимката е обновена!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}