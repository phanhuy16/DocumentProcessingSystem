using DocumentProcessing.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.RefreshToken).IsRequired();
                entity.Property(e => e.Role).IsRequired();
            });

            // Configure ApplicationRole
            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(e => e.Description)
                    .HasMaxLength(200);
            });

            // Configure UserSession
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
                entity.Property(e => e.UserAgent).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasOne(e => e.User).WithMany(u => u.UserSessions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Document (if needed)
            if (modelBuilder.Model.FindEntityType(typeof(Document)) != null)
            {
                modelBuilder.Entity<Document>(entity =>
                {
                    entity.HasOne<ApplicationUser>()
                        .WithMany(u => u.Documents)
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
            }

            // Document configuration
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ExtractedText).HasColumnType("nvarchar(max)");

                // Foreign key relationship
                entity.HasOne(d => d.User)
                      .WithMany(u => u.Documents)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed initial data
            modelBuilder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "admin",
                    Email = "admin@example.com",
                    PasswordHash = "hashed_password_here",
                    FirstName = "Admin",
                    LastName = "User",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Seed roles
            SeedRoles(modelBuilder);
        }
        private void SeedRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Administrator role with full access",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = "Manager",
                    NormalizedName = "MANAGER",
                    Description = "Manager role with limited access",
                    CreatedAt = DateTime.UtcNow
                },
                new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Regular user role",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
