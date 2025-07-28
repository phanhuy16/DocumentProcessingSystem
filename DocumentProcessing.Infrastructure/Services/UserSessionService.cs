using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.Entities;
using DocumentProcessing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentProcessing.Infrastructure.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserSessionService> _logger;

        public UserSessionService(ApplicationDbContext context, ILogger<UserSessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent)
        {
            var session = new UserSession
            {
                UserId = userId,
                SessionId = Guid.NewGuid().ToString(),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<UserSession?> GetSessionAsync(string sessionId)
        {
            return await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
        }

        public async Task UpdateSessionAsync(UserSession session)
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session != null)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteUserSessionsAsync(Guid userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow && s.IsActive)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
        }
    }
}
