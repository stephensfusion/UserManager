using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UserManager.Models
{
    public class UserMetadata
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public UserModel User { get; set; }
    }
}