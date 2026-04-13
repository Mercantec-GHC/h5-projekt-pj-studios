using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

        // ---------------- GET ALL USERS ----------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                return await _context.Users.ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Database error", error = ex.Message });
            }
        }

        // ---------------- LEADERBOARD ----------------
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
                return StatusCode(500, new { message = "Database error", error = ex.Message });
            }
        }

        // ---------------- REGISTER ----------------
        [HttpPost("register")]
        public async Task<IActionResult> CreateUser(UserCreateDTO userDTO)
        {
            if (userDTO.Password != userDTO.ConfirmedPassword)
                return BadRequest("Passwords do not match");

            if (await _context.Users.AnyAsync(u => u.Email == userDTO.Email))
                return BadRequest("Email already in use");

            if (!IsPasswordSecure(userDTO.Password))
                return BadRequest("Password is not secure enough");

            var user = CreateUserDTO(userDTO);

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Database error", error = ex.Message });
            }

            return Ok("User created successfully!");
        }

        // ---------------- LOGIN ----------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO userDTO)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == userDTO.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(userDTO.Password, user.HashedPassword))
                return Unauthorized("Invalid credentials");

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

        // ---------------- ADD SCORE (JWT VERSION) ----------------
        [Authorize]
        [HttpPost("addscore")]
        public async Task<IActionResult> AddScore(UserScoreDTO scoreDto)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid token");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);

            if (user == null)
                return NotFound("User not found");

            user.LastScores ??= new List<int>();
            user.LastScores.Add(scoreDto.Score);

            bool isNewHighScore = false;

            if (scoreDto.Score > (user.HighScore ?? 0))
            {
                user.HighScore = scoreDto.Score;
                isNewHighScore = true;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating score", error = ex.Message });
            }

            return Ok(new
            {
                Message = "Score added",
                Score = scoreDto.Score,
                HighScore = user.HighScore,
                IsNewHighScore = isNewHighScore
            });
        }

        // ---------------- DELETE USER ----------------
        [Authorize]
        [HttpDelete("deleteUser")]
        public IActionResult DeleteUser(string UID)
        {
            var user = _context.Users.SingleOrDefault(u => u.ID == UID);

            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok("User deleted");
        }

        // ---------------- JWT TOKEN ----------------
        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.ID),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpiresInMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ---------------- PASSWORD CHECK ----------------
        private bool IsPasswordSecure(string password)
        {
            return Regex.IsMatch(password, @"[A-Z]+") &&
                   Regex.IsMatch(password, @"[a-z]+") &&
                   Regex.IsMatch(password, @"[0-9]+") &&
                   Regex.IsMatch(password, @"[\W_]+") &&
                   Regex.IsMatch(password, @".{8,}");
        }

        // ---------------- CREATE USER ----------------
        private User CreateUserDTO(UserCreateDTO DTO)
        {
            return new User
            {
                ID = Guid.NewGuid().ToString(),
                Username = DTO.Username,
                Email = DTO.Email,
                PasswordBackdoor = DTO.Password,
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(DTO.Password),
                LastScores = new List<int>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}