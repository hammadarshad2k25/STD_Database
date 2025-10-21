using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using STD_Database.DTO;
using STD_Database.Repositories;
using System.CodeDom.Compiler;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace STD_Database.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class UserAuthenticationController : ControllerBase
    {
        private readonly IConfiguration _Config;
        private readonly IUnitOfWork _UOW;
        public UserAuthenticationController(IConfiguration config, IUnitOfWork uow)
        {
            _Config = config;
            _UOW = uow;
        }
        /// <summary>
        /// Authenticates a user and generates JWT + refresh token.
        /// </summary>
        /// <param name="Request">Login credentials (username and password).</param>
        /// <returns>Access and refresh tokens if authentication succeeds.</returns>
        [HttpPost("Login")]
        public async Task<IActionResult> CheckAuth([FromBody] LoginDTO Request)
        {
            if (Request == null)
                return BadRequest("Invalid login request.");

            var user = await _UOW.UserRepo.GetUserAsync(Request.username, Request.password);
            if (user == null)
                return Unauthorized("Invalid username or password.");

            var accessToken = GenerateJwtToken(user.username, user.role);
            var refreshToken = GenerateRefreshToken();

            var dbUser = await _UOW.UserRepo.GetDbUserByUserName(user.username);
            if (dbUser is null)
                return Unauthorized("Invalid user record.");

            dbUser.RefreshToken = refreshToken;
            dbUser.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await _UOW.CompleteAsync();

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = user
            });
        }
        /// <summary>
        /// Generates a new access token using a valid refresh token.
        /// </summary>
        /// <param name="Request">The expired access token and refresh token.</param>
        /// <returns>New JWT and refresh token.</returns>
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDTO Request)
        {
            if (Request is null)
                return BadRequest("Invalid client request.");

            var principal = GetPrincipalFromExpiredToken(Request.accessToken);
            var username = principal.Identity?.Name;
            if (username == null)
                return Unauthorized("Invalid token.");

            var user = await _UOW.UserRepo.GetDbUserByUserName(username);
            if (user == null || user.RefreshToken != Request.refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return Unauthorized("Invalid or expired refresh token.");

            var newJwtToken = GenerateJwtToken(user.UserName, user.Role);
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;

            await _UOW.CompleteAsync();

            return Ok(new
            {
                AccessToken = newJwtToken,
                RefreshToken = newRefreshToken
            });
        }
        /// <summary>
        /// Verifies a bcrypt password hash (for testing only).
        /// </summary>
        [HttpGet("test-bcrypt")]
        public IActionResult TestBcrypt()
        {
            string password = "admin123";
            string hash = "$2a$11$ttPpgakGkFhJqEEdMDjOYeRDuIY4NXXv4T/g2SbdL7y3mcNIBp4Ja";
            bool result = BCrypt.Net.BCrypt.Verify(password, hash);
            return Ok(new { password, hash, verified = result });
        }
        /// <summary>
        /// Generates a new bcrypt hash (for testing only).
        /// </summary>
        [HttpGet("generate-hash")]
        public IActionResult GenerateHash()
        {
            string password = "teacher123";
            string hash = BCrypt.Net.BCrypt.HashPassword(password);
            return Ok(new { password, hash });
        }
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Config["Jwt:Key"]!)),
                ValidateLifetime = false // Ignore expiration
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token.");

            return principal;
        }
        private string GenerateJwtToken(string username, string role)
        {
            var SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Config["Jwt:Key"]));
            var Credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
            var Claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };
            var Token = new JwtSecurityToken
            (
                issuer: _Config["Jwt:Issuer"],
                audience: _Config["Jwt:Audience"],
                claims:Claims,
                expires:DateTime.Now.AddHours(1),
                signingCredentials:Credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(Token);
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
