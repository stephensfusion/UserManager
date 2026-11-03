using Azure;
using Microsoft.AspNetCore.Mvc;
using NSCore.Models.Enums;
using System.Threading;
using UserManager.Dtos;
using UserManager.Interfaces;
using UserManager.Models;

namespace UserManager.Controllers
{
    /// <summary>
    /// Base controller for user management operations following REST API conventions.
    /// Provides CRUD operations and user-specific functionality with proper HTTP verbs and resource naming.
    /// </summary>
    /// <typeparam name="TRole">Enumeration type for user roles</typeparam>
    /// <typeparam name="TStatus">Enumeration type for user status</typeparam>
    /// <typeparam name="TPermission">Enumeration type for user permissions</typeparam>
    /// <typeparam name="TTheme">Enumeration type for user themes</typeparam>
    [ApiController]
    [Route("api/v1/users")]
    public class UserManagerControllerBase<TRole, TStatus, TPermission, TTheme> : ControllerBase
        where TRole : struct, Enum
        where TStatus : struct, Enum
        where TPermission : struct, Enum
        where TTheme : struct, Enum
    {
        private readonly IManageUsers<TRole, TStatus, TPermission, TTheme> _userManagerHandler;

        /// <summary>
        /// Initializes a new instance of the UserManagerControllerBase.
        /// </summary>
        /// <param name="userManagerHandler">The user management service handler</param>
        /// <exception cref="ArgumentNullException">Thrown when userManagerHandler is null</exception>
        public UserManagerControllerBase(IManageUsers<TRole, TStatus, TPermission, TTheme> userManagerHandler)
        {
            _userManagerHandler = userManagerHandler ?? throw new ArgumentNullException(nameof(userManagerHandler));
        }

        /// <summary>
        /// Retrieves all users with optional filtering and pagination.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 50)</param>
        /// <param name="includeMetadata">Whether to include user metadata (default: false)</param>
        /// <param name="role">Optional role filter</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="search">Optional search term for name/email</param>
        /// <param name="sortBy">Optional sort term for name/email</param>
        /// <returns>Paginated list of users</returns>
        /// <response code="200">Returns the list of users</response>
        /// <response code="400">Invalid pagination parameters</response>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool includeMetadata = false,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
              [FromQuery] string? sortBy = "FirstName")
        {
            // Use SearchUsersAsync for unified filtering/pagination logic
            var (users, totalCount) = await _userManagerHandler.SearchUsersAsync(
                search, role, status, sortBy, true, pageNumber, pageSize, SearchMode.Contains, includeMetadata);

            var response = new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Users = users
            };

