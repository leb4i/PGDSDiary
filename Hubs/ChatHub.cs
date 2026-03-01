using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GradingSystem.Data;
using GradingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace GradingSystem.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string receiverId, string text)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(receiverId))
                return;

            var message = new Message
            {
                SenderId = senderId!,
                ReceiverId = receiverId,
                Text = text,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Изпрати до получателя
            await Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = senderId,
                text = message.Text,
                sentAt = message.SentAt.ToString("HH:mm")
            });

            // Изпрати обратно до изпращача
            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                senderId = senderId,
                text = message.Text,
                sentAt = message.SentAt.ToString("HH:mm")
            });
        }

        public async Task MarkAsRead(string senderId)
        {
            var receiverId = Context.UserIdentifier;
            var unread = await _context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread) m.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}