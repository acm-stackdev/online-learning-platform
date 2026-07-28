using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearnHub.Tests.Fixtures
{
    public static class ControllerTestHelpers
    {
        public static ControllerContext BuildControllerContext(ClaimsPrincipal? user = null, string? cookieHeader = null)
        {
            var httpContext = new DefaultHttpContext();

            if (cookieHeader is not null)
                httpContext.Request.Headers["Cookie"] = cookieHeader;

            if (user is not null)
                httpContext.User = user;

            return new ControllerContext { HttpContext = httpContext };
        }

        public static ClaimsPrincipal BuildUserPrincipal(long userId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
            }, authenticationType: "TestAuth");

            return new ClaimsPrincipal(identity);
        }
    }
}
