namespace UserManager.Dtos
{
    /// <summary>  
    /// Represents the data transfer object for creating a user.  
    /// </summary>  
    public class CreateUserDto
    {
        /// <summary>  
        /// Gets or sets the first name of the user.  
        /// </summary>  
        public required string FirstName { get; set; }

        /// <summary>  
        /// Gets or sets the last name of the user.  
        /// </summary>  
        public required string LastName { get; set; }

        /// <summary>  
        /// Gets or sets the email address of the user.  
        /// </summary>  
        public required string Email { get; set; }

        /// <summary>  
        /// Gets or sets the username of the user.  
        /// </summary>  
        public required string UserName { get; set; }

        /// <summary>  
        /// Gets or sets the status of the user.  
        /// </summary>  
        public string? Status { get; set; }

        /// <summary>  
        /// Gets or sets the role of the user.  
        /// </summary>  
        public string? Role { get; set; }

        /// <summary>  
        /// Gets or sets the theme preference of the user.  
        /// </summary>  
        public string? Theme { get; set; }

        /// <summary>  
        /// Gets or sets the permission level of the user.  
        /// </summary>  
        public string? Permission { get; set; }

        /// <summary>  
        /// Gets or sets the contact information of the user.  
        /// </summary>  
        public string? Contact { get; set; }

        /// <summary>  
        /// Gets or sets the password of the user.  
        /// </summary>  
        public string? Password { get; set; }

        /// <summary>  
        /// Gets or sets the profile picture URL of the user.  
        /// </summary>  
        public string? ProfilePicture { get; set; }

        /// <summary>  
        /// Gets or sets the metadata associated with the user.  
        /// </summary>  
        public Dictionary<string, string>? Metadata { get; set; }
    }
}