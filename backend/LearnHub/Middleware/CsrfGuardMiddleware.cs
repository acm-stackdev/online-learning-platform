namespace LearnHub.Middleware
{
    public class CsrfGuardMiddleware
    {
        private const string RequiredHeader = "X-Requested-With";
        private const string RequiredValue = "LearnHub";
        private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
            { "POST", "PUT", "PATCH", "DELETE" };

        private readonly RequestDelegate _next;

        public CsrfGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var hasAuthCookie = context.Request.Cookies.ContainsKey("access_token");
            var isMutating = MutatingMethods.Contains(context.Request.Method);

            if (hasAuthCookie && isMutating && context.Request.Headers[RequiredHeader] != RequiredValue)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "Missing required request header." });
                return;
            }

            await _next(context);
        }
    }
}
