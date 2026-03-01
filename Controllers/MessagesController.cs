using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradingSystem.Data;
using GradingSystem.Models;

namespace GradingSystem.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // Всички разговори
            var conversations = await _context.Messages
                .Where(m => m.SenderId == user!.Id || m.ReceiverId == user!.Id)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .ToListAsync();

            // Групираме по събеседник
            var contacts = conversations
                .GroupBy(m => m.SenderId == user!.Id ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UserName = g.First().SenderId == user!.Id
                        ? g.First().Receiver!.FirstName + " " + g.First().Receiver!.LastName
                        : g.First().Sender!.FirstName + " " + g.First().Sender!.LastName,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First().Text,
                    LastTime = g.OrderByDescending(m => m.SentAt).First().SentAt,
                    Unread = g.Count(m => m.ReceiverId == user!.Id && !m.IsRead)
                })
                .OrderByDescending(c => c.LastTime)
                .ToList();

            // Всички потребители за нов разговор
            var allUsers = await _userManager.Users
                .Where(u => u.Id != user!.Id)
                .ToListAsync();

            ViewBag.Contacts = contacts;
            ViewBag.AllUsers = allUsers;
            ViewBag.CurrentUser = user;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string userId)
        {
            var user = await _userManager.GetUserAsync(User);

            var messages = await _context.Messages
                .Where(m => (m.SenderId == user!.Id && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == user!.Id))
                .OrderBy(m => m.SentAt)
                .Select(m => new {
                    senderId = m.SenderId,
                    text = m.Text,
                    sentAt = m.SentAt.ToString("HH:mm")
                })
                .ToListAsync();

            // Маркирай като прочетени
            var unread = await _context.Messages
                .Where(m => m.SenderId == userId && m.ReceiverId == user!.Id && !m.IsRead)
                .ToListAsync();
            foreach (var m in unread) m.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(messages);
        }

        public async Task<IActionResult> Chat(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            var otherUser = await _userManager.FindByIdAsync(userId);

            if (otherUser == null) return NotFound();

            // Маркираме като прочетени
            var unread = await _context.Messages
                .Where(m => m.SenderId == userId && m.ReceiverId == user!.Id && !m.IsRead)
                .ToListAsync();
            foreach (var m in unread) m.IsRead = true;
            await _context.SaveChangesAsync();

            // Всички съобщения в разговора
            var messages = await _context.Messages
                .Where(m => (m.SenderId == user!.Id && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == user!.Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.OtherUser = otherUser;
            ViewBag.CurrentUser = user;
            ViewBag.Messages = messages;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            var count = await _context.Messages
                .CountAsync(m => m.ReceiverId == user!.Id && !m.IsRead);
            return Json(new { count });
        }
    }
}