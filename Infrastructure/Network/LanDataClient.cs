using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Security;

namespace QuanLyHoSo.Infrastructure.Network
{
    public sealed class LanDataClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Timer _heartbeatTimer;

        public LanDataClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(AppPathSettings.Current.AdminServerUrl + "/"),
                Timeout = TimeSpan.FromSeconds(5)
            };
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-QuanLyHoSo-Client", Environment.MachineName);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _heartbeatTimer = new Timer(_ => SendHeartbeat(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public void Ping()
        {
            Call<object>("health", null);
        }

        private void SendHeartbeat()
        {
            try
            {
                Ping();
            }
            catch
            {
                // Heartbeat is best-effort and must never interrupt client work.
            }
        }

        public T Call<T>(string route, object data)
        {
            try
            {
                var envelope = new LanApiEnvelope<object>
                {
                    User = AuthContext.CurrentUser,
                    Data = data
                };
                var json = JsonSerializer.Serialize(envelope, _jsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = _httpClient.PostAsync($"api/{route}", content).GetAwaiter().GetResult();
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(responseBody)
                        ? $"Máy admin trả về lỗi HTTP {(int)response.StatusCode}."
                        : responseBody);
                }

                if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(responseBody))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseBody, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new LanServerUnavailableException(AppPathSettings.Current.AdminServerUrl, ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new LanServerUnavailableException(AppPathSettings.Current.AdminServerUrl, ex);
            }
        }

        public void DownloadFile(string route, object data, string destinationPath)
        {
            try
            {
                var envelope = new LanApiEnvelope<object>
                {
                    User = AuthContext.CurrentUser,
                    Data = data
                };
                var json = JsonSerializer.Serialize(envelope, _jsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = _httpClient.PostAsync($"api/{route}", content).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(responseBody)
                        ? $"MÃ¡y admin tráº£ vá» lá»—i HTTP {(int)response.StatusCode}."
                        : responseBody);
                }

                using var remoteStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                using var fileStream = System.IO.File.Create(destinationPath);
                remoteStream.CopyTo(fileStream);
            }
            catch (HttpRequestException ex)
            {
                throw new LanServerUnavailableException(AppPathSettings.Current.AdminServerUrl, ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new LanServerUnavailableException(AppPathSettings.Current.AdminServerUrl, ex);
            }
        }

        public void Dispose()
        {
            _heartbeatTimer.Dispose();
            _httpClient.Dispose();
        }
    }
}
