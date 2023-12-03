using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using WebApplicationPOC1.Models;

namespace WebApplicationPOC1.Controllers
{
    public class HomeController : Controller
    {
        public string clientId = "4dc7eac21d584910b0339f149bff3e9e";
        public string clientSecret = "a6aa8fbdfb0d4590baa827aa4548ebf6";
        public string redirectUri = "https://localhost:7286/home/callback";
        public string authorizationEndpoint = "https://accounts.spotify.com/authorize";
        public string tokenEndpoint = "https://accounts.spotify.com/api/token";

        private readonly ILogger<HomeController> _logger;
        private static string TOKEN;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var redirectsUri = Uri.EscapeDataString(redirectUri);

            var authorizationUrl = $"{authorizationEndpoint}?" +
                $"response_type=code&client_id={clientId}&scope=user-follow-read%20user-top-read%20user-read-recently-played%20user-library-read&redirect_uri={redirectsUri}";

            return Redirect(authorizationUrl);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public async Task<IActionResult> Callback(string code)
        {
            TOKEN = GetTokenAuth(code);
            var topArtists = GetTopArtists();
            var topTracks = GetTopTracksByArtist("https://api.spotify.com/v1/artists/4gzpq5DPGxSnKTe4SA8HAU");
            // Pasar el token a la vista utilizando ViewBag
            ViewBag.Token = TOKEN;
            ViewBag.TopArtists = topArtists;
            ViewBag.TopTracks = topTracks;
            // Devolver la vista con el token
            return View("Privacy");
        }
        public string GetTokenAuth(string code)
        {
            string tokenResult = string.Empty;
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

            var credentials = $"{clientId}:{clientSecret}";
            var base64Credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));

            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Credentials);

            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            });

            using (var client = new HttpClient())
            {
                var response = client.SendAsync(tokenRequest).Result; // Bloquea hasta que se complete la tarea

                response.EnsureSuccessStatusCode();

                var json = response.Content.ReadAsStringAsync().Result; // Bloquea hasta que se complete la tarea
                tokenResult = JObject.Parse(json)["access_token"].Value<string>();
            }
            return tokenResult;
        }
        public JArray GetTopArtists()
        {

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7238/Spotify?token={TOKEN}");
            var client = new HttpClient();
            var response = client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            var rsul = response.Content.ReadAsStringAsync().Result;
            var result = JArray.Parse(rsul);            
            return result;
        }
        public JArray GetTopTracksByArtist(string hrefArtist)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7238/Spotify/GetTopTracks?token={TOKEN}&hrefArtist={hrefArtist}");
            var client = new HttpClient();
            var response = client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            var rsul = response.Content.ReadAsStringAsync().Result;
            var result = JArray.Parse(rsul);
            return result;
        }
    }
}