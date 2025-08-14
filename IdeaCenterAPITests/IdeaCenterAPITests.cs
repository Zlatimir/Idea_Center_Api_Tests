using IdeaCenterAPITests.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace IdeaCenterAPITests
{
    public class IdeaCenterApiTests
    {        
        private static readonly string Email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        private static readonly string Password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");
        private string jwtToken = string.Empty;

        private RestClient client;
        private static readonly string baseUrl = Environment.GetEnvironmentVariable("BASE_URL");

        private static string lastCreatedIdeaId;
        private static string lastCreatedIdeaTitle;
        private static string lastCreatedIdeaDescription;
        private const string IdeaImageUrl = "https://cdn.pixabay.com/photo/2016/03/30/02/21/idea-1289871_640.jpg";

        [OneTimeSetUp]
        public void Setup()
        {
            jwtToken = GetJwtToken(Email, Password);

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var random = new Random().Next(1000, 9999);
            lastCreatedIdeaTitle = "Title" + random;
            lastCreatedIdeaDescription = "Description" + random;
            
            var newIdea = new IdeaDTO
            {
                Title = lastCreatedIdeaTitle,
                Description = lastCreatedIdeaDescription,
                Url = IdeaImageUrl
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(newIdea);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseData.Msg, Is.EqualTo("Successfully created!"), "Expected success message");
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");

            var responseData = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(responseData, Is.Not.Null, "Responce is null");
            Assert.That(responseData.Count, Is.GreaterThan(0), "Expected at least one idea in the response");

            lastCreatedIdeaId = responseData.Last().Id;                        
        }

        [Order(3)]
        [Test]
        public void EditLastCreatedIdea_ShouldReturnSuccess()
        {
            var updatedIdea = new IdeaDTO
            {
                Title = lastCreatedIdeaTitle + " Updated",
                Description = lastCreatedIdeaDescription + " Updated",
                Url = IdeaImageUrl
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(updatedIdea);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseData.Msg, Is.EqualTo("Edited successfully"), "Expected success message");
        }

        [Order(4)]
        [Test]
        public void DeleteLastCreatedIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);


            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");            

            Assert.That(response.Content, Does.Contain("The idea is deleted!"), "Expected success message");
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var newIdea = new IdeaDTO
            {
                Title = string.Empty,
                Description = lastCreatedIdeaDescription,
                Url = IdeaImageUrl
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(newIdea);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest");
        }

        [Order(6)]
        [Test]
        public void EditNonExistingIdea_ShouldReturnBadRequest()
        {
            var updatedIdea = new IdeaDTO
            {
                Title = lastCreatedIdeaTitle + " Updated",
                Description = lastCreatedIdeaDescription + " Updated",
                Url = IdeaImageUrl
            };

            var request = new RestRequest($"/api/Idea/Edit/", Method.Put);
            request.AddQueryParameter("ideaId", $"{ lastCreatedIdeaId}00");
            request.AddJsonBody(updatedIdea);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest");            

            Assert.That(response.Content, Does.Contain("There is no such idea!"), "Expected warning message");
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingIdea_ShouldReturnBadRequest()
        {
            var request = new RestRequest($"/api/Idea/Delete/", Method.Delete);
            request.AddQueryParameter("ideaId", $"{lastCreatedIdeaId}00");

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest");
            
            Assert.That(response.Content, Does.Contain("There is no such idea!"), "Expected warning message");
        }

        [OneTimeTearDown]
        public void TearDown() 
        {
            client.Dispose();
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = tempClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Authentication failed: {response.Content}");
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var token = responseData.GetProperty("accessToken").GetString();

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Authentication failed: No token received.");
            }

            return token;
        }
     }
}