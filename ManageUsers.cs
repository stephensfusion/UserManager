using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NSCache.Interfaces;
using NSCore.DatabaseContext;
using NSCore.DatabaseProviders;
using NSCore.Models.Enums;
using NSCore.Validation;
using System.Data;
using System.Security;
using UserManager.Data;
using UserManager.DbImplementation;
using UserManager.Dtos;
using UserManager.Enumerations;
using UserManager.Interfaces;
using UserManager.LazyLoading;
using UserManager.Models;
using UserManager.Services;

namespace UserManager;

/// <summary>
/// Manages user-related operations such as adding, deleting, and updating users.
/// </summary>
public class ManageUsers : ManageUsers<Role, Status, Permission, Theme>
{
    /// <summary>
    /// Initializes a new instance of the ManageUsers class.
    /// </summary>
    /// <param name="config">The database configuration.</param>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cacheManager">The cache manager instance.</param>
    /// <param name="keyBuilder">The cache key builder instance.</param>
    /// <param name="applyMigrationsAutomatically">Whether to apply migrations automatically.</param>
    public ManageUsers(IDatabaseConfig config, IDbContextFactory<AppDbContext> contextFactory, ICacheManager cacheManager, ICacheKeyBuilder keyBuilder, bool applyMigrationsAutomatically = true) : base(config, contextFactory, cacheManager, keyBuilder, applyMigrationsAutomatically) { }
}

/// <summary>
/// Manages user-related operations such as adding, deleting, and updating users.
/// </summary>
public class ManageUsers<TRole, TStatus, TPermission, TTheme> : BackgroundService, IManageUsers<TRole, TStatus, TPermission, TTheme> where TRole : struct, Enum where TStatus : struct, Enum where TPermission : struct, Enum where TTheme : struct, Enum
{
    private IManageUsers<TRole, TStatus, TPermission, TTheme> _connection;
    private bool _isContextCreated;
    private bool _applyMigrationsAutomatically;
    private LazyLoadManager _userLoadManager;
    private INsContextInit _initializer;

    private readonly ICacheManager _cacheManager;
    private readonly ICacheKeyBuilder _keyBuilder;

    // private readonly CacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the ManageUsers class with cache support.
    /// </summary>
    /// <param name="config">The database configuration.</param>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cacheManager">The cache manager instance.</param>
    /// <param name="keyBuilder">The cache key builder instance.</param>
    /// <param name="applyMigrationsAutomatically">Whether to apply migrations automatically.</param>
    public ManageUsers(IDatabaseConfig config, IDbContextFactory<AppDbContext> contextFactory,
        ICacheManager cacheManager, ICacheKeyBuilder keyBuilder, bool applyMigrationsAutomatically = true)
    {
        _applyMigrationsAutomatically = applyMigrationsAutomatically;
        this._cacheManager = cacheManager;
        this._keyBuilder = keyBuilder;

        _connection = config switch
        {
            SQLDb => new SQL<TRole, TStatus, TPermission, TTheme>(contextFactory),
            PSQLDb or SuperBaseDb => new PostgreSQL<TRole, TStatus, TPermission, TTheme>(contextFactory),
            MySQLDb => new MySQL<TRole, TStatus, TPermission, TTheme>(contextFactory),
            _ => throw new ArgumentException("Unsupported database type.")
        };

        if (_connection == null)
            throw new InvalidOperationException("Failed to initialize a valid INsContextInitializer.");
        _initializer = _connection as INsContextInit ?? throw new InvalidOperationException("Failed to initialize a valid INsContextInitializer.");
        InitializeLazyLoadingSystem();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Database connection is not initialized.");
        }

        if (_applyMigrationsAutomatically)
        {
            await _initializer.ApplyMigrationsAsync(stoppingToken);
        }

