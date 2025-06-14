using System.IO;
using System.Net.Http;
using System.Text.Json;
using CoffeeUpdateClient.Models;
using Serilog;

namespace CoffeeUpdateClient.Services;

public class HttpsAddOnDownloader : IAddOnDownloader
{
    private readonly string BaseUri = "coffee-auras.nyc3.digitaloceanspaces.com";

    public HttpsAddOnDownloader() { }

    public async Task<AddOnManifest?> GetLatestManifestAsync()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"https://{BaseUri}/manifest.json");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AddOnManifest>(content);
        }
        else
        {
            Log.Error("Failed to fetch the latest manifest. Status code: {StatusCode} {Error}", response.StatusCode, response.ReasonPhrase);
        }
        return null;
    }

    public async Task<AddOnBundle?> GetAddOnBundleAsync(AddOnMetadata metadata)
    {
        var url = $"https://{BaseUri}/addons/{metadata.Name}-{metadata.Version}.zip";
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStreamAsync();
            return new AddOnBundle(metadata, data);
        }
        else
        {
            Log.Error("Failed to download AddOn: {AddOn}. Status code: {StatusCode} {Error}", metadata, response.StatusCode, response.ReasonPhrase);
            return null;
        }
    }
}