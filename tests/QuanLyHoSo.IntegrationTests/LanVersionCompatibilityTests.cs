using System;
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
        public async Task LanServer_ShouldAllowClientVersionToDifferFromServerVersion()
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
                Assert.Null(health.RequiredClientVersion);
                Assert.True(health.IsClientVersionSupported);

                using var loginResponse = await client.PostAsync("api/auth/login", new StringContent("{}", Encoding.UTF8, "application/json"));
                Assert.NotEqual((HttpStatusCode)426, loginResponse.StatusCode);
            }
            finally
            {
                server.Stop();
            }
        }

        [Fact]
        [Trait("Category", "Regression")]
        [Trait("Category", "Integration")]
        public async Task LanClient_ShouldAdaptLegacyVersionHeaderForOlderServer()
        {
            using var database = new TestDatabase();
            var portProbe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            portProbe.Start();
            var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
            portProbe.Stop();

            var serverUrl = "http://127.0.0.1:" + port;
            AppPathSettings.UseServerMode(database.DatabasePath, System.IO.Path.Combine(database.RootPath, "logs"), serverUrl);

            using var listener = new HttpListener();
            listener.Prefixes.Add(serverUrl + "/");
            listener.Start();

            string initialLegacyVersion = null;
            string actualClientVersion = null;
            string adaptedLegacyVersion = null;
            var legacyRequiredVersion = string.Equals(LanProtocolVersion.Current, "9.9.9", StringComparison.OrdinalIgnoreCase)
                ? "8.8.8"
                : "9.9.9";
            var legacyServer = Task.Run(async () =>
            {
                var healthContext = await listener.GetContextAsync();
                initialLegacyVersion = healthContext.Request.Headers[LanProtocolVersion.ClientVersionHeader];
                actualClientVersion = healthContext.Request.Headers[LanProtocolVersion.ActualClientVersionHeader];
                await WriteJsonAsync(healthContext, new LanHealthResponse
                {
                    Ok = true,
                    ServerVersion = legacyRequiredVersion,
                    RequiredClientVersion = legacyRequiredVersion,
                    IsClientVersionSupported = false
                });

                var loginContext = await listener.GetContextAsync();
                adaptedLegacyVersion = loginContext.Request.Headers[LanProtocolVersion.ClientVersionHeader];
                await WriteJsonAsync(loginContext, null);
            });

            try
            {
                using var client = new LanDataClient();
                client.Ping();
                client.Call<object>("auth/login", new LoginRequest());
                await legacyServer.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.Equal(LanProtocolVersion.Current, initialLegacyVersion);
                Assert.Equal(LanProtocolVersion.Current, actualClientVersion);
                Assert.NotEqual(initialLegacyVersion, adaptedLegacyVersion);
                Assert.Equal(legacyRequiredVersion, adaptedLegacyVersion);
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task WriteJsonAsync(HttpListenerContext context, object value)
        {
            var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = content.Length;
            await context.Response.OutputStream.WriteAsync(content);
            context.Response.Close();
        }
    }
}
