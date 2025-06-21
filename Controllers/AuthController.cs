using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using TaskSchedulingApp.DTOs;
using TaskSchedulingApp.Models;

namespace TaskSchedulingApp.Controllers
{
    /// <summary>
    /// Controller for handling user authentication and registration.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Registers a new user with the specified role (TeamLead or Developer).
        /// </summary>
        /// <param name="model">The registration data in <see cref="RegisterDto"/> format.</param>
        /// <returns>A success message if registration is successful.</returns>
        /// <response code="200">User registered successfully.</response>
        /// <response code="400">Invalid role or registration data provided.</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid || (model.Role != "TeamLead" && model.Role != "Developer"))
                return BadRequest(new { error = "Invalid role. Use 'TeamLead' or 'Developer'." });

            var user = new User
            {
                UserName = model.Username,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, model.Role);
            return Ok(new { message = "User registered successfully" });
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="model">The login credentials in <see cref="LoginDto"/> format.</param>
        /// <returns>A JWT token if authentication is successful.</returns>
        /// <response code="200">Authentication successful, returns JWT token.</response>
        /// <response code="401">Invalid username or password.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return Unauthorized(new { error = "Invalid username" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized(new { error = "Invalid password" });

            var token = await _tokenService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}