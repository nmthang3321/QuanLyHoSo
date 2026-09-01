using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using QuanLyHoSo.Infrastructure.Configuration;
using QuanLyHoSo.Infrastructure.Security;

namespace QuanLyHoSo.Infrastructure.Network
{
    public sealed class LanDataClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public LanDataClient()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(AppPathSettings.Current.AdminServerUrl + "/"),
                Timeout = TimeSpan.FromSeconds(5)
            };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public void Ping()
        {
            Call<object>("health", null);
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
    }
}
