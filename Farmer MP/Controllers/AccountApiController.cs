using Farmer_MP.Data;
using Farmer_MP.Models;
using Farmer_MP.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
.....
namespace Farmer_MP.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AccountApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/AccountApi/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || !VerifyPassword(model.Password, user.Password))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(new
            {
                user.UserID,
                user.Name,
                user.Email,
                user.UserType
            });
        }

        // POST: api/AccountApi/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest(new { message = "This email is already registered." });
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = HashPassword(model.Password),
                Phone = model.Phone,
                Address = model.Address,
                UserType = model.UserType,
                CreatedAt = DateTime.Now,
                IsEmailVerified = false,
                OTP = "",
                OTPExpiry = DateTime.Now.AddMinutes(10)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.UserID,
                user.Name,
                user.Email,
                user.UserType
            });
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string enteredPassword, string storedPassword)
        {
            string[] parts = storedPassword.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            var enteredHash = KeyDerivation.Pbkdf2(
                password: enteredPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            return storedHash.SequenceEqual(enteredHash);
        }
    }
}
