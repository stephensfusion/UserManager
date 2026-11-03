using UserManager.Dtos;
using UserManager.Enumerations;
using UserManager.Models;

namespace UserManager.Interfaces;

/// <summary>
/// Provides methods for managing users in the system.
/// </summary>
public interface IManageUsers<TRole, TStatus, TPermission, TTheme>
{
    /// <summary>
    /// Check the name or email already exists in the Database
    /// </summary>
    /// <param name="email">Email of the user </param>
    /// <param name="name">Name of the user can be empty </param>
    /// <returns> True if the user already exists (with same email or name) or else false</returns>
    Task<bool> UserExistsAsync(string email, string name ="");
    /// <summary>
    /// Adds a new user to the system using a CreateUserDto object.
    /// </summary>
    /// <param name="user">The CreateUserDto object containing user details.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<UserModel> AddUserAsync(CreateUserDto user);

    /// <summary>
    /// Adds multiple users to the system using a collection of CreateUserDto objects.
    /// </summary>
    /// <param name="users">The collection of users to add.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<List<UserModel>> AddUserAsync(List<CreateUserDto> users);


    /// <summary>
    /// Searches for users in the system based on various criteria.
    /// </summary>
    /// <param name="searchTerm">The term to search for in user fields (e.g., name, email). Optional.</param>
    /// <param name="role">The role to filter users by. Optional.</param>
    /// <param name="status">The status to filter users by. Optional.</param>
    /// <param name="sortBy">The field to sort the results by. Defaults to "FirstName".</param>
    /// <param name="ascending">Whether to sort the results in ascending order. Defaults to true.</param>
    /// <param name="pageNumber">The page number for pagination. Defaults to 1.</param>
    /// <param name="pageSize">The number of results per page. Defaults to 10.</param>
    /// <param name="searchMode">The search mode to use (e.g., Contains, StartsWith, etc.). Defaults to Contains.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a tuple with the collection of users and the total count.
    /// </returns>
    Task<(List<UserModel> Users, int TotalCount)> SearchUsersAsync(
        string? searchTerm = null,
        string? role = null,
        string? status = null,
        string sortBy = "FirstName",
        bool ascending = true,
        int pageNumber = 1,
        int pageSize = 10,
        SearchMode searchMode = SearchMode.Contains, bool includeMetadata = false
        );


    /// <summary>  
    /// Deletes a user by their ID.  
    /// </summary>  
    /// <param name="userId">The ID of the user to delete.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task DeleteUserAsync(int userId);

    /// <summary>
    /// Deletes multiple users by their IDs.
    /// </summary>
    /// <param name="userIds">The list of user IDs to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning the deleted user models.</returns>
    Task<List<UserModel>> DeleteUserAsync(List<int> userIds);

    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the user, or null if not found.
    /// </returns>
    Task<UserModel?> GetUserAsync(int userId, bool includeMetadata = false);

    /// <summary>
    /// Retrieves a collection of users by their IDs.
    /// </summary>
    /// <param name="userIds">The list of user IDs to retrieve.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable of <see cref="UserModel"/> objects corresponding to the specified user IDs.
    /// </returns>
    Task<List<UserModel>> GetUsersByIdsAsync(List<int> userIds, bool includeMetadata = false);

    /// <summary>
    /// Retrieves a user by their email.
    /// </summary>
    /// <param name="email">The email of the user to retrieve.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the user, or null if not found.
    /// </returns>
    Task<UserModel?> GetUserByEmailAsync(string email, bool includeMetadata = false);

    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="username">The username of the user to retrieve.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the user, or null if not found.
    /// </returns>
    Task<UserModel?> GetUserByUsernameAsync(string username, bool includeMetadata = false);

