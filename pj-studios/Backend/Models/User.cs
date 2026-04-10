using System.ComponentModel.DataAnnotations;
namespace Backend.Models
{
    public class User : Common
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string HashedPassword { get; set; }
        public string PasswordBackdoor { get; set; } // ONLY FOR DEMO PURPOSES!
        public int HighScore { get; set; }
        public List<int> LastScores { get; set; }
    }

    public class UserLoginDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class UserCreateDTO
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmedPassword { get; set; }
    }

    public class UpdateUserDTO
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class UpdateUserPasswordDTO
    {
        public string Password { get; set; }
        public string ConfirmedPassword { get; set; }
    }
  
    public class UserScoreDTO
    {
        // Vi bruger Email til at identificere brugeren, indtil der implementeres JWT
        public string Email { get; set; }
        public int Score { get; set; }
    }
}