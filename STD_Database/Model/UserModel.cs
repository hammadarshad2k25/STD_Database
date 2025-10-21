using System.ComponentModel.DataAnnotations;

namespace STD_Database.Model
{
    public class UserModel
    {
        [Key]
        public int ID { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string Role { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
