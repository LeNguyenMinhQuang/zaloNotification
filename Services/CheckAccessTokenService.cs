using System.IdentityModel.Tokens.Jwt;
using SigmaNotificationBackend.Models;
using Microsoft.EntityFrameworkCore;

public class CheckAccessTokenService
{
    private readonly AppDbContext _context;

    public CheckAccessTokenService(AppDbContext context)
    {
        _context = context;
    }

    public (SVN_Users? User, string Status) CheckToken(string token)
{
    var handler = new JwtSecurityTokenHandler();
    try
    {
        var jwtToken = handler.ReadJwtToken(token);
        var idUserStr = jwtToken.Claims.FirstOrDefault(c => c.Type == "id_user")?.Value;

        if (!int.TryParse(idUserStr, out int idUser))
            return (null, "Invalid Token");

        var user = _context.SVN_Users
                    .Include(u => u.Department)
                    .FirstOrDefault(u => u.id_user == idUser);

        return user != null ? (user, "OK") : (null, "User Not Found");
    }
    catch
    {
        return (null, "Invalid Token");
    }
}

}
