using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using STD_Database.DTO;
using Xunit;

namespace STD_Database.IntegrationTests
{
    public class AddStdDocker : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AddStdDocker(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AddDocReal()
        {
            var login = new LoginDTO
            {
                username = "admin",
                password = "admin123"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/UserAuthentication/Login", login);
            loginResponse.EnsureSuccessStatusCode();

            var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
            tokens.Should().NotBeNull();
            tokens!.AccessToken.Should().NotBeNullOrEmpty();

            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            var newSTD = new StudentModelDTO
            {
                name = "Docker User",
                rollnumber = new Random().Next(1000, 9999),
                degree = "Information Technology",
                semester = 3,
                cgpa = 3.5,
            };

            var response = await _client.PostAsJsonAsync("/api/v1/StudentControllerEF/Add-Student", newSTD);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        private class TokenResponse
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}
