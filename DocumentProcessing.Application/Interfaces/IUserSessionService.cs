using DocumentProcessing.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentProcessing.Application.Interfaces
{
    public interface IUserSessionService
    {
        Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent);
        Task<UserSession?> GetSessionAsync(string sessionId);
        Task UpdateSessionAsync(UserSession session);
        Task DeleteSessionAsync(string sessionId);
        Task DeleteUserSessionsAsync(Guid userId);
        Task CleanupExpiredSessionsAsync();
    }
}
