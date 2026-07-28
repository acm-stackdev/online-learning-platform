using Microsoft.Extensions.Configuration;

namespace LearnHub.Tests.Fixtures
{
    public static class TestConfigurationFactory
    {
        public static IConfiguration Create() =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "unit-test-secret-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "learnhub-api",
                ["Jwt:Audience"] = "learnhub-client",
                ["Jwt:ExpiryMinutes"] = "15",
                ["Frontend:BaseUrl"] = "http://localhost:5173",
                ["Google:ClientId"] = "test-google-client-id",
            }).Build();
    }
}
