using System.ComponentModel.DataAnnotations;

namespace CoinBank.Services._User.DTOs.Updates
{
    public class AdminLoginUpdate
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
