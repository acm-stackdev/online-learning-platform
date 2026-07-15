using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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
    }
}
