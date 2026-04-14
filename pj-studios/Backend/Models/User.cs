using System.ComponentModel.DataAnnotations;
namespace Backend.Models
{
    public class User : Common
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string HashedPassword { get; set; } = string.Empty;
        public string PasswordBackdoor { get; set; } = string.Empty; // ONLY FOR DEMO PURPOSES!
        public int? HighScore { get; set; } = 0;
        public List<int> LastScores { get; set; } = new List<int>();
    }

    public class UserLoginDTO
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class UserCreateDTO
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmedPassword { get; set; }
    }

    public class UpdateUserDTO
    {
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }
    }

    public class UpdateUserPasswordDTO
    {
        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmedPassword { get; set; }
    }
  
    public class UserScoreDTO
    {
        // Vi bruger Email til at identificere brugeren, indtil der implementeres JWT
        public string Email { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    public class LeaderboardDTO
    {
        public string Username { get; set; } = string.Empty;
        public int Highscore { get; set; }
    }
}
