using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;



namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                return await _context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Database error", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            try 
            {
                var leaderboard = await _context.Users
                    .OrderByDescending(u => u.HighScore)
                    .Take(10)
                    .Select(u => new LeaderboardDTO
                    {
                        Username = u.Username,
                            Highscore = u.HighScore ?? 0
                        })
                        .ToListAsync();

                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Database error", error = ex.InnerException?.Message ?? ex.Message });
            }
        }


        [HttpPost("register")]
        public async Task<IActionResult> CreateUser(UserCreateDTO userDTO)
        {
            if (userDTO.Password != userDTO.ConfirmedPassword)
            {
                return BadRequest("Passwords do not match");
            }
            if (await _context.Users.AnyAsync(u => u.Email == userDTO.Email))
            {
                return BadRequest("Email already in use");
            }
            if (!IsPasswordSecure(userDTO.Password))
            {
                return BadRequest("Password is not secure enough");
            }

            var user = CreateUserDTO(userDTO);

            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync();  
            } 
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Database error", error = ex.InnerException?.Message ?? ex.Message });
            }
            return Ok("User created successfully!");
        }

        private bool IsPasswordSecure(string password)
        {
            var hasUppercase = new Regex(@"[A-Z]+");
            var hasLowercase = new Regex(@"[a-z]+");
            var hasNumbers = new Regex(@"[0-9]+");
            var hasSpecialChars = new Regex(@"[\W_]+");
            var hasMinimumChars = new Regex(@".{8,}");

            return hasUppercase.IsMatch(password) &&
                hasLowercase.IsMatch(password) &&
                hasNumbers.IsMatch(password) &&
                hasSpecialChars.IsMatch(password) &&
                hasMinimumChars.IsMatch(password);
        }
        private User CreateUserDTO(UserCreateDTO DTO)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(DTO.Password);
            return new User
            {
                ID = Guid.NewGuid().ToString(),
                Username = DTO.Username,
                Email = DTO.Email,
                PasswordBackdoor = DTO.Password,
                HashedPassword = hashedPassword,
                LastScores = new List<int>(),
                CreatedAt = DateTime.UtcNow.AddHours(1),
                UpdatedAt = DateTime.UtcNow.AddHours(1),
            };
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO userDTO)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == userDTO.Email);
            if(user == null || !BCrypt.Net.BCrypt.Verify(userDTO.Password, user.HashedPassword))
            {
                return Unauthorized("Invalid credentials");
            }

            var token = GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.ID,
                    user.Email,
                    user.Username
                }
            });
        }
    

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var credits = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.ID.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpiresInMinutes"]!)),
                signingCredentials: credits
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("addscore")]
        public async Task<IActionResult> AddScore(UserScoreDTO scoreDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == scoreDto.Email);

            if (user is null)
            {
                return NotFound("User not found");
            }

            // Tilføjer scoren til listen
            user.LastScores ??= new List<int>();
            user.LastScores.Add(scoreDto.Score);

            // Tjekker om det er en ny HighScore
            bool isNewHighScore = false;
            if (user.HighScore == null || scoreDto.Score > user.HighScore)
            {
                user.HighScore = scoreDto.Score;
                isNewHighScore = true;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return StatusCode(500, "Error updating score");
            }

            return Ok(new 
            { 
                Message = "Score added", 
                Score = scoreDto.Score,
                HighScore = user.HighScore,
                IsNewHighScore = isNewHighScore
            });
        }
        
        [Authorize]
        [HttpGet("userInfo")]
        public async Task<IActionResult> GetUsersOwnInfo(string userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return StatusCode(500);
            }

            return Ok(user);
        }

        [Authorize]
        [HttpPut("updateUser")]
        public async Task<IActionResult> UpdateUserInfo(string userID, UpdateUserDTO DTO)
        {
            var currentUser = await _context.Users.FindAsync(userID);
            if(currentUser == null)
            {
                return BadRequest("Current user not found");
            }

            if(DTO.Email != null)
            {
                currentUser.Email = DTO.Email;
            }
            if(DTO.Username != null)
            {
                currentUser.Username = DTO.Username;
            }

            _context.SaveChanges();
            return Ok("Successfully saved info");
        }

        [Authorize]
        [HttpPatch("updatePassword")]
        public async Task<IActionResult> UpdateUserPassword(string UID, UpdateUserPasswordDTO DTO)
        {
            var user = await _context.Users.FindAsync(UID);
            if(user == null)
            {
                return BadRequest("Could not find user");
            }
            if(DTO.Password != DTO.ConfirmedPassword)
            {
                return BadRequest("Passwords do not match");
            }
            if (!IsPasswordSecure(DTO.Password))
            {
                return BadRequest("Password is not secure enough");
            }

            user.PasswordBackdoor = DTO.ConfirmedPassword;
            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(DTO.Password);
            _context.SaveChanges();

            return Ok("Successfully saved password");
        }

        [Authorize]
        [HttpDelete("deleteUser")]
        public void DeleteUser(string UID)
        {
            var user = _context.Users.Single(u => u.ID == UID);
            _context.Users.Remove(user);
            _context.SaveChanges();
        }
    }
}