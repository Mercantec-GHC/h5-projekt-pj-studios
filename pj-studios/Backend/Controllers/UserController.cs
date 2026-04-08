using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
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

        var user = CreateUserDTO(userDTO);

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();  
        } catch
        {
            return StatusCode(500);
        }
        return Ok("User created successfully!");
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
            CreatedAt = DateTime.UtcNow.AddHours(1),
            UpdatedAt = DateTime.UtcNow.AddHours(1),
            LastScores = new List<int>()
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDTO userLoginDTO)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userLoginDTO.Email);

        if (user is null)
        {
            return Unauthorized("Invalid email or password");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(userLoginDTO.Password, user.HashedPassword);
        if (!isPasswordValid)
        {
            return Unauthorized("Invalid email or password");
        }

        return Ok("Login successful");
    }

    //[HttpGet]
    // Get users own info

    //[HttpPatch("updateUser")]
    // Update user

    //[HttpDelete("deleteUser")]
    // Delete user
}