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
        public string PasswordConfirm { get; set; }
    }
}   