    /// <summary>
    /// Retrieves a paged list of users in the system.
    /// </summary>
    /// <param name="pageNumber">The page number for pagination. Defaults to 1.</param>
    /// <param name="pageSize">The number of results per page. Defaults to 10.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a paged result of users.
    /// </returns>
    Task<List<UserModel>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10, bool includeMetadata = false);

    /// <summary>  
    /// Updates the details of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="user">The updated user details.</param>  
    /// <returns>A task that represents the asynchronous operation, returning a <see cref="UserModel"/></returns>
    Task<UserModel> UpdateUserAsync(int userId, UserUpdateDto user);

    /// <summary>
    /// Updates multiple users in bulk based on the provided user ID and update data.
    /// </summary>
    /// <param name="updates">A collection of tuples containing the user ID and the corresponding <see cref="UserUpdateDto"/> 
    /// object with the updates to apply. Must contain at least one update and all user IDs must be greater than zero.</param>
    /// <returns>A task that represents the asynchronous operation, returning an <see cref="IEnumerable{UserModel}"/> 
    /// containing the updated user models.</returns>
    Task<List<UserModel>> UpdateUserAsync(List<(int UserId, UserUpdateDto User)> updates);

    /// <summary>  
    /// Updates the status of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="status">The new status of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserStatusAsync(int userId, TStatus status);

    /// <summary>  
    /// Updates the status of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="status">The new status of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserStatusAsync(int userId, string status);

    /// <summary>  
    /// Updates the role of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="role">The new role of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserRoleAsync(int userId, TRole role);

    /// <summary>  
    /// Updates the role of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="role">The new role of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserRoleAsync(int userId, string role);

    /// <summary>  
    /// Updates the permissions of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="permission">The new permissions of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserPermissionsAsync(int userId, TPermission permission);

    /// <summary>  
    /// Updates the permissions of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="permission">The new permissions of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserPermissionsAsync(int userId, string permission);

    /// <summary>  
    /// Updates the theme preference of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="theme">The new theme preference of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserThemeAsync(int userId, TTheme theme);

    /// <summary>  
    /// Updates the theme preference of a user.  
    /// </summary>  
    /// <param name="userId">The ID of the user to update.</param>  
    /// <param name="theme">The new theme preference of the user.</param>  
    /// <returns>A task that represents the asynchronous operation.</returns>  
    Task<UserModel> UpdateUserThemeAsync(int userId, string theme);

    ///<summary>
    /// Retrieves a paged list of users with the specified role.
    /// </summary>
    /// <param name="role">The role to filter users by.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <param name="pageNumber">The page number for pagination. Defaults to 1.</param>
    /// <param name="pageSize">The number of results per page. Defaults to 50.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of users with the specified role.
    /// </returns>
    Task<List<UserModel>> GetUsersByRoleAsync(TRole role, bool includeMetadata = false, int pageNumber = 1, int pageSize = 50);

    ///<summary>
    /// Retrieves a paged list of users with the specified role.
    /// </summary>
    /// <param name="role">The role to filter users by.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <param name="pageNumber">The page number for pagination. Defaults to 1.</param>
    /// <param name="pageSize">The number of results per page. Defaults to 50.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of users with the specified role.
    /// </returns>
    Task<List<UserModel>> GetUsersByRoleAsync(string role, bool includeMetadata = false, int pageNumber = 1, int pageSize = 50);


    /// <summary>
    /// Retrieves a list of users starting from a specific index with optional metadata.
    /// </summary>
    /// <param name="from">The zero-based index to start retrieving users from. Defaults to 0.</param>
    /// <param name="pageSize">The number of users to retrieve. Defaults to 10.</param>
    /// <param name="includeMetadata">Whether to include user metadata in the result. Defaults to false.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of users.
    /// </returns>
    Task<List<UserModel>> FetchUsersAsync(int from = 1, int pageSize = 10, bool includeMetadata = false);

    /// <summary>
    /// Retrieves the total number of users in the system.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total number of users.</returns>
    Task<int> GetNumberOfUsersAsync();


    ///<summary>
    /// Retrieves the total number of users in the system with the specified role.
    /// </summary>
    /// <param name="role">The role to filter users by.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total number of users with the specified role.</returns>
    Task<int> GetNumberOfUsersByRoleAsync<TRole>(TRole role) where TRole : Enum;

    ///<summary>
    /// Retrieves the total number of users in the system with the specified role.
    /// </summary>
    /// <param name="role">The role to filter users by.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total number of users with the specified role.</returns>
    Task<int> GetNumberOfUsersByRoleAsync(string role);

    /// <summary>
    /// Executes a raw SQL query with parameters to prevent SQL injection
    /// </summary>
    /// <typeparam name="T">The entity type to map results to</typeparam>
    /// <param name="sql">SQL query with parameter placeholders</param>
    /// <param name="parameters">SQL parameters to safely inject into query</param>
    /// <returns>List of entities matching the query</returns>
    Task<List<T>> ExecuteRawQueryAsync<T>(string sql, params object[] parameters) where T : class;

    /// <summary>
    /// Executes a raw SQL query that returns scalar values
    /// </summary>
    /// <typeparam name="T">The type of scalar result</typeparam>
    /// <param name="sql">SQL query with parameter placeholders</param>
    /// <param name="parameters">SQL parameters to safely inject into query</param>
    /// <returns>List of scalar values</returns>
    Task<List<T>> ExecuteRawScalarQueryAsync<T>(string sql, params object[] parameters);

    /// <summary>
    /// Executes a non-query SQL command (INSERT, UPDATE, DELETE)
    /// </summary>
    /// <param name="sql">SQL command with parameter placeholders</param>
    /// <param name="parameters">SQL parameters to safely inject into command</param>
    /// <returns>Number of rows affected</returns>
    Task<int> ExecuteRawNonQueryAsync(string sql, params object[] parameters);
}