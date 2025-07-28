using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentProcessing.Application.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Đăng ký Application services
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
