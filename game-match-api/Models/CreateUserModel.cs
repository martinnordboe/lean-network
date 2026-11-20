using System.ComponentModel.DataAnnotations;

namespace game_match_api.Models
{
    public class CreateUserModel
    {
        [Required(ErrorMessage = "Du mangler et brugernavn, flot :)")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Nej! Brugernavnet er for langt... eller for kort?")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Flot, du har glemt at skrive et password")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Password er for langt... eller for kort?")]
        public string Password { get; set; } = string.Empty;
    }
}
