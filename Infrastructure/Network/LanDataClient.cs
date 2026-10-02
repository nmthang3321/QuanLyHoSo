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
                Timeout = Timeout.InfiniteTimeSpan
            };
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-QuanLyHoSo-Client", Environment.MachineName);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(LanProtocolVersion.ClientVersionHeader, LanProtocolVersion.Current);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(LanProtocolVersion.ActualClientVersionHeader, LanProtocolVersion.Current);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _heartbeatTimer = new Timer(_ => SendHeartbeat(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public void Ping()
        {
            var health = GetHealth();
            if (health?.Ok != true)
            {
                throw new InvalidOperationException("Máy server không trả về trạng thái sẵn sàng.");
            }

            if (!health.IsClientVersionSupported && !string.IsNullOrWhiteSpace(health.RequiredClientVersion))
            {
                _httpClient.DefaultRequestHeaders.Remove(LanProtocolVersion.ClientVersionHeader);
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    LanProtocolVersion.ClientVersionHeader,
                    health.RequiredClientVersion.Trim());
            }
        }

        public LanHealthResponse GetHealth()
        {
            return Call<LanHealthResponse>("health", null);
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
            return Call<T>(route, data, TimeSpan.FromSeconds(5));
        }

        public T Call<T>(string route, object data, TimeSpan timeout)
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
                using var timeoutSource = new CancellationTokenSource(timeout);
                using var response = _httpClient.PostAsync($"api/{route}", content, timeoutSource.Token).GetAwaiter().GetResult();
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 426)
                    {
                        throw new LanVersionMismatchException(responseBody);
                    }

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
                using var timeoutSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                using var response = _httpClient.PostAsync($"api/{route}", content, timeoutSource.Token).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if ((int)response.StatusCode == 426)
                    {
                        throw new LanVersionMismatchException(responseBody);
                    }

                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(responseBody)
                        ? $"Máy admin trả về lỗi HTTP {(int)response.StatusCode}."
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
