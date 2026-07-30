using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using LearnHub.Models.Entities;

namespace LearnHub.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.Parse(value!);
        }

        public static Role GetRole(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.Role);
            return Enum.Parse<Role>(value!);
        }
    }
}
