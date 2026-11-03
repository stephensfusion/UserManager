using Microsoft.EntityFrameworkCore;
using NSCore.DatabaseContext;
using NSCore.Models.Enums;
using NSCore.SearchOption;
using NSCore.Validation;
using NSEncryption;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security;
using UserManager.Data;
using UserManager.Dtos;
using UserManager.Enumerations;
using UserManager.Interfaces;
using UserManager.Models;

namespace UserManager.DbImplementation;

/// <summary>
/// Base implementation class for user management operations.
/// Contains common logic shared across all database providers.
/// </summary>
public abstract class BaseDbImplementation<TRole, TStatus, TPermission, TTheme> : INsContextInit,
    IManageUsers<TRole, TStatus, TPermission, TTheme>, IDisposable
    where TRole : struct, Enum
    where TStatus : struct, Enum
    where TPermission : struct, Enum
    where TTheme : struct, Enum
{
    protected bool _disposed = false;
    protected readonly IDbContextFactory<AppDbContext> _contextFactory;

    #region Compiled Queries
    // Compiled queries for frequently executed operations
    protected static readonly Func<AppDbContext, int, bool, Task<UserModel?>> GetUserByIdQuery =
        EF.CompileAsyncQuery((AppDbContext context, int userId, bool includeMetadata) =>
            context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = includeMetadata ? u.Metadata != null ? u.Metadata.Select(m => new UserMetadata
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Key = m.Key,
                        Value = m.Value
                    }).ToList() : new List<UserMetadata>() : new List<UserMetadata>()
                })
                .FirstOrDefault());


    protected static readonly Func<AppDbContext, string, bool, Task<UserModel?>> GetUserByEmailQuery =
        EF.CompileAsyncQuery((AppDbContext context, string email, bool includeMetadata) =>
            context.Users.AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = includeMetadata ? u.Metadata != null ? u.Metadata.Select(m => new UserMetadata
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Key = m.Key,
                        Value = m.Value
                    }).ToList() : new List<UserMetadata>() : new List<UserMetadata>()
                })
                .FirstOrDefault());


    protected static readonly Func<AppDbContext, string, bool, Task<UserModel?>> GetUserByUsernameQuery =
        EF.CompileAsyncQuery((AppDbContext context, string userName, bool includeMetadata) =>
            context.Users.AsNoTracking()
                .Where(u => u.UserName == userName)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = includeMetadata ? u.Metadata != null ? u.Metadata.Select(m => new UserMetadata
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        Key = m.Key,
                        Value = m.Value
                    }).ToList() : new List<UserMetadata>() : new List<UserMetadata>()
                })
                .FirstOrDefault());

    protected static readonly Func<AppDbContext, List<int>, bool, Task<List<UserModel>>> GetUsersByIdsQuery =
        EF.CompileAsyncQuery((AppDbContext context, List<int> userIds, bool includeMetadata) =>
           context.Users
                .AsNoTracking()
                    .Include(u => u.Metadata)
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new UserModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        DisplayName = u.DisplayName,
                        Email = u.Email,
                        UserName = u.UserName,
                        Contact = u.Contact,
                        Status = u.Status,
                        Role = u.Role,
                        Theme = u.Theme,
                        Permission = u.Permission,
                        ProfilePicture = u.ProfilePicture,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        Metadata = includeMetadata ? u.Metadata != null ? u.Metadata.Select(m => new UserMetadata
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Key = m.Key,
                            Value = m.Value
                        }).ToList() : new List<UserMetadata>() : new List<UserMetadata>()
                    }).ToList());

    protected static readonly Func<AppDbContext, string, Task<bool>> EmailExistsQuery =
            EF.CompileAsyncQuery((AppDbContext context, string email) =>
                context.Users.Any(u => u.Email.ToLower() == email.ToLower()));

    protected static readonly Func<AppDbContext, string, Task<bool>> UsernameExistsQuery =
        EF.CompileAsyncQuery((AppDbContext context, string userName) =>
            context.Users.Any(u => u.UserName.ToLower() == userName.ToLower()));

    protected static readonly Func<AppDbContext, int, int, IAsyncEnumerable<UserModel>> GetPagedUsersQuery =
        EF.CompileAsyncQuery((AppDbContext context, int skip, int take) =>
            context.Users.AsNoTracking()
                .OrderBy(u => u.Id)
                .Skip(skip)
                .Take(take)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = new List<UserMetadata>()
                }));

    protected static readonly Func<AppDbContext, Task<int>> GetTotalUsersCountQuery =
        EF.CompileAsyncQuery((AppDbContext context) =>
            context.Users.Count());

    protected static readonly Func<AppDbContext, int, Task<bool>> UserExistsByIdQuery =
        EF.CompileAsyncQuery((AppDbContext context, int userId) =>
            context.Users.Any(u => u.Id == userId));
    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the BaseDbImplementation class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    protected BaseDbImplementation(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        InitializeConnection();
    }

    /// <summary>
    /// Initializes the database connection. Can be overridden by derived classes.
    /// </summary>
    protected virtual void InitializeConnection()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            if (!context.Database.CanConnect())
            {
                throw new InvalidOperationException("Unable to connect to the database. Please check the connection string and database server.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize the database context.", ex);
        }
    }
    #endregion

    #region Helper Methods Using Compiled Queries
    /// <summary>
    /// Checks if a user exists by email using compiled query.
    /// </summary>
    /// <param name="email">The email to check.</param>
    /// <returns>True if user exists, false otherwise.</returns>
    protected async Task<bool> UserExistsByEmailAsync(string email)
    {
        using var context = _contextFactory.CreateDbContext();
        return await EmailExistsQuery(context, email);
    }

    /// <summary>
    /// Checks if a user exists by username using compiled query.
    /// </summary>
    /// <param name="userName">The username to check.</param>
    /// <returns>True if user exists, false otherwise.</returns>
    protected async Task<bool> UserExistsByUsernameAsync(string userName)
    {
        using var context = _contextFactory.CreateDbContext();
        return await UsernameExistsQuery(context, userName);
    }

    /// <summary>
    /// Checks if a user exists by ID using compiled query.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if user exists, false otherwise.</returns>
    protected async Task<bool> UserExistsByIdAsync(int userId)
    {
        using var context = _contextFactory.CreateDbContext();
        return await UserExistsByIdQuery(context, userId);
    }
    #endregion

    #region Migration Methods
    /// <inheritdoc/>
    /// <summary>
    /// Creates a database context for migration operations.
    /// Can be overridden by derived classes to provide database-specific contexts.
    /// </summary>
    /// <returns>The database context to use for migrations.</returns>
    protected virtual AppDbContext CreateMigrationContext()
    {
        return _contextFactory.CreateDbContext();
    }

    /// <summary>
    /// Applies migrations to the database asynchronously.
    /// This method uses the database-specific context for proper migration discovery.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = CreateMigrationContext();
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                throw new InvalidOperationException("Database doesn't exist. Please create and configure it first.");
            }

            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
                await context.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (IsTableAlreadyExistsError(ex))
        {
            await HandleMigrationConflictAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception($"Migration failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Determines if the exception indicates a table already exists error.
    /// Can be overridden by derived classes for database-specific error handling.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a table already exists error.</returns>
    protected virtual bool IsTableAlreadyExistsError(Exception ex)
    {
        // Base implementation - override in derived classes for specific database error codes
        return false;
    }

    /// <summary>
    /// Marks a migration as applied in the database.
    /// Can be overridden by derived classes for database-specific implementation.
    /// </summary>
    /// <param name="migrationId">The migration ID to mark as applied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    protected virtual async Task MarkMigrationAsAppliedAsync(string migrationId, CancellationToken cancellationToken)
    {
        using var context = _contextFactory.CreateDbContext();
        var provider = context.Database.ProviderName;
        var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "8.0.0";

        // This is a generic implementation - override in derived classes for database-specific SQL
        var rawQuery = GetMarkMigrationSql();
        var parameters = GetMarkMigrationParameters(migrationId, productVersion);

        await context.Database.ExecuteSqlRawAsync(rawQuery, parameters, cancellationToken);
    }

    /// <summary>
    /// Gets the SQL statement for marking a migration as applied.
    /// Must be overridden by derived classes.
    /// </summary>
    /// <returns>The SQL statement.</returns>
    protected abstract string GetMarkMigrationSql();

    /// <summary>
    /// Gets the parameters for marking a migration as applied.
    /// Must be overridden by derived classes.
    /// </summary>
    /// <param name="migrationId">The migration ID.</param>
    /// <param name="productVersion">The product version.</param>
    /// <returns>The parameters array.</returns>
    protected abstract object[] GetMarkMigrationParameters(string migrationId, string productVersion);

    private async Task HandleMigrationConflictAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var allMigrations = context.Database.GetMigrations().ToList();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            var latestMigration = allMigrations.LastOrDefault();

            if (latestMigration != null && pendingMigrations.Contains(latestMigration))
            {
                foreach (var migration in pendingMigrations)
                {
                    await MarkMigrationAsAppliedAsync(migration, cancellationToken);
                }
            }
            else
            {
                throw new InvalidOperationException("Could not resolve conflict: No valid latest migration found.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to resolve migration conflict: {ex.Message}", ex);
        }
    }
    #endregion

    /// <inheritdoc/>
    public virtual bool IsContextCreated()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            return context.Database.CanConnect();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to check if the context is created.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<UserModel>> FetchUsersAsync(int from = 1, int pageSize = 10, bool includeMetadata = false)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var query = context.Users.AsNoTracking();

            if (includeMetadata)
            {
                query = query.Include(u => u.Metadata);
            }

            var users = await query
                .OrderBy(u => u.Id)
                .Skip(from)
                .Take(pageSize)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = includeMetadata && u.Metadata != null
                        ? u.Metadata.Select(m => new UserMetadata
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Key = m.Key,
                            Value = m.Value
                        }).ToList()
                        : new List<UserMetadata>()
                }).ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while fetching users.", ex);
        }
    }

    ///<inheritdoc/>
    public virtual async Task<bool> UserExistsAsync(string email, string name = "")
    {
        if (await UserExistsByEmailAsync(email))
        {
            return true;
        }
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (await UserExistsByUsernameAsync(name))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> AddUserAsync(CreateUserDto userDto)
    {
        // Check for existing users using compiled queries (fast check first)
        if (await UserExistsByEmailAsync(userDto.Email))
        {
            throw new ValidationException($"User with email {userDto.Email} already exists.");
        }

        if (await UserExistsByUsernameAsync(userDto.UserName))
        {
            throw new ValidationException($"User with username {userDto.UserName} already exists.");
        }


        using var context = _contextFactory.CreateDbContext();
        using var transaction = await context.Database.BeginTransactionAsync();

        var hasher = new Hasher(12);
        var user = new UserModel
        {
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Email = userDto.Email.Trim(),
            UserName = userDto.UserName,
            Password = string.IsNullOrWhiteSpace(userDto.Password) ? string.Empty : hasher.GenerateHashPassword(userDto.Password),
            Contact = userDto.Contact,
            Permission = InputParser.ParseEnumOrDefault<TPermission>(userDto.Permission, true).ToString(),
            Role = InputParser.ParseEnumOrDefault<TRole>(userDto.Role, true).ToString(),
            Status = InputParser.ParseEnumOrDefault<TStatus>(userDto.Status, true).ToString(),
            Theme = InputParser.ParseEnumOrDefault<TTheme>(userDto.Theme, true).ToString(),
            ProfilePicture = userDto.ProfilePicture,
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = userDto.Metadata?.Count > 0
           ? userDto.Metadata
               .GroupBy(m => m.Key)
               .Select(g => g.First())
               .Select(m => new UserMetadata { Key = m.Key, Value = m.Value })
               .ToList()
           : new List<UserMetadata>()
        };
        user.SetDisplayName();

        try
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return user;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Failed to add user due to a database error.", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("An unexpected error occurred while adding user.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<UserModel>> AddUserAsync(List<CreateUserDto> userDtos)
    {
        var duplicateEmails = userDtos
            .GroupBy(dto => dto.Email.Trim().ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        var duplicateUserNames = userDtos
            .GroupBy(dto => dto.UserName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        if (duplicateEmails.Count() > 0 || duplicateUserNames.Count() > 0)
        {
            var errors = new List<string>();
            if (duplicateEmails.Count() > 0)
                errors.Add($"Duplicate emails in input: {string.Join(", ", duplicateEmails)}");
            if (duplicateUserNames.Count() > 0)
                errors.Add($"Duplicate usernames in input: {string.Join(", ", duplicateUserNames)}");
            throw new ArgumentException($"Validation failed: {string.Join("; ", errors)}", nameof(userDtos));
        }

        var emails = userDtos.Select(dto => dto.Email.Trim().ToLower()).Distinct();
        var userNames = userDtos.Select(dto => dto.UserName).Distinct();
        using var context = _contextFactory.CreateDbContext();
        var existingUsers = context.Users
            .AsNoTracking()
            .Where(u => emails.Contains(u.Email.ToLower()) || userNames.Contains(u.UserName))
            .Select(u => new { u.Email, u.UserName });

        if (existingUsers.Count() > 0)
        {
            var existingEmails = existingUsers.Select(u => u.Email.ToLower());
            var existingUserNames = existingUsers.Select(u => u.UserName);
            var errors = new List<string>();
            if (existingEmails.Count(e => emails.Contains(e)) > 0)
                errors.Add($"Emails already exist: {string.Join(", ", emails.Where(e => existingEmails.Contains(e)))}");
            if (existingUserNames.Count(u => userNames.Contains(u)) > 0)
                errors.Add($"Usernames already exist: {string.Join(", ", userNames.Where(u => existingUserNames.Contains(u)))}");
            throw new InvalidOperationException($"Cannot add users: {string.Join("; ", errors)}");
        }

        using var transaction = await context.Database.BeginTransactionAsync();

        var hasher = new Hasher(12);
        var usersToAdd = new List<UserModel>();

        foreach (var userDto in userDtos)
        {
            var user = new UserModel
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email.Trim(),
                UserName = userDto.UserName,
                Password = string.IsNullOrWhiteSpace(userDto.Password) ? string.Empty : hasher.GenerateHashPassword(userDto.Password),
                Contact = userDto.Contact,
                Permission = InputParser.ParseEnumOrDefault<TPermission>(userDto.Permission, true).ToString(),
                Role = InputParser.ParseEnumOrDefault<TRole>(userDto.Role, true).ToString(),
                Status = InputParser.ParseEnumOrDefault<TStatus>(userDto.Status, true).ToString(),
                Theme = InputParser.ParseEnumOrDefault<TTheme>(userDto.Theme, true).ToString(),
                ProfilePicture = userDto.ProfilePicture,
                CreatedAt = DateTimeOffset.UtcNow,
                Metadata = userDto.Metadata?.Count > 0
                    ? userDto.Metadata
                        .GroupBy(m => m.Key)
                        .Select(g => g.First())
                        .Select(m => new UserMetadata
                        {
                            Key = m.Key,
                            Value = m.Value,
                        })
                        .ToList()
                    : new List<UserMetadata>()
            };
            user.SetDisplayName();

            usersToAdd.Add(user);
        }

        try
        {
            context.Users.AddRange(usersToAdd);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return usersToAdd;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Failed to add users due to a database error.", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("An unexpected error occurred while adding users.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<(List<UserModel> Users, int TotalCount)> SearchUsersAsync(
    string? searchTerm = null,
    string? role = null,
    string? status = null,
    string sortBy = "FirstName",
    bool ascending = true,
    int pageNumber = 1,
    int pageSize = 10,
    SearchMode searchMode = SearchMode.Contains,
    bool includeMetadata = false)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var query = context.Users.AsNoTracking();

            if (includeMetadata)
            {
                query = query.Include(u => u.Metadata);
            }

            // Generic search
            query = query.ApplySearch(searchTerm, searchMode, u => u.FirstName, u => u.LastName, u => u.Email, u => u.UserName);

            // Optional filters with validation
            if (!string.IsNullOrWhiteSpace(role))
            {
                var parsedRole = InputParser.ParseEnum<TRole>(role, true);
                query = query.Where(u => u.Role == parsedRole.ToString());
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsedStatus = InputParser.ParseEnum<TStatus>(status, true);
                query = query.Where(u => u.Status == parsedStatus.ToString());
            }

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "firstname" => ascending ? query.OrderBy(u => u.FirstName) : query.OrderByDescending(u => u.FirstName),
                "lastname" => ascending ? query.OrderBy(u => u.LastName) : query.OrderByDescending(u => u.LastName),
                "email" => ascending ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                "username" => ascending ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName),
                "createdat" => ascending ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
                "updatedat" => ascending ? query.OrderBy(u => u.UpdatedAt) : query.OrderByDescending(u => u.UpdatedAt),
                _ => ascending ? query.OrderBy(u => u.FirstName) : query.OrderByDescending(u => u.FirstName)
            };

            // Apply pagination
            var skip = (pageNumber - 1) * pageSize;
            var users = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = includeMetadata && u.Metadata != null
                        ? u.Metadata.Select(m => new UserMetadata
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Key = m.Key,
                            Value = m.Value
                        }).ToList()
                        : new List<UserMetadata>()
                })
                .ToListAsync();

            return (users, totalCount);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while searching users.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeleteUserAsync(int userId)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            var user = await context.Users
                .Include(u => u.Metadata)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID {userId} not found.");

            // Remove user metadata first
            if (user.Metadata?.Count > 0)
            {
                context.UserMetadata.RemoveRange(user.Metadata);
            }

            // Remove the user
            context.Users.Remove(user);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to delete user due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while deleting user.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<UserModel>> DeleteUserAsync(List<int> userIds)
    {
        var validUserIds = userIds.Distinct().ToList();
        try
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            var users = context.Users
                .Include(u => u.Metadata)
                .Where(u => validUserIds.Contains(u.Id))
                .ToList();

            if (users.Count() == 0)
                throw new InvalidOperationException("No users found with the provided IDs.");

            // Remove all metadata first
            var allMetadata = users.SelectMany(u => u.Metadata ?? new List<UserMetadata>());
            if (allMetadata.Count() > 0)
            {
                context.UserMetadata.RemoveRange(allMetadata);
            }

            // Remove all users
            context.Users.RemoveRange(users);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return users;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to delete users due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while deleting users.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel?> GetUserAsync(int userId, bool includeMetadata = false)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            return await GetUserByIdQuery(context, userId, includeMetadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving user.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> GetUsersByIdsAsync(List<int> userIds, bool includeMetadata = false)
    {
        var validUserIds = userIds.Distinct().ToList();

        try
        {
            using var context = _contextFactory.CreateDbContext();
            return await GetUsersByIdsQuery(context, validUserIds, includeMetadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occured while retrieving users by IDs.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel?> GetUserByEmailAsync(string email, bool includeMetadata = false)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            return await GetUserByEmailQuery(context, email, includeMetadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving user by email.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel?> GetUserByUsernameAsync(string username, bool includeMetadata = false)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();

            return await GetUserByUsernameQuery(context, username, includeMetadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving user by username.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<UserModel>> GetAllUsersAsync(
    int pageNumber = 1,
    int pageSize = 10,
    bool includeMetadata = false)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();

            var query = context.Users.AsNoTracking();

            if (includeMetadata)
            {
                query = query.Include(u => u.Metadata);
            }

            var skip = (pageNumber - 1) * pageSize;

            var users = await query
                .OrderBy(u => u.FirstName)
                .Skip(skip)
                .Take(pageSize)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    UserName = u.UserName,
                    DisplayName = u.DisplayName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = includeMetadata && u.Metadata != null
                        ? u.Metadata.Select(m => new UserMetadata
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Key = m.Key,
                            Value = m.Value
                        }).ToList()
                        : new List<UserMetadata>()
                })
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving all users.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserAsync(int userId, UserUpdateDto user)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            var existingUser = await context.Users
                .Include(u => u.Metadata)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null)
                throw new InvalidOperationException($"User with ID {userId} not found.");

            var originalFirstName = existingUser.FirstName;
            var originalLastName = existingUser.LastName;

            // Update user properties
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                InputValidator.ValidateEmail(user.Email);
                var normalizedEmail = user.Email.Trim().ToLowerInvariant();
                if (normalizedEmail != existingUser.Email)
                {
                    var emailExists = await context.Users
                        .CountAsync(u => u.Id != userId && u.Email == normalizedEmail) > 0;
                    if (emailExists)
                        throw new InvalidOperationException("Email is already in use.");

                    existingUser.Email = normalizedEmail;
                }
            }

            if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName != existingUser.UserName)
            {
                var usernameExists = await context.Users.CountAsync(u => u.Id != userId && u.UserName == user.UserName) > 0;
                if (usernameExists) throw new InvalidOperationException("Username already in use.");
                existingUser.UserName = user.UserName.Trim();
            }

            existingUser.Contact = user.Contact ?? existingUser.Contact;
            existingUser.FirstName = user.FirstName ?? existingUser.FirstName;
            existingUser.LastName = user.LastName ?? existingUser.LastName;
            existingUser.UserName = user.UserName ?? existingUser.UserName;
            existingUser.ProfilePicture = user.ProfilePicture ?? existingUser.ProfilePicture;

            if (!string.IsNullOrWhiteSpace(user.Role))
            {
                var parsedRole = InputParser.ParseEnum<TRole>(user.Role, true);
                existingUser.Role = parsedRole.ToString();
            }

            if (!string.IsNullOrWhiteSpace(user.Permission))
            {
                var parsedPermission = InputParser.ParseEnum<TPermission>(user.Permission, true);
                existingUser.Permission = parsedPermission.ToString();
            }

            if (!string.IsNullOrWhiteSpace(user.Status))
            {
                var parsedStatus = InputParser.ParseEnum<TStatus>(user.Status, true);
                existingUser.Status = parsedStatus.ToString();
            }

            if (!string.IsNullOrWhiteSpace(user.Theme))
            {
                var parsedTheme = InputParser.ParseEnum<TTheme>(user.Theme, true);
                existingUser.Theme = parsedTheme.ToString();
            }

            // Update metadata if provided
            if (user.Metadata != null)
            {
                // Remove existing metadata
                if (existingUser.Metadata?.Count > 0)
                {
                    context.UserMetadata.RemoveRange(existingUser.Metadata);
                }

                // Add new metadata
                var newMetadata = user.Metadata.Select(kvp => new UserMetadata
                {
                    UserId = userId,
                    Key = kvp.Key,
                    Value = kvp.Value
                }).ToList();

                context.UserMetadata.AddRange(newMetadata);
            }

            // Update DisplayName only if FirstName or LastName changed
            if (existingUser.FirstName != originalFirstName || existingUser.LastName != originalLastName)
            {
                existingUser.SetDisplayName();
            }
            existingUser.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return existingUser;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to update user due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while updating user.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<UserModel>> UpdateUserAsync(List<(int UserId, UserUpdateDto User)> updates)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            var userIds = updates.Select(u => u.UserId).ToList();
            InputValidator.Validate(userIds);

            // Fetch all users to be updated in a single query
            var existingUsers = await context.Users
                .Include(u => u.Metadata)
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            var updatedUsers = new List<UserModel>();

            foreach (var (userId, user) in updates)
            {
                if (user == null)
                    throw new ArgumentException($"User update data for user ID {userId} is null.", nameof(updates));

                if (!existingUsers.TryGetValue(userId, out var existingUser))
                    throw new InvalidOperationException($"User with ID {userId} not found.");

                var originalFirstName = existingUser.FirstName;
                var originalLastName = existingUser.LastName;

                // Update user properties
                if (!string.IsNullOrWhiteSpace(user.FirstName))
                    existingUser.FirstName = user.FirstName;

                if (!string.IsNullOrWhiteSpace(user.LastName))
                    existingUser.LastName = user.LastName;

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    InputValidator.ValidateEmail(user.Email);
                    existingUser.Email = user.Email.Trim().ToLowerInvariant();
                }

                if (!string.IsNullOrWhiteSpace(user.Role))
                {
                    var parsedRole = InputParser.ParseEnum<TRole>(user.Role, true);
                    existingUser.Role = parsedRole.ToString();
                }

                if (!string.IsNullOrWhiteSpace(user.Permission))
                {
                    var parsedPermission = InputParser.ParseEnum<TPermission>(user.Permission, true);
                    existingUser.Permission = parsedPermission.ToString();
                }

                if (!string.IsNullOrWhiteSpace(user.Status))
                {
                    var parsedStatus = InputParser.ParseEnum<TStatus>(user.Status, true);
                    existingUser.Status = parsedStatus.ToString();
                }

                if (!string.IsNullOrWhiteSpace(user.Theme))
                {
                    var parsedTheme = InputParser.ParseEnum<TTheme>(user.Theme, true);
                    existingUser.Theme = parsedTheme.ToString();
                }

                if (user.ProfilePicture != null)
                    existingUser.ProfilePicture = user.ProfilePicture;

                // Update metadata if provided
                if (user.Metadata != null)
                {
                    // Remove existing metadata
                    if (existingUser.Metadata?.Count > 0)
                    {
                        context.UserMetadata.RemoveRange(existingUser.Metadata);
                    }

                    // Add new metadata
                    var newMetadata = user.Metadata.Select(kvp => new UserMetadata
                    {
                        UserId = userId,
                        Key = kvp.Key,
                        Value = kvp.Value
                    }).ToList();

                    context.UserMetadata.AddRange(newMetadata);
                }

                // Update DisplayName only if FirstName or LastName changed
                if (existingUser.FirstName != originalFirstName || existingUser.LastName != originalLastName)
                {
                    existingUser.SetDisplayName();
                }

                existingUser.UpdatedAt = DateTimeOffset.UtcNow;

                updatedUsers.Add(existingUser);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return updatedUsers;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to bulk update users due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while bulk updating users.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserStatusAsync(int userId, TStatus status)
    {
        var user = await UpdateUserFieldAsync(userId, "Status", status.ToString());
        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserStatusAsync(int userId, string status)
    {
        var parsedStatus = InputParser.ParseEnum<TStatus>(status, true);
        var user = await UpdateUserStatusAsync(userId, parsedStatus);
        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserRoleAsync(int userId, TRole role)
    {
        var user = await UpdateUserFieldAsync(userId, "Role", role.ToString());
        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserRoleAsync(int userId, string role)
    {
        var parsedRole = InputParser.ParseEnum<TRole>(role, true);
        var user = await UpdateUserRoleAsync(userId, parsedRole);
        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserPermissionsAsync(int userId, TPermission permission)
    {
        var user = await UpdateUserFieldAsync(userId, "Permission", permission.ToString());
        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserPermissionsAsync(int userId, string permission)
    {
        var parsedPermision = InputParser.ParseEnum<TPermission>(permission, true);
        var user = await UpdateUserPermissionsAsync(userId, parsedPermision);
        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserThemeAsync(int userId, TTheme theme)
    {
        var user = await UpdateUserFieldAsync(userId, "Theme", theme.ToString());
        return user;
    }

    /// <inheritdoc/>
    public virtual async Task<UserModel> UpdateUserThemeAsync(int userId, string theme)
    {
        var parsedTheme = InputParser.ParseEnum<TTheme>(theme, true);
        var user = await UpdateUserThemeAsync(userId, parsedTheme);
        return user;
    }

    /// <summary>
    /// Helper method to update a specific field of a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="fieldName">The field name to update.</param>
    /// <param name="value">The new value.</param>
    protected virtual async Task<UserModel> UpdateUserFieldAsync(int userId, string fieldName, string value)
    {
        InputValidator.Validate(userId, IntegerValidationType.GreaterThanZero);

        try
        {
            using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException($"User with ID {userId} not found.");

            switch (fieldName)
            {
                case "Status":
                    if (!Enum.IsDefined(typeof(TStatus), value))
                        throw new ArgumentException($"Invalid Status value: {value}");
                    user.Status = value;
                    break;
                case "Role":
                    if (!Enum.IsDefined(typeof(TRole), value))
                        throw new ArgumentException($"Invalid Role value: {value}");
                    user.Role = value;
                    break;
                case "Permission":
                    if (!Enum.IsDefined(typeof(TPermission), value))
                        throw new ArgumentException($"Invalid Permission value: {value}");
                    user.Permission = value;
                    break;
                case "Theme":
                    if (!Enum.IsDefined(typeof(TTheme), value))
                        throw new ArgumentException($"Invalid Theme value: {value}");
                    user.Theme = value;
                    break;
                default:
                    throw new ArgumentException($"Invalid field name: {fieldName}", nameof(fieldName));
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Failed to update user {fieldName} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An unexpected error occurred while updating user {fieldName}.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<UserModel>> GetUsersByRoleAsync(TRole role, bool includeMetadata = false, int pageNumber = 1, int pageSize = 50)
    {
        return await GetUsersByRoleAsync(role.ToString(), includeMetadata, pageNumber, pageSize);
    }

    /// <inheritdoc/>
    public virtual async Task<List<UserModel>> GetUsersByRoleAsync(string role, bool includeMetadata = false, int pageNumber = 1, int pageSize = 50)
    {
        var parsedRole = InputParser.ParseEnum<TRole>(role, true);
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var query = context.Users.AsNoTracking().Where(u => u.Role == parsedRole.ToString());

            if (includeMetadata)
            {
                query = query.Include(u => u.Metadata);
            }

            var skip = (pageNumber - 1) * pageSize;
            var users = await query
                .OrderBy(u => u.FirstName)
                .Skip(skip)
                .Take(pageSize)
                .Select(u => new UserModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    UserName = u.UserName,
                    Contact = u.Contact,
                    Status = u.Status,
                    Role = u.Role,
                    Theme = u.Theme,
                    Permission = u.Permission,
                    ProfilePicture = u.ProfilePicture,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Metadata = includeMetadata && u.Metadata != null
                        ? u.Metadata.Select(m => new UserMetadata
                        {
                            Id = m.Id,
                            UserId = m.UserId,
                            Key = m.Key,
                            Value = m.Value
                        }).ToList()
                        : new List<UserMetadata>()
                })
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving users by role.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<int> GetNumberOfUsersAsync()
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            return await GetTotalUsersCountQuery(context);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while getting the number of users.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<int> GetNumberOfUsersByRoleAsync<T>(T role) where T : Enum
    {
        return await GetNumberOfUsersByRoleAsync(role.ToString());
    }

    /// <inheritdoc/>
    public virtual async Task<int> GetNumberOfUsersByRoleAsync(string role)
    {
        //if (string.IsNullOrWhiteSpace(role))
        //    throw new ArgumentException("Role cannot be null or empty.", nameof(role));
        var parsedRole = InputParser.ParseEnum<TRole>(role, true);

        try
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Users.AsNoTracking().CountAsync(u => u.Role == parsedRole.ToString());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while getting the number of users by role.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<T>> ExecuteRawQueryAsync<T>(string sql, params object[] parameters) where T : class
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));

        try
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<T>().FromSqlRaw(sql, parameters).ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing raw query.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<T>> ExecuteRawScalarQueryAsync<T>(string sql, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));

        try
        {
            using var context = _contextFactory.CreateDbContext();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            // Add parameters
            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.Value = param ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            await context.Database.OpenConnectionAsync();
            using var reader = await command.ExecuteReaderAsync();

            var results = new List<T>();
            while (await reader.ReadAsync())
            {
                var value = reader.GetValue(0);
                if (value != DBNull.Value)
                {
                    results.Add((T)Convert.ChangeType(value, typeof(T)));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing raw scalar query.", ex);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<int> ExecuteRawNonQueryAsync(string sql, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL command cannot be null or empty.", nameof(sql));

        try
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Database.ExecuteSqlRawAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while executing raw non-query command.", ex);
        }
    }

    #region Dispose Pattern
    /// <summary>
    /// Disposes the resources used by the BaseDbImplementation.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by the BaseDbImplementation.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Dispose managed resources if any
            _disposed = true;
        }
    }
    #endregion
}