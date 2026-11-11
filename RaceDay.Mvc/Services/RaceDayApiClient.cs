
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Services
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Error { get; set; } = "";
    }

    public class RaceDayApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _context;

        public RaceDayApiClient(
            HttpClient http,
            IHttpContextAccessor context)
        {
            _http = http;
            _context = context;
        }

        // Add the logged-in user's JWT token to API requests.
        private void AddToken()
        {
            var token = _context.HttpContext?
                .Session.GetString("Token");

            _http.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Handle successful and failed API responses.
        private async Task<ApiResult<T>> HandleResponse<T>(
            HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent ||
                    response.Content.Headers.ContentLength == 0)
                {
                    return new ApiResult<T>
                    {
                        Success = true
                    };
                }

                var responseText =
                    await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    return new ApiResult<T>
                    {
                        Success = true
                    };
                }

                try
                {
                    var data = JsonSerializer.Deserialize<T>(
                        responseText,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    return new ApiResult<T>
                    {
                        Success = true,
                        Data = data
                    };
                }
                catch (JsonException)
                {
                    return new ApiResult<T>
                    {
                        Success = false,
                        Error = "The API returned an invalid response."
                    };
                }
            }

            var content =
                await response.Content.ReadAsStringAsync();

            string error =
                $"API returned HTTP {(int)response.StatusCode} " +
                $"({response.ReasonPhrase}).";

            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    using var json = JsonDocument.Parse(content);

                    var root = json.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty(
                            "message", out var message) &&
                            message.ValueKind == JsonValueKind.String)
                        {
                            error = message.GetString() ?? error;
                        }
                        else if (root.TryGetProperty(
                            "errors", out var errors) &&
                            errors.ValueKind == JsonValueKind.Object)
                        {
                            var messages =
                                new System.Collections.Generic.List<string>();

                            foreach (var property in
                                errors.EnumerateObject())
                            {
                                if (property.Value.ValueKind ==
                                    JsonValueKind.Array)
                                {
                                    foreach (var item in
                                        property.Value.EnumerateArray())
                                    {
                                        if (item.ValueKind ==
                                            JsonValueKind.String)
                                        {
                                            var text = item.GetString();

                                            if (!string.IsNullOrWhiteSpace(text))
                                                messages.Add(text);
                                        }
                                    }
                                }
                            }

                            if (messages.Count > 0)
                                error = string.Join(" ", messages);
                        }
                        else if (root.TryGetProperty(
                            "title", out var title) &&
                            title.ValueKind == JsonValueKind.String)
                        {
                            error = title.GetString() ?? error;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Do not display raw server exception details.
                    // Keep the HTTP status message.
                }
            }

            return new ApiResult<T>
            {
                Success = false,
                Error = error
            };
        }

        // GET: Retrieve information from the API.
        public async Task<ApiResult<T>> GetAsync<T>(string url)
        {
            AddToken();

            using var response = await _http.GetAsync(url);

            return await HandleResponse<T>(response);
        }

        // POST: Send JSON data to the API.
        public async Task<ApiResult<TResponse>>
            PostAsync<TRequest, TResponse>(
                string url,
                TRequest request)
        {
            AddToken();

            using var response =
                await _http.PostAsJsonAsync(url, request);

            return await HandleResponse<TResponse>(response);
        }

        // POST: Send a request without a JSON body.
        // Used when participants enrol in a category.
        public async Task<ApiResult<T>>
            PostEmptyAsync<T>(string url)
        {
            AddToken();

            using var response =
                await _http.PostAsync(url, null);

            return await HandleResponse<T>(response);
        }

        // PUT: Update information through the API.
        public async Task<ApiResult<TResponse>>
            PutAsync<TRequest, TResponse>(
                string url,
                TRequest request)
        {
            AddToken();

            using var response =
                await _http.PutAsJsonAsync(url, request);

            return await HandleResponse<TResponse>(response);
        }

        public async Task<bool> DeleteAsync(string url)
        {
            AddToken();

            using var response =
                await _http.DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }

        // Upload profile pictures using multipart/form-data.
        public async Task<ApiResult<T>>
            UploadImageAsync<T>(
                string url,
                IFormFile file)
        {
            AddToken();

            using var content =
                new MultipartFormDataContent();

            using var stream =
                file.OpenReadStream();

            using var fileContent =
                new StreamContent(stream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(file.ContentType);

            content.Add(
                fileContent,
                "file",
                file.FileName);

            using var response =
                await _http.PostAsync(url, content);

            return await HandleResponse<T>(response);
        }
    }
}

