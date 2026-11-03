using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UserManager.Data;

namespace UserManager.DbImplementation;

/// <summary>
/// Represents the MySQL implementation of the IManageUsers interface.
/// Provides MySQL-specific methods for managing users and interacting with the database.
/// </summary>
public class MySQL<TRole, TStatus, TPermission, TTheme> : BaseDbImplementation<TRole, TStatus, TPermission, TTheme>
    where TRole : struct, Enum
    where TStatus : struct, Enum
    where TPermission : struct, Enum
    where TTheme : struct, Enum
{
    /// <summary>
    /// Initializes a new instance of the MySQL class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    public MySQL(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    /// <summary>
    /// Creates a MySQL-specific context for migration operations.
    /// This ensures that MySQL migrations are discovered and applied correctly.
    /// </summary>
    /// <returns>The MySQL-specific context for migrations.</returns>
    protected override AppDbContext CreateMigrationContext()
    {
        // Get connection string from the base context factory
        using var baseContext = _contextFactory.CreateDbContext();
        var connectionString = baseContext.Database.GetConnectionString();

        // Create MySQL-specific context for proper migration discovery
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new MySQLAppDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Gets the SQL statement for marking a migration as applied in MySQL.
    /// </summary>
    /// <returns>The MySQL-specific SQL statement.</returns>
    protected override string GetMarkMigrationSql()
    {
        return @"INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES (@migrationId, @productVersion);";
    }

    /// <summary>
    /// Gets the parameters for marking a migration as applied in MySQL.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="productVersion">The product version.</param>
    /// <returns>The MySQL-specific parameters array.</returns>
    protected override object[] GetMarkMigrationParameters(string migrationId, string productVersion)
    {
        return new[]
                {
                new MySqlConnector.MySqlParameter("@migrationId", migrationId),
                new MySqlConnector.MySqlParameter("@productVersion", productVersion)
            };
    }

    /// <summary>
    /// Determines if the exception indicates a table already exists error for MySQL.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a table already exists error in MySQL.</returns>
    protected override bool IsTableAlreadyExistsError(Exception ex)
    {
        return ex is SqlException sqlEx && sqlEx.Number == 2714;
    }
}