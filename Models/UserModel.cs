using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManager.Models;

public class UserModel
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required]
    public string UserName { get; set; }

    public string? Status { get; set; }

    public string? Role { get; set; }

    public string? Theme { get; set; }

    public string? Permission { get; set; }
    public string? Contact { get; set; }
    public string? Password { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? ProfilePicture { get; set; }
    public List<UserMetadata> Metadata { get; set; } = new();

    public void SetDisplayName()
    {
        DisplayName = $"{FirstName} {LastName}";
    }
}