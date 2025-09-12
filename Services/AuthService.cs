using Microsoft.IdentityModel.Tokens;
using SigmaNotificationBackend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SigmaNotificationBackend.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public (string? token, SVN_Users? user) Authenticate(LoginRequest request)
        {
            var user = _context.SVN_Users.FirstOrDefault(u =>
                u.name_user == request.NameUser &&
                u.password_user == request.Password
            );

            if (user == null) return (null, null);

            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id_user", user.id_user.ToString()),
                new Claim("name_user", user.name_user),
                new Claim("svn_number", user.svn_number),
                new Claim("department_id", user.id_department.ToString()),
                new Claim("img_user", user.img_user)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiresInMinutes"]!)),
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            return (accessToken, user);
        }
    }
}
