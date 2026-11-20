using System.ComponentModel.DataAnnotations;

namespace game_match.Models

{
    public class Activity
    {

        public int Id { get; set; }
        public int OwnerId { get; set; }

        public Activity()
        {

        }

        public Activity(int OwnerId)
        {
            this.OwnerId = OwnerId;
        }

        [Required(ErrorMessage = "Du mangler titel til din aktivitet")]
        [StringLength(70, ErrorMessage = "Maks. 70 tegn")]
        public string ActivityTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Du mangler mere info om din aktivitet")]
        [StringLength(300, ErrorMessage = "Maks. 300 tegn")]
        public string ActivityInfo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Skriv, hvor din aktivitet foregår")]
        [StringLength(50, ErrorMessage = "Maks. 50 tegn")]
        public string Address { get; set; } = string.Empty;

        public DateTime ActivityStartTime { get; set; } = DateTime.Now.AddHours(1);

        [Required]
        [Range(1, 25, ErrorMessage = "Der er min. 1 og maks. 25, der kan deltage i samme aktivitet")]
        public int MaxParticipant { get; set; } = 1;

        public List<User> users = new List<User>();
    }
}
