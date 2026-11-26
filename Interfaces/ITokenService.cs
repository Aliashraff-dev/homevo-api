using HomevoAPI.Models;

namespace HomevoAPI.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateToken(ApplicationUser user);
        string GenerateRefreshToken();
    }
}