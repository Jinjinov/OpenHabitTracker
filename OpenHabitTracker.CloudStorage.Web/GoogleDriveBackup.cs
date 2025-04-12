using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using OpenHabitTracker.App;

namespace OpenHabitTracker.CloudStorage.Web;

internal class GoogleDriveFile
{
    [JsonPropertyName("modifiedTime")]
    public DateTime ModifiedTime { get; set; }
}

internal class GoogleDriveBackup : GoogleDriveBase, IGoogleDriveBackup
{
    private readonly HttpClient _httpClient;

    private readonly IAccessTokenProvider _tokenProvider;

    public GoogleDriveBackup(ClientState clientState, HttpClient httpClient, IAccessTokenProvider tokenProvider) : base(clientState)
    {
        _httpClient = httpClient;

        _tokenProvider = tokenProvider;
    }

    protected override async Task<string> GetFolderId()
    {
        string folderId = string.Empty;

        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            string q = $"name = '{_folderName}' and mimeType = '{_folderMimeType}'";
            string url = "https://www.googleapis.com/drive/v3/files?q=" + Uri.EscapeDataString(q);

            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>();

                JsonElement.ArrayEnumerator files = data.GetProperty("files").EnumerateArray();

                while (files.MoveNext())
                {
                    JsonElement file = files.Current;

                    string? name = file.GetProperty("name").GetString();

                    if (name == _folderName)
                    {
                        folderId = file.GetProperty("id").GetString() ?? string.Empty;
                    }
                }
            }
        }

        return folderId;
    }

    protected override async Task<string> GetFileId(string folderId)
    {
        string fileId = string.Empty;

        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            string q = $"'{folderId}' in parents";
            string url = "https://www.googleapis.com/drive/v3/files?q=" + Uri.EscapeDataString(q);

            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>();

                JsonElement.ArrayEnumerator files = data.GetProperty("files").EnumerateArray();

                while (files.MoveNext())
                {
                    JsonElement file = files.Current;

                    string? fileName = file.GetProperty("name").GetString();

                    if (fileName == _fileName)
                    {
                        fileId = file.GetProperty("id").GetString() ?? string.Empty;
                    }
                }
            }
        }

        return fileId;
    }

    protected override async Task<string> GetFile(string fileId)
    {
        string file = string.Empty;

        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            string url = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";

            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                file = await response.Content.ReadAsStringAsync();
            }
        }

        return file;
    }

    protected override async Task<DateTime> GetFileModifiedTime(string fileId)
    {
        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            string url = $"https://www.googleapis.com/drive/v3/files/{fileId}?fields=modifiedTime";

            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                GoogleDriveFile? file = await response.Content.ReadFromJsonAsync<GoogleDriveFile>();

                if (file is not null)
                    return file.ModifiedTime.ToLocalTime();
            }
        }

        return DateTime.MinValue;
    }

    protected override async Task<string> CreateFolder()
    {
        string folderId = string.Empty;

        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart"),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            JsonContent metaContent = JsonContent.Create(new { name = _folderName, description = _folderDescription, mimeType = _folderMimeType });

            MultipartContent multipart = new() { metaContent };

            requestMessage.Content = multipart;

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>();

                folderId = data.GetProperty("id").GetString() ?? string.Empty;
            }
        }

        return folderId;
    }

    protected override async Task<string> CreateFile(string folderId, string content)
    {
        string fileId = string.Empty;

        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Post,
                //RequestUri = new Uri("https://www.googleapis.com/upload/drive/v3/files?uploadType=media"),
                RequestUri = new Uri("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart"),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            JsonContent metaContent;

            if (string.IsNullOrEmpty(folderId))
                metaContent = JsonContent.Create(new { name = _fileName, description = _fileDescription });
            else
                metaContent = JsonContent.Create(new { name = _fileName, description = _fileDescription, parents = new[] { folderId } });

            //string content = JsonSerializer.Serialize(new { Title = "Blazor POST Request Example" });
            //StringContent fileContent = new StringContent(content);
            //fileContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Plain);

            StringContent fileContent = new(content, Encoding.UTF8, _fileMimeType);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(_fileMimeType);
            fileContent.Headers.ContentLength = content.Length;

            MultipartContent multipart = new() { metaContent, fileContent };

            requestMessage.Content = multipart;

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>();

                fileId = data.GetProperty("id").GetString() ?? string.Empty;
            }
        }

        return fileId;
    }

    protected override async Task<string> UpdateFile(string fileId, string content)
    {
        string updatedFileId = string.Empty;

        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Patch,
                //RequestUri = new Uri("https://www.googleapis.com/upload/drive/v3/files?uploadType=media"),
                RequestUri = new Uri("https://www.googleapis.com/upload/drive/v3/files/" + fileId + "?uploadType=multipart"),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            JsonContent metaContent = JsonContent.Create(new { name = _fileName, description = _fileDescription });

            //string content = JsonSerializer.Serialize(new { Title = "Blazor POST Request Example", DateTime = DateTime.Now });
            //StringContent fileContent = new StringContent(content);
            //fileContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Plain);

            StringContent fileContent = new(content, Encoding.UTF8, _fileMimeType);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(_fileMimeType);
            fileContent.Headers.ContentLength = content.Length;

            MultipartContent multipart = new() { metaContent, fileContent };

            requestMessage.Content = multipart;

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>();

                updatedFileId = data.GetProperty("id").GetString() ?? string.Empty;
            }
        }

        return updatedFileId;
    }

    public async Task<string> GetFolders()
    {
        string folders = string.Empty;

        AccessTokenResult tokenResult = await _tokenProvider.RequestAccessToken();

        if (tokenResult.TryGetToken(out AccessToken? token))
        {
            string q = $"mimeType = '{_folderMimeType}'";
            string url = "https://www.googleapis.com/drive/v3/files?q=" + Uri.EscapeDataString(q);

            HttpRequestMessage requestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

            HttpStatusCode responseStatusCode = response.StatusCode;

            if (responseStatusCode == HttpStatusCode.OK)
            {
                JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>();

                JsonElement.ArrayEnumerator files = data.GetProperty("files").EnumerateArray();

                folders = string.Empty;

                while (files.MoveNext())
                {
                    JsonElement file = files.Current;

                    string? name = file.GetProperty("name").GetString();

                    folders += name + Environment.NewLine;
                }
            }
        }

        return folders;
    }
}