            return Ok(response);
        }

        /// <summary>
        /// Retrieves a paginated list of users starting from a specified index.
        /// </summary>
        /// <param name="from">The starting index for fetching users (default: 0)</param>
        /// <param name="pageSize">Number of users to fetch (default: 10)</param>
        /// <param name="includeMetadata">Whether to include user metadata (default: false)</param>
        /// <returns>List of users starting from the specified index</returns>
        /// <response code="200">Returns the list of users</response>
        /// <response code="400">Invalid pagination parameters</response>
        [HttpGet("paginated")]
        public async Task<ActionResult<List<UserModel>>> GetUsersPaginated([FromQuery] int from = 1, [FromQuery] int pageSize = 10, [FromQuery] bool includeMetadata = false)
        {
            var users = await _userManagerHandler.GetAllUsersAsync(from, pageSize, includeMetadata);

            return Ok(users);
        }

        /// <summary>
        /// Retrieves a specific user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user</param>
        /// <param name="includeMetadata">Whether to include user metadata (default: false)</param>
        /// <returns>User details</returns>
        /// <response code="200">Returns the user</response>
        /// <response code="404">User not found</response>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id, [FromQuery] bool includeMetadata = false)
        {
            var user = await _userManagerHandler.GetUserAsync(id, includeMetadata);
            if (user == null)
                return NotFound($"User with ID {id} not found.");

            return Ok(user);
        }

        /// <summary>
        /// Retrieves users by their role.
        /// </summary>
        /// <param name="role">The role to filter by</param>
        /// <param name="includeMetadata">Whether to include user metadata (default: false)</param>
        /// <returns>List of users with the specified role</returns>
        /// <response code="200">Returns users with the specified role</response>
        /// <response code="400">Invalid role specified</response>
        [HttpGet("by-role/{role}")]
        public async Task<IActionResult> GetUsersByRole(TRole role, [FromQuery] bool includeMetadata = false)
        {
            var users = await _userManagerHandler.GetUsersByRoleAsync(role, includeMetadata);
            return Ok(users);
        }

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user</param>
        /// <param name="includeMetadata">Whether to include user metadata (default: false)</param>
        /// <returns>User details</returns>
        /// <response code="200">Returns the user</response>
        /// <response code="404">User not found</response>
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email, [FromQuery] bool includeMetadata = false)
        {
            var user = await _userManagerHandler.GetUserByEmailAsync(email, includeMetadata);
            if (user == null)
                return NotFound($"User with email {email} not found.");

            return Ok(user);
        }

        /// <summary>
        /// Retrieves a user by their username.
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <param name="includeMetadata">Whether to include user metadata (default: false)</param>
        /// <returns>User details</returns>
        /// <response code="200">Returns the user</response>
        /// <response code="404">User not found</response>
        [HttpGet("by-username/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username, [FromQuery] bool includeMetadata = false)
        {
            var user = await _userManagerHandler.GetUserByUsernameAsync(username, includeMetadata);
            if (user == null)
                return NotFound($"User with username {username} not found.");

            return Ok(user);
        }

        /// <summary>
        /// Creates a new user in the system.
        /// </summary>
        /// <param name="userDto">User creation data transfer object</param>
        /// <returns>Created user details</returns>
        /// <response code="201">User created successfully</response>
        /// <response code="400">Invalid user data or validation errors</response>
        /// <response code="409">User with email/username already exists</response>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Check authorization using virtual hook
            if (!await CanPerformActionAsync(Actions.Create, additionalData: userDto))
                return Forbid("You don't have permission to create users.");

            // 2. Business rule validation using virtual hook
            var (isValid, errorMessage) = await ValidateBusinessRulesAsync(Actions.Create, userDto);
            if (!isValid)
                return BadRequest(errorMessage);

            try
            {
                await _userManagerHandler.AddUserAsync(userDto);
                // 3. Log the action using virtual hook
                await RecordActionAsync(Actions.Create, userDto, userDto);

                return Created("", new { Message = "User created successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        /// <summary>
        /// Creates multiple users in a single batch operation.
        /// </summary>
        /// <param name="users">List of user creation data transfer objects</param>
        /// <returns>Batch operation result</returns>
        /// <response code="201">Users created successfully</response>
        /// <response code="400">Invalid user data or validation errors</response>
        /// <response code="207">Partial success with some failures</response>
        [HttpPost("bulk")]
        public async Task<IActionResult> CreateUsers([FromBody] List<CreateUserDto> users)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _userManagerHandler.AddUserAsync(users);
                return Created("", new { Message = "Users created successfully.", Count = users.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates an existing user's information.
        /// </summary>
        /// <param name="id">The unique identifier of the user to update</param>
        /// <param name="userDto">User update data transfer object</param>
        /// <returns>No content on success</returns>
        /// <response code="204">User updated successfully</response>
        /// <response code="400">Invalid user data</response>
        /// <response code="404">User not found</response>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var oldData = await _userManagerHandler.GetUserAsync(id);

                var oldUser = new UserUpdateDto
                {
                    Id = oldData.Id,
                    Role = oldData.Role,
                    Theme = oldData.Theme,
                    Permission = oldData.Permission,
                    Password = oldData.Password,
                    FirstName = oldData.FirstName,
                    LastName = oldData.LastName,
                    Contact = oldData.Contact,
                    Status = oldData.Status,
                    ProfilePicture = oldData.ProfilePicture,
                };

                var updatedUser = await _userManagerHandler.UpdateUserAsync(id, userDto);
                userDto.Id = id;
                // Log the action using virtual hook
                await RecordActionAsync(Actions.Update, oldUser, userDto);
                return Ok(updatedUser);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }


        /// <summary>
        /// Updates a user's role using either a role name (string) or a role enum value.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="role">The new role as a string (e.g., "Admin").</param>
        /// <param name="roleEnum">The new role as an enum value (e.g., ?roleEnum=1 or ?roleEnum=Admin).</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Role was updated successfully.</response>
        /// <response code="400">No role provided or invalid role value.</response>
        /// <response code="404">User with the specified ID was not found.</response>
        [HttpPatch("{id:int}/role")]
        public async Task<IActionResult> UpdateUserRole(
        int id,
        [FromQuery] string? role = null,
        [FromQuery] TRole? roleEnum = null)
        {
            if (string.IsNullOrWhiteSpace(role) && !roleEnum.HasValue)
                return BadRequest("Role must be provided as 'role' (string) or 'roleEnum' (enum).");

            var roleName = roleEnum?.ToString() ?? role!.Trim();

            try
            {
                var oldData = await _userManagerHandler.GetUserAsync(id);

                var oldUser = new UserUpdateDto
                {
                    Id = oldData.Id,
                    Role = oldData.Role
                };

                var updatedUser = await _userManagerHandler.UpdateUserRoleAsync(id, roleName);

                var newUser = new UserUpdateDto
                {
                    Id = id,
                    Role = roleName
                };
                // Log the action using virtual hook
                await RecordActionAsync(Actions.Update, oldUser, newUser);

                return Ok(updatedUser);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Updates a user's permission using either a permission name (string) or a permission enum value.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="permission">The new role as a string (e.g., "Admin").</param>
        /// <param name="permissionEnum">The new permission as an enum value (e.g., ?permissionEnum=1 or ?permissionEnum=Read).</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Permission was updated successfully.</response>
        /// <response code="400">No permission provided or invalid permission value.</response>
        /// <response code="404">User with the specified ID was not found.</response>
        [HttpPatch("{id:int}/permission")]
        public async Task<IActionResult> UpdateUserPermission(
      int id,
      [FromQuery] string? permission = null,
      [FromQuery] TPermission? permissionEnum = null)
        {
            if (string.IsNullOrWhiteSpace(permission) && !permissionEnum.HasValue)
                return BadRequest("Permission must be provided as 'permission' (string) or 'permissionEnum' (enum).");

            var permissionName = permissionEnum?.ToString() ?? permission!.Trim();

            try
            {
                var oldData = await _userManagerHandler.GetUserAsync(id);

                var oldUser = new UserUpdateDto
                {
                    Id = oldData.Id,
                    Permission = oldData.Permission
                };

                var updatedUser = await _userManagerHandler.UpdateUserPermissionsAsync(id, permissionName);

                var newUser = new UserUpdateDto
                {
                    Id = id,
                    Permission = permissionName
                };
                // Log the action using virtual hook
                await RecordActionAsync(Actions.Update, oldUser, newUser);

                return Ok(updatedUser);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Updates a user's status using either a status name (string) or a status enum value.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="status">The new role as a string (e.g., "Admin").</param>
        /// <param name="statusEnum">The new status as an enum value (e.g., ?statusEnum=1 or ?statusEnum=Read).</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Status was updated successfully.</response>
        /// <response code="400">No status provided or invalid status value.</response>
        /// <response code="404">User with the specified ID was not found.</response>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateUserStatus(
      int id,
      [FromQuery] string? status = null,
      [FromQuery] TStatus? statusEnum = null)
        {
            if (string.IsNullOrWhiteSpace(status) && !statusEnum.HasValue)
                return BadRequest("Status must be provided as 'status' (string) or 'statusEnum' (enum).");

            var statusName = statusEnum?.ToString() ?? status!.Trim();

            try
            {
                var oldData = await _userManagerHandler.GetUserAsync(id);

                var oldUser = new UserUpdateDto
                {
                    Id = oldData.Id,
                    Status = oldData.Status
                };

                var updatedUser =await _userManagerHandler.UpdateUserStatusAsync(id, statusName);

                var newUser = new UserUpdateDto
                {
                    Id = id,
                    Status = statusName
                };
                // Log the action using virtual hook
                await RecordActionAsync(Actions.Update, oldUser, newUser);

                return Ok(updatedUser);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Updates a user's theme using either a theme name (string) or a theme enum value.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="theme">The new theme as a string (e.g., "Dark").</param>
        /// <param name="themeEnum">The new theme as an enum value (e.g., ?themeEnum=1 or ?themeEnum=Light).</param>
        /// <returns>No content on success.</returns>
        /// <response code="204">Theme was updated successfully.</response>
        /// <response code="400">No theme provided or invalid theme value.</response>
        /// <response code="404">User with the specified ID was not found.</response>
        [HttpPatch("{id:int}/theme")]
        public async Task<IActionResult> UpdateUserTheme(
        int id,
        [FromQuery] string? theme = null,
        [FromQuery] TTheme? themeEnum = null)
        {
            if (string.IsNullOrWhiteSpace(theme) && !themeEnum.HasValue)
                return BadRequest("Theme must be provided as 'theme' (string) or 'themeEnum' (enum).");

            var themeName = themeEnum?.ToString() ?? theme!.Trim();

            try
            {
                var oldData = await _userManagerHandler.GetUserAsync(id);

                var oldUser = new UserUpdateDto
                {
                    Id = oldData.Id,
                    Theme = oldData.Theme
                };

               var updatedUser = await _userManagerHandler.UpdateUserThemeAsync(id, themeName);

                var newUser = new UserUpdateDto
                {
                    Id = id,
                    Theme = theme
                };
                // Log the action using virtual hook
                await RecordActionAsync(Actions.Update, oldUser, newUser);

                return Ok(updatedUser);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }


        /// <summary>
        /// Deletes a user from the system.
        /// </summary>
        /// <param name="id">The unique identifier of the user to delete</param>
        /// <returns>No content on success</returns>
        /// <response code="204">User deleted successfully</response>
        /// <response code="404">User not found</response>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _userManagerHandler.DeleteUserAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Deletes multiple users in a single batch operation.
        /// </summary>
        /// <param name="userIds">Collection of user IDs to delete</param>
        /// <returns>No content on success</returns>
        /// <response code="204">Users deleted successfully</response>
        /// <response code="400">Invalid user IDs</response>
        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteUsers([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
                return BadRequest("User IDs are required.");

            try
            {
                await _userManagerHandler.DeleteUserAsync(userIds);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Performs advanced search on users with parameterized SQL query.
        /// </summary>
        /// <param name="firstName">First name search pattern</param>
        /// <param name="lastName">Last name search pattern</param>
        /// <returns>List of matching users</returns>
        /// <response code="200">Returns matching users</response>
        /// <response code="400">Invalid search parameters</response>
        [HttpGet("advanced-search")]
        public async Task<ActionResult> AdvancedSearchUsers(
            [FromQuery] string firstName,
            [FromQuery] string lastName)
        {
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                return BadRequest("At least one search parameter is required.");

            try
            {
                // Using parameterized queries to prevent SQL injection
                var sql = "SELECT * FROM \"__SNUsers\" WHERE \"FirstName\" LIKE @p0 AND \"LastName\" LIKE @p1";
                var users = await _userManagerHandler.ExecuteRawQueryAsync<UserModel>(
                    sql,
                    $"%{firstName}%",
                    $"%{lastName}%"
                );

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest($"Search failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the total number of users in the system.
        /// </summary>
        /// <returns>Total user count</returns>
        /// <response code="200">Returns the total user count</response>
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetTotalUsersCount()
        {
            return await _userManagerHandler.GetNumberOfUsersAsync();
        }

        /// <summary>
        /// Gets the number of users by specific role.
        /// </summary>
        /// <param name="role">The role to count users for</param>
        /// <returns>User count for the specified role</returns>
        /// <response code="200">Returns the user count for the role</response>
        /// <response code="400">Invalid role specified</response>
        [HttpGet("count-by-role")]
        public async Task<ActionResult<int>> GetUsersCountByRole([FromQuery] TRole role)
        {
            return await _userManagerHandler.GetNumberOfUsersByRoleAsync<TRole>(role);
        }

        /// <summary>
        /// Virtual authorization hook - can be overridden by implementations for custom auth logic
        /// </summary>
        /// <param name="action">The action being performed (e.g., "create", "delete", "update")</param>
        /// <param name="userId">Optional user ID for user-specific operations</param>
        /// <param name="additionalData">Any additional data needed for authorization</param>
        /// <returns>True if authorized, false otherwise</returns>
        protected virtual async Task<bool> CanPerformActionAsync(Actions action, int? userId = null, object? additionalData = null)
        {
            // Default implementation - always allow (no authorization)
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// Virtual hook for data validation beyond model validation
        /// </summary>
        /// <param name="action">The action being performed</param>
        /// <param name="data">The data being validated</param>
        /// <returns>Validation result</returns>
        protected virtual async Task<(bool IsValid, string? ErrorMessage)> ValidateBusinessRulesAsync(Actions action, object data)
        {
            // Default implementation - always valid
            await Task.CompletedTask;
            return (true, null);
        }

        /// <summary>
        /// Virtual hook for logging/auditing actions
        /// </summary>
        /// <param name="action">The action performed</param>
        /// <param name="oldData">The old data</param>
        /// <param name="newData">The new data</param>
        protected virtual async Task RecordActionAsync(Actions action, object oldData, object newData)
        {
            // Default implementation - do nothing
            await Task.CompletedTask;
        }
    }
}

