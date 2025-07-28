using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace DocumentProcessing.API.Helpers
{
    public static class AuthHelper
    {
        // Helper methods
        public static string GetIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
                ipAddress = context.Request.Headers["X-Forwarded-For"];
            else if (context.Request.Headers.ContainsKey("X-Real-IP"))
                ipAddress = context.Request.Headers["X-Real-IP"];

            return ipAddress ?? "Unknown";
        }

        public static Guid GetCurrentUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        public static string GetSessionId(HttpContext context)
        {
            return context.Request.Cookies["SessionId"] ?? string.Empty;
        }
    }
}
