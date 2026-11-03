using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserManager.Data;

/// <summary>
/// Design-time factory for creating MySQLAppDbContext instances during migration operations.
/// </summary>
public class MySQLAppDbContextFactory : IDesignTimeDbContextFactory<MySQLAppDbContext>
{
    /// <summary>
    /// Creates a new instance of MySQLAppDbContext for design-time operations.
    /// </summary>
    public MySQLAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = "Server=localhost;Database=user_manager_mysql;User=root;Password=35688410;";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new MySQLAppDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Design-time factory for creating PostgresAppDbContext instances during migration operations.
/// </summary>
public class PostgresAppDbContextFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    /// <summary>
    /// Creates a new instance of PostgresAppDbContext for design-time operations.
    /// </summary>
    public PostgresAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = "Host=localhost;Port=5432;Database=user_manager_pg;Username=postgres;Password=35688410";
        optionsBuilder.UseNpgsql(connectionString);
        return new PostgresAppDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Design-time factory for creating SqlServerAppDbContext instances during migration operations.
/// </summary>
public class SqlServerAppDbContextFactory : IDesignTimeDbContextFactory<SqlServerAppDbContext>
{
    /// <summary>
    /// Creates a new instance of SqlServerAppDbContext for design-time operations.
    /// </summary>
    public SqlServerAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = "Server=localhost;Database=user_manager_sql;Trusted_Connection=True;TrustServerCertificate=True;";
        optionsBuilder.UseSqlServer(connectionString);
        return new SqlServerAppDbContext(optionsBuilder.Options);
    }
}