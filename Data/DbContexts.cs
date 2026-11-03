using Microsoft.EntityFrameworkCore;
using UserManager.Models;

namespace UserManager.Data
{
    /// <summary>
    /// Represents the database context for user management operations.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the AppDbContext class.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Configures the schema needed for the identity framework.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModel>(entity =>
            {
                entity.ToTable("__SNUsers");
                entity.HasKey(u => u.Id); ;
            });

            modelBuilder.Entity<UserMetadata>(entity =>
            {
                entity.ToTable("__SNUserMetadata");
                entity.HasIndex(u => new { u.UserId, u.Key }).IsUnique();
            });

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserModel>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<UserModel>()
                .HasIndex(u => u.UserName)
                .IsUnique();
        }

        /// <summary>
        /// Gets or sets the Users DbSet.
        /// </summary>
        public DbSet<UserModel> Users { get; set; }

        /// <summary>
        /// Gets or sets the UserMetadata DbSet.
        /// </summary>
        public DbSet<UserMetadata> UserMetadata { get; set; }
    }

    /// <summary>
    /// PostgreSQL-specific DbContext variant for migration generation.
    /// </summary>
    public class PostgresAppDbContext : AppDbContext
    {
        /// <summary>
        /// Initializes a new instance of the PostgresAppDbContext class.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public PostgresAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }

    /// <summary>
    /// SQL Server-specific DbContext variant for migration generation.
    /// </summary>
    public class SqlServerAppDbContext : AppDbContext
    {
        /// <summary>
        /// Initializes a new instance of the SqlServerAppDbContext class.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public SqlServerAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }

    /// <summary>
    /// MySQL-specific DbContext variant for migration generation.
    /// </summary>
    public class MySQLAppDbContext : AppDbContext
    {
        /// <summary>
        /// Initializes a new instance of the MySQLAppDbContext class.
        /// </summary>
        /// <param name="options">The options to be used by the DbContext.</param>
        public MySQLAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}