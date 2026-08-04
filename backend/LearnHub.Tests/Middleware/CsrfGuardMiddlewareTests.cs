using FluentAssertions;
using LearnHub.Middleware;
using Microsoft.AspNetCore.Http;

namespace LearnHub.Tests.Middleware
{
    public class CsrfGuardMiddlewareTests
    {
        private static (DefaultHttpContext Context, CsrfGuardMiddleware Sut, bool[] WasCalled) CreateSut()
        {
            var wasCalled = new[] { false };
            var middleware = new CsrfGuardMiddleware(_ =>
            {
                wasCalled[0] = true;
                return Task.CompletedTask;
            });
            var context = new DefaultHttpContext();
            return (context, middleware, wasCalled);
        }

        [Fact]
        public async Task InvokeAsync_GetWithCookieNoHeader_CallsNext()
        {
            var (context, sut, wasCalled) = CreateSut();
            context.Request.Method = "GET";
            context.Request.Headers["Cookie"] = "access_token=abc";

            await sut.InvokeAsync(context);

            wasCalled[0].Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_PostWithCookieNoHeader_Returns403()
        {
            var (context, sut, wasCalled) = CreateSut();
            context.Request.Method = "POST";
            context.Request.Headers["Cookie"] = "access_token=abc";

            await sut.InvokeAsync(context);

            wasCalled[0].Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task InvokeAsync_PostWithCookieAndHeader_CallsNext()
        {
            var (context, sut, wasCalled) = CreateSut();
            context.Request.Method = "POST";
            context.Request.Headers["Cookie"] = "access_token=abc";
            context.Request.Headers["X-Requested-With"] = "LearnHub";

            await sut.InvokeAsync(context);

            wasCalled[0].Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_PostWithNoCookie_CallsNext()
        {
            var (context, sut, wasCalled) = CreateSut();
            context.Request.Method = "POST";

            await sut.InvokeAsync(context);

            wasCalled[0].Should().BeTrue();
        }
    }
}
