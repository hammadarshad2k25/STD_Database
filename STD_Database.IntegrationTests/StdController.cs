using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using STD_Database.DTO;
using Xunit;

namespace STD_Database.IntegrationTests
{
    public class StdController : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        public StdController(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }
        private async Task<string> AuthenticateAsync(string UserName, string PassWord)
        {
            var loginData = new LoginDTO
            {
                username = UserName,
                password = PassWord
            };
            var response = await _client.PostAsJsonAsync("/api/v1/UserAuthentication/Login", loginData);
            response.EnsureSuccessStatusCode();
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDTO>();
            return tokenResponse?.AccessToken ?? string.Empty;
        }
        [Fact]
        public async Task GetAllStudents_ReturnsOk()
        {
            var token =  await AuthenticateAsync("teacher", "teacher123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var Response = await _client.GetAsync("/api/v1/StudentControllerEF/Display-Students");
            Response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
        [Fact]
        public async Task AddSTD()
        {
            var token = await AuthenticateAsync("admin", "admin123");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var newSTD = new StudentModelDTO
            {
                name = "John Doe",
                rollnumber = new Random().Next(1000, 9999),
                degree = "Computer Science",
                semester = 5,
                cgpa = 3.8,
            };
            var response = await _client.PostAsJsonAsync("/api/v1/StudentControllerEF/Add-Student", newSTD);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
        private class TokenResponseDTO
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}
