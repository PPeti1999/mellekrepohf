using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    namespace AuthService.Controllers
    {
        public record LoginRequest(string Username, string Password);
        public record LoginResponse(string Token);

        [ApiController]
        [Route("api/[controller]")]
        public class AuthController : ControllerBase
        {
            private readonly IConfiguration _configuration;

            public AuthController(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            [HttpPost("login")]
            public IActionResult Login([FromBody] LoginRequest request)
            {
                string role = "";
                
                // Hardcoded felhasználók (Demo célra)
                if (request.Username == "admin" && request.Password == "admin")
                {
                    role = "Admin";
                }
                else if (request.Username == "user" && request.Password == "user")
                {
                    role = "User";
                }
                else
                {
                    return Unauthorized("Hibás adatok! Próbáld: admin/admin vagy user/user");
                }

                var token = GenerateJwtToken(request.Username, role);
                return Ok(new LoginResponse(token));
            }

            private string GenerateJwtToken(string username, string role)
            {
                // A kulcsot a környezeti változókból olvassuk
                var jwtKey = _configuration["JWT:Key"] ?? "EzEgyNagyonHosszuEsTitkosKulcsAmiLegalabb32Karakter2026";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: "TicketMasterAuth",
                    audience: "TicketMasterServices",
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
        }
    }