        if (_initializer.IsContextCreated())
        {
            _isContextCreated = true;
        }
    }

    private void InitializeLazyLoadingSystem()
    {
        var dataService = new DataService(pageSize: 10, fetchDataFunc: (from, pageSize) => FetchUsersAsync(from, pageSize, includeMetadata: false));
        _userLoadManager = new LazyLoadManager(dataService);
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> FetchUsersAsync(int from = 1, int pageSize = 10, bool includeMetadata = false)
    {
        EnsureContext();
        List<int> numbersToValidate = new List<int>() { from, pageSize };
        InputValidator.Validate(numbersToValidate, IntegerValidationType.GreaterThanZero);

        var parts = new List<string> { $"fr:{from}", $"ps:{pageSize}", $"im:{includeMetadata}" };
        var cacheKey = AdvancedCacheKey(parts);
        var exists = await _cacheManager.ExistsAsync(cacheKey);

        if (!exists)
        {
            var userData = await _connection.FetchUsersAsync(from, pageSize, includeMetadata);

            if (userData.Count == 0)
            {
                await _cacheManager.SetAsync(cacheKey, new List<int>());
            }
            await _cacheManager.SetManyAsListAsync<UserModel, int>(userData, cacheKey, u => u.Id);
            await _cacheManager.SetManyAsync<UserModel, UserModel>(userData, u => UserCacheKey(u.Id), u => u, TimeSpan.FromMinutes(5));
            return userData;
        }
        else
        {
            List<int> userIds = await _cacheManager.GetAsync<List<int>>(cacheKey);
            var result = await _cacheManager.GetManyAsync<UserModel, int>(userIds, u => UserCacheKey(u));
            return result.Values.Where(u => u != null).ToList()!;
        }
    }

    ///<inheritdoc/>
    public virtual async Task<bool> UserExistsAsync(string email, string name = "")
    {
        EnsureContext();
        InputValidator.ValidateEmail(email);
        return await _connection.UserExistsAsync(email, name);
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> AddUserAsync(List<CreateUserDto> users)
    {
        EnsureContext();

        if (users == null || users.Count == 0)
        {
            throw new ArgumentNullException(nameof(users), "User list cannot be null or empty.");
        }

        foreach (var userDto in users)
        {
            InputValidator.Validate([userDto.FirstName, userDto.LastName, userDto.UserName]);
            InputValidator.Validate(userDto.Email);
            if (userDto.Metadata?.Count(m => string.IsNullOrWhiteSpace(m.Key) || string.IsNullOrWhiteSpace(m.Value)) > 0)
            {
                throw new ArgumentException($"Metadata keys/values cannot be empty for user: {userDto.UserName}", nameof(users));
            }
        }

        List<UserModel> createdUsers;
        createdUsers = await _connection.AddUserAsync(users);

        await _cacheManager.SetManyAsync<UserModel, UserModel>(createdUsers, u => UserCacheKey(u.Id), u => u);
        return createdUsers;
    }

    /// <inheritdoc/>
    public async Task<UserModel> AddUserAsync(CreateUserDto user)
    {
        EnsureContext();

        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.Metadata != null && user.Metadata.Count(m => string.IsNullOrWhiteSpace(m.Key) || string.IsNullOrWhiteSpace(m.Value)) > 0)
        {
            throw new ArgumentException("Metadata entries must have non-empty keys and values.", nameof(user.Metadata));
        }

        UserModel createdUser;
        createdUser = await _connection.AddUserAsync(user);

        var cacheKey = UserCacheKey(createdUser.Id);
        await _cacheManager.SetAsync(cacheKey, createdUser);
        return createdUser;
    }

    /// <inheritdoc/>
    public async Task<(List<UserModel> Users, int TotalCount)> SearchUsersAsync(string? searchTerm, string? role, string? status, string sortBy, bool ascending, int page, int pageSize, SearchMode searchMode, bool includeMetadata = false)
    {
        EnsureContext();

        var numbersToValidate = new List<int>() { page, pageSize };
        InputValidator.Validate(numbersToValidate);

        List<string> parts = [$"st:{searchTerm}", $"rl:{role}", $"st:{status}", $"sb:{sortBy}", $"as:{ascending}", $"pg:{page}", $"ps:{pageSize}", $"sm:{searchMode}", $"im:{includeMetadata}"];
        var cacheKey = AdvancedCacheKey(parts);

        var cachedResult = await _cacheManager.GetAsync<SearchIdsAndCount>(cacheKey);

        if (cachedResult != null)
        {
            var users = await GetUsersByIdsAsync(cachedResult.Ids, includeMetadata);
            return (users, cachedResult.TotalCount);
        }

        var (dbUsers, totalCount) = await _connection.SearchUsersAsync(searchTerm, role, status, sortBy, ascending, page, pageSize, searchMode, includeMetadata);

        await _cacheManager.SetManyAsync<UserModel, UserModel>(dbUsers, u => UserCacheKey(u.Id), u => u, TimeSpan.FromMinutes(30));

        return (dbUsers, totalCount);
    }

    /// <inheritdoc/>
    public async Task DeleteUserAsync(int userId)
    {
        EnsureContext();

        InputValidator.Validate(userId, IntegerValidationType.GreaterThanZero);

        await _connection.DeleteUserAsync(userId);

        await _cacheManager.RemoveAsync(UserCacheKey(userId));
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> DeleteUserAsync(List<int> userIds)
    {
        EnsureContext();
        var usersToDelete = await _connection.DeleteUserAsync(userIds);

        await _cacheManager.RemoveManyAsync(usersToDelete, u => UserCacheKey(u.Id));
        return usersToDelete;
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, bool includeMetadata = false)
    {
        EnsureContext();

        List<int> numbersToValidate = new List<int>() { pageNumber, pageSize };
        InputValidator.Validate(numbersToValidate, IntegerValidationType.GreaterThanZero);

        List<string> parts = [$"pn:{pageNumber}", $"ps:{pageSize}", $"im:{includeMetadata}"];
        var cacheKey = AdvancedCacheKey(parts);

        var exists = await _cacheManager.ExistsAsync(cacheKey);

        if (!exists)
        {
            var userData = await _connection.GetAllUsersAsync(pageNumber, pageSize, includeMetadata);

            await _cacheManager.SetManyAsync<UserModel, UserModel>(userData, u => $"user:{u.Id}", u => u, TimeSpan.FromMinutes(5));
            return userData;
        }
        else
        {
            List<int> userIds = await _cacheManager.GetAsync<List<int>>(cacheKey);
            var result = await _cacheManager.GetManyAsync<UserModel, int>(userIds, u => UserCacheKey(u));
            return result.Values.Where(u => u != null).ToList()!;
        }
    }

    /// <inheritdoc/>
    public async Task<UserModel?> GetUserAsync(int userId, bool includeMetadata = false)
    {
        EnsureContext();
        InputValidator.Validate(userId, IntegerValidationType.GreaterThanZero);
        var userCacheKey = UserCacheKey(userId);
        var exists = await _cacheManager.ExistsAsync(userCacheKey);
        if (!exists)
        {
            var userData = await _connection.GetUserAsync(userId, includeMetadata);
            if (userData != null)
            {
                await _cacheManager.SetAsync(userCacheKey, userData);
            }
            return userData;
        }
        else
        {
            return await _cacheManager.GetAsync<UserModel>(userCacheKey);
        }
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> GetUsersByIdsAsync(List<int> userIds, bool includeMetadata = false)
    {
        EnsureContext();
        InputValidator.Validate(userIds, IntegerValidationType.GreaterThanZero);
        return await _connection.GetUsersByIdsAsync(userIds, includeMetadata);
    }


    /// <inheritdoc/>
    public async Task<UserModel?> GetUserByEmailAsync(string email, bool includeMetadata = false)
    {
        EnsureContext();
        InputValidator.ValidateEmail(email);
        List<string> parts = [$"em:{email}", $"md:{includeMetadata}"];
        var emailCacheKey = AdvancedCacheKey(parts);

        var userId = await _cacheManager.GetAsync<int?>(emailCacheKey);

        if (userId.HasValue)
        {
            var userCacheKey = UserCacheKey(userId.Value);
            var cachedUser = await _cacheManager.GetAsync<UserModel>(userCacheKey);
            if (cachedUser != null) return cachedUser;

            // Clear the bad ID and force a database hit
            await _cacheManager.RemoveAsync(emailCacheKey);
        }

        var userData = await _connection.GetUserByEmailAsync(email, includeMetadata);

        if (userData != null)
        {
            await _cacheManager.SetAsync<int>(emailCacheKey, userData.Id);
            await _cacheManager.SetAsync(UserCacheKey(userData.Id), userData);
        }

        return userData;
    }

    /// <inheritdoc/>
    public async Task<UserModel?> GetUserByUsernameAsync(string username, bool includeMetadata = false)
    {
        EnsureContext();
        InputValidator.Validate(username);
        List<string> parts = [$"un:{username}", $"md:{includeMetadata}"];
        var usernameCacheKey = AdvancedCacheKey(parts);

        var userId = await _cacheManager.GetAsync<int?>(usernameCacheKey);

        if (userId.HasValue)
        {
            var userCacheKey = UserCacheKey(userId.Value);
            var cachedUser = await _cacheManager.GetAsync<UserModel>(userCacheKey);
            if (cachedUser != null) return cachedUser;
        }

        var userData = await _connection.GetUserByUsernameAsync(username, includeMetadata);

        if (userData != null)
        {
            await _cacheManager.SetAsync<int>(usernameCacheKey, userData.Id);
            await _cacheManager.SetAsync(UserCacheKey(userData.Id), userData);
        }

        return userData;
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> GetUsersByRoleAsync(TRole role, bool includeMetadata = false, int pageNumber = 1, int pageSize = 10)
    {
        EnsureContext();
        List<string> parts = [$"role:{role}", $"im:{includeMetadata}", $"pn:{pageNumber}", $"ps:{pageSize}"];
        var cacheKey = AdvancedCacheKey(parts);

        var exists = await _cacheManager.ExistsAsync(cacheKey);
        if (!exists)
        {
            var userData = await _connection.GetUsersByRoleAsync(role, includeMetadata, pageNumber, pageSize);
            if (userData.Count == 0)
            {
                await _cacheManager.SetAsync(cacheKey, new List<int>());
                throw new Exception("No user available for this role");
            }
            await _cacheManager.SetManyAsListAsync<UserModel, int>(userData, cacheKey, userData => userData.Id);
            await _cacheManager.SetManyAsync<UserModel, UserModel>(userData, u => UserCacheKey(u.Id), u => u);
            return userData;
        }
        else
        {
            List<int> userIds = await _cacheManager.GetAsync<List<int>>(cacheKey);
            var result = await _cacheManager.GetManyAsync<UserModel, int>(userIds, u => UserCacheKey(u));
            return result.Values.Where(u => u != null).ToList()!;
        }
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> GetUsersByRoleAsync(string role, bool includeMetadata = false, int pageNumber = 1, int pageSize = 10)
    {
        EnsureContext();

        var numbersToValidate = new List<int>() { pageNumber, pageSize };
        InputValidator.Validate(numbersToValidate, IntegerValidationType.GreaterThanZero);

        List<string> parts = [$"role:{role}", $"im:{includeMetadata}", $"pn:{pageNumber}", $"ps:{pageSize}"];
        var cacheKey = AdvancedCacheKey(parts);

        var exists = await _cacheManager.ExistsAsync(cacheKey);
        if (!exists)
        {
            var userData = await _connection.GetUsersByRoleAsync(role, includeMetadata, pageNumber, pageSize);
            if (userData.Count == 0)
            {
                await _cacheManager.SetAsync(cacheKey, new List<int>());
            }
            await _cacheManager.SetManyAsListAsync<UserModel, int>(userData, cacheKey, userData => userData.Id);
            await _cacheManager.SetManyAsync<UserModel, UserModel>(userData, u => UserCacheKey(u.Id), u => u);
            return userData;
        }
        else
        {
            List<int> userIds = await _cacheManager.GetAsync<List<int>>(cacheKey);
            var result = await _cacheManager.GetManyAsync<UserModel, int>(userIds, u => UserCacheKey(u));
            return result.Values.Where(u => u != null).ToList()!;
        }
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserAsync(int userId, UserUpdateDto user)
    {
        EnsureContext();

        if (user == null)
            throw new ArgumentNullException(nameof(user));
        InputValidator.Validate(userId, IntegerValidationType.GreaterThanZero);

        UserModel updatedUser;
        updatedUser = await _connection.UpdateUserAsync(userId, user);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<List<UserModel>> UpdateUserAsync(List<(int UserId, UserUpdateDto User)> updates)
    {
        EnsureContext();

        if (updates == null || updates.Count() == 0)
            throw new ArgumentException("At least one user update must be provided.", nameof(updates));

        List<UserModel> updatedUsers;
        updatedUsers = await _connection.UpdateUserAsync(updates);
        await _cacheManager.SetManyAsync<UserModel, UserModel>(updatedUsers, u => UserCacheKey(u.Id), u => u);
        return updatedUsers;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserPermissionsAsync(int userId, TPermission permission)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserPermissionsAsync(userId, permission);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserPermissionsAsync(int userId, string permission)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserPermissionsAsync(userId, permission);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserRoleAsync(int userId, TRole role)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserRoleAsync(userId, role);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserRoleAsync(int userId, string role)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserRoleAsync(userId, role);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserStatusAsync(int userId, TStatus status)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserStatusAsync(userId, status);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserStatusAsync(int userId, string status)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserStatusAsync(userId, status);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserThemeAsync(int userId, TTheme theme)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserThemeAsync(userId, theme);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<UserModel> UpdateUserThemeAsync(int userId, string theme)
    {
        EnsureContext();

        var updatedUser = await _connection.UpdateUserThemeAsync(userId, theme);

        await _cacheManager.SetAsync<UserModel>(UserCacheKey(updatedUser.Id), updatedUser);

        return updatedUser;
    }

    /// <inheritdoc/>
    public async Task<int> GetNumberOfUsersAsync()
    {
        EnsureContext();
        return await _connection.GetNumberOfUsersAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetNumberOfUsersByRoleAsync<TRole>(TRole role) where TRole : Enum
    {
        EnsureContext();
        return await _connection.GetNumberOfUsersByRoleAsync(role);
    }

    /// <inheritdoc/>
    public async Task<int> GetNumberOfUsersByRoleAsync(string role)
    {
        EnsureContext();
        return await _connection.GetNumberOfUsersByRoleAsync(role);
    }

    /// <inheritdoc/>
    public async Task<List<T>> ExecuteRawQueryAsync<T>(string sql, params object[] parameters) where T : class
    {
        EnsureContext();

        // For queries that return entity objects
        return await _connection.ExecuteRawQueryAsync<T>(sql, parameters);
    }

    /// <inheritdoc/>
    public Task<List<T>> ExecuteRawScalarQueryAsync<T>(string sql, params object[] parameters)
    {
        EnsureContext();

        // For queries that return simple scalar values
        return _connection.ExecuteRawScalarQueryAsync<T>(sql, parameters);
    }

    /// <inheritdoc/>
    public Task<int> ExecuteRawNonQueryAsync(string sql, params object[] parameters)
    {
        EnsureContext();

        // For commands that modify data (INSERT/UPDATE/DELETE)
        return _connection.ExecuteRawNonQueryAsync(sql, parameters);
    }

    #region Cache key generators
    private string UserCacheKey(int id)
    {
        return _keyBuilder.BuildKey($"user:{id}");
    }

    private string AdvancedCacheKey(List<string> parts)
    {
        return _keyBuilder.BuildKey(string.Join(";", parts));
    }
    #endregion


    /// <summary>
    /// Ensures that the context has been properly initialized.
    /// Throws a NotInitializedException if the context is not created.
    /// </summary>
    private void EnsureContext()
    {
        if (!_isContextCreated)
        {
            throw new InvalidOperationException("Failed to initialize the database context.");
        }
    }
}
