using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/authenticator")]
    public class AuthenticatorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthenticatorController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registers a new user account. Role: Organiser or Participant.
        /// Date of birth and phone number are optional. Password is hashed before storage.
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<object>> Register(
            [FromQuery(Name = "email")] string email,
            [FromQuery(Name = "password")] string password,
            [FromQuery(Name = "firstname")] string firstname,
            [FromQuery(Name = "surname")] string surname,
            [FromQuery(Name = "role")] UserRole role,
            [FromQuery(Name = "dateOfBirth")] DateTime? dateOfBirth,
            [FromQuery(Name = "phoneNumber")] string? phoneNumber)
        {
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return Conflict(new { message = "Email is already in use." });
            }

            var user = new User
            {
                FirstName = firstname,
                Surname = surname,
                Email = email,
                DateOfBirth = dateOfBirth,
                PhoneNumber = phoneNumber,
                Role = role.ToString()
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { id = user.UserId }, new
            {
                user.UserId,
                user.FirstName,
                user.Surname,
                user.Email,
                user.DateOfBirth,
                user.PhoneNumber,
                user.Role
            });
        }

        /// <summary>
        /// Authenticates a user and creates a server-side session.
        /// The session stores the user's id, email, and role.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<object>> Login(
            [FromQuery(Name = "email")] string email,
            [FromQuery(Name = "password")] string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.Surname}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok(new
            {
                user.UserId,
                user.FirstName,
                user.Surname,
                user.Email,
                user.Role
            });
        }

        /// <summary>
        /// Signs the current user out, clearing their server-side session.
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out." });
        }
    }
}
