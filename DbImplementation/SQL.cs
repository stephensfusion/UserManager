using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using UserManager.Data;

namespace UserManager.DbImplementation;

/// <summary>
/// Represents the SQL implementation of the IManageUsers interface.
/// Provides SQL-specific methods for managing users and interacting with the database.
/// </summary>
public class SQL<TRole, TStatus, TPermission, TTheme> : BaseDbImplementation<TRole, TStatus, TPermission, TTheme>
    where TRole : struct, Enum
    where TStatus : struct, Enum
    where TPermission : struct, Enum
    where TTheme : struct, Enum
{
    /// <summary>
    /// Initializes a new instance of the SQL class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    public SQL(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    /// <summary>
    /// Creates a SqlServer-specific context for migration operations.
    /// This ensures that SqlServer migrations are discovered and applied correctly.
    /// </summary>
    /// <returns>The SqlServer-specific context for migrations.</returns>
    protected override AppDbContext CreateMigrationContext()
    {
        // Get connection string from the base context factory
        using var baseContext = _contextFactory.CreateDbContext();
        var connectionString = baseContext.Database.GetConnectionString();

        // Create SqlServer-specific context for proper migration discovery
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new SqlServerAppDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Gets the SQL statement for marking a migration as applied in SQLServer.
    /// </summary>
    /// <returns>The SQLServer-specific SQL statement.</returns>
    protected override string GetMarkMigrationSql()
    {
        return @"IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = @migrationId) INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (@migrationId, @productVersion);";
    }

    /// <summary>
    /// Gets the parameters for marking a migration as applied in SQLServer.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="productVersion">The product version.</param>
    /// <returns>The SqlServer-specific parameters array.</returns>
    protected override object[] GetMarkMigrationParameters(string migrationId, string productVersion)
    {
        return new[]
        {
                new Microsoft.Data.SqlClient.SqlParameter("@migrationId", migrationId),
                new Microsoft.Data.SqlClient.SqlParameter("@productVersion", productVersion)
    };
    }

    /// <summary>
    /// Determines if the exception indicates a table already exists error for SQLServer.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a table already exists error in SQLServer.</returns>
    protected override bool IsTableAlreadyExistsError(Exception ex)
    {
        return ex is SqlException sqlEx && sqlEx.Number == 2714;
    }
}