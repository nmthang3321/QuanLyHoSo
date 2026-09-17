using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Network;
using Xunit;

namespace QuanLyHoSo.IntegrationTests
{
    public sealed class LanVersionCompatibilityTests
    {
        [Fact]
        [Trait("Category", "Regression")]
        [Trait("Category", "Integration")]
        public async Task LanServer_ShouldReportHealthAndRejectMismatchedClientVersion()
        {
            using var database = new TestDatabase();
            var portProbe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            portProbe.Start();
            var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
            portProbe.Stop();

            var serverUrl = "http://127.0.0.1:" + port;
            AppPathSettings.UseServerMode(database.DatabasePath, System.IO.Path.Combine(database.RootPath, "logs"), serverUrl);
            var server = new LanDataServer(database.Service);
            server.Start();

            try
            {
                using var client = new HttpClient { BaseAddress = new System.Uri(serverUrl + "/") };
                client.DefaultRequestHeaders.TryAddWithoutValidation(LanProtocolVersion.ClientVersionHeader, "0.9.0");

                using var healthResponse = await client.PostAsync("api/health", new StringContent("{}", Encoding.UTF8, "application/json"));
                var healthJson = await healthResponse.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<LanHealthResponse>(healthJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
                Assert.Equal(LanProtocolVersion.Current, health.ServerVersion);
                Assert.Equal(LanProtocolVersion.Current, health.RequiredClientVersion);
                Assert.False(health.IsClientVersionSupported);

                using var loginResponse = await client.PostAsync("api/auth/login", new StringContent("{}", Encoding.UTF8, "application/json"));
                var error = await loginResponse.Content.ReadAsStringAsync();

                Assert.Equal((HttpStatusCode)426, loginResponse.StatusCode);
                Assert.Contains(LanProtocolVersion.Current, error);
            }
            finally
            {
                server.Stop();
            }
        }
    }
}
