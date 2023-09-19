using BnS_Multitool.Functions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BnS_Multitool.Models
{
    public class httpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<httpClient> _logger;

        public httpClient(ILogger<httpClient> logger)
        {
            _logger = logger;
            var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36");
            ServicePointManager.DefaultConnectionLimit = 50;
        }

        public bool RemoteFileExists(string url)
        {
            try
            {
                using HttpResponseMessage response = _httpClient.Send(new HttpRequestMessage(HttpMethod.Head, new Uri(url)));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get content response");
                return false;
            }
        }

        public async Task<long?> RemoteFileSize(string url)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, new Uri(url)));
                response.EnsureSuccessStatusCode();
                return response.Content.Headers.ContentLength;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get content size");
                return null;
            }
        }

        public bool DownloadFile(string url, string path, bool retry = true, string hash = "")
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (File.Exists(path) && Crypto.SHA1_File(path) == hash)
                return true;
            else
                File.Delete(path);

            int retries = 0;
            while (true)
            {
                try
                {
                    using HttpResponseMessage response = _httpClient.GetAsync(new Uri(url)).Result;
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(path, FileMode.Create))
                        response.Content.ReadAsStream().CopyTo(fs);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download file");
                    if (!retry || retries >= 4) return false;
                    retries++;
                }
            }
        }

        public async Task<bool> DownloadFileAsync(string url, string path, bool retry = true, string hash = "")
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (File.Exists(path) && Crypto.SHA1_File(path) == hash)
                return true;
            else
                File.Delete(path);

            int retries = 0;
            while (true)
            {
                try
                {
                    using HttpResponseMessage response = await _httpClient.GetAsync(new Uri(url));
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(path, FileMode.Create))
                    {
                        var content = await response.Content.ReadAsStreamAsync();
                        await content.CopyToAsync(fs);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download file");
                    if (!retry || retries >= 4) return false;
                    retries++;
                }
            }
        }


        public async Task<string> DownloadString(string url)
        {
            string content = string.Empty;
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(new Uri(url));
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download contents as a string");
            }
            return content;
        }

        public async Task<dynamic?> DownloadJson<T>(string url)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(new Uri(url));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();

            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download contents as JSON");
                return null;
            }
        }

        public async Task<string> UploadString(string url, StringContent content)
        {
            try
            {
                //var contentData = new StringContent(content, Encoding.UTF8, content);
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload content");
                return string.Empty;
            }
        }

        public async Task<string> UploadFile(string url, string file_path)
        {
            try
            {
                using (var multipartFormContent = new MultipartFormDataContent())
                {
                    var fileStreamContent = new StreamContent(File.OpenRead(file_path));
                    fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("binary/octet-stream");

                    multipartFormContent.Add(fileStreamContent, name: "file", fileName: Path.GetFileName(file_path));

                    var response = await _httpClient.PostAsync(url, multipartFormContent);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }

            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file");
                return string.Empty;
            }
        }
    }
}
