using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using CoffeeUpdater.Models;
using Serilog;

namespace CoffeeUpdater.Services;

public class HttpsAddOnDownloader : IAddOnDownloader
{
    private readonly HttpClient _client;
    private const string BaseUri = "coffee-auras.nyc3.digitaloceanspaces.com";

    private readonly object _manifestCacheLock = new();
    private string? _manifestETag;
    private AddOnManifest? _cachedManifest;

    public HttpsAddOnDownloader(HttpClient client)
    {
        _client = client;
    }

    public async Task<ManifestResult> GetLatestManifestAsync()
    {
        string? etag;
        AddOnManifest? cached;
        lock (_manifestCacheLock)
        {
            etag = _manifestETag;
            cached = _cachedManifest;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://{BaseUri}/manifest.json");
        if (etag != null)
        {
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
        }

        var response = await _client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotModified && cached != null)
        {
            Log.Debug("manifest not modified (304), using cached version");
            return ManifestResult.NotModified(cached);
        }

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var manifest = JsonSerializer.Deserialize<AddOnManifest>(content);
            if (manifest != null)
            {
                lock (_manifestCacheLock)
                {
                    _manifestETag = response.Headers.ETag?.Tag;
                    _cachedManifest = manifest;
                }
                return ManifestResult.Updated(manifest);
            }
        }

        Log.Error("Failed to fetch the latest manifest. Status code: {StatusCode} {Error}", response.StatusCode, response.ReasonPhrase);
        return ManifestResult.Failed();
    }

    public async Task<AddOnBundle?> GetAddOnBundleAsync(AddOnMetadata metadata)
    {
        var url = $"https://{BaseUri}/addons/{metadata.Name}-{metadata.Version}.zip";
        var response = await _client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStreamAsync();
            return new AddOnBundle(metadata, data);
        }

        Log.Error("Failed to download AddOn: {AddOn}. Status code: {StatusCode} {Error}", metadata, response.StatusCode, response.ReasonPhrase);
        return null;
    }
}
