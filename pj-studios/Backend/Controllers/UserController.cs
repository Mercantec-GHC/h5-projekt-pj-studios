using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        // ---------------- LOGIN ----------------
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO userDTO)
        {
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == userDTO.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(userDTO.Password, user.HashedPassword))
                return Unauthorized("Invalid credentials");

            var token = GenerateToken(user);

            return Ok(new { token });
        }

        // ---------------- ADD SCORE ----------------
        [Authorize]
        [HttpPost("addscore")]
        public async Task<IActionResult> AddScore(UserScoreDTO scoreDto)
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User not found");

            user.LastScores ??= new List<int>();
            user.LastScores.Add(scoreDto.Score);

            if (user.HighScore == null || scoreDto.Score > user.HighScore)
                user.HighScore = scoreDto.Score;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Score saved",
                highScore = user.HighScore
            });
        }

        // ---------------- TOKEN ----------------
        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

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
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}