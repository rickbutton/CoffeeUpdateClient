using System.Net;
using System.Net.Http;
using System.Text.Json;
using CoffeeUpdateClient.Models;
using CoffeeUpdateClient.Services;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace CoffeeUpdateClient.Tests;

public class HttpsAddOnDownloaderTest
{
    private const string BaseUri = "https://coffee-auras.nyc3.digitaloceanspaces.com";

    [Test]
    public async Task GetLatestManifestAsync_Success_ReturnsManifestAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [
                new AddOnMetadata { Name = "TestAddOn", Version = "1.0.0" }
            ]
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUri}/manifest.json")
                .Respond("application/json", JsonSerializer.Serialize(manifest));

        var downloader = new HttpsAddOnDownloader(mockHttp.ToHttpClient());

        var result = await downloader.GetLatestManifestAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AddOns, Has.Count.EqualTo(1));
        Assert.That(result.AddOns[0].Name, Is.EqualTo("TestAddOn"));
        Assert.That(result.AddOns[0].Version, Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task GetLatestManifestAsync_ServerError_ReturnsNullAsync()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUri}/manifest.json")
                .Respond(HttpStatusCode.InternalServerError);

        var downloader = new HttpsAddOnDownloader(mockHttp.ToHttpClient());

        var result = await downloader.GetLatestManifestAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAddOnBundleAsync_Success_ReturnsBundleWithStreamAsync()
    {
        var metadata = new AddOnMetadata { Name = "MyAddOn", Version = "2.0.0" };
        var fakeZipBytes = new byte[] { 0x50, 0x4B, 0x05, 0x06 }; // minimal ZIP end-of-central-dir

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUri}/addons/{metadata.Name}-{metadata.Version}.zip")
                .Respond("application/zip", new MemoryStream(fakeZipBytes));

        var downloader = new HttpsAddOnDownloader(mockHttp.ToHttpClient());

        var result = await downloader.GetAddOnBundleAsync(metadata);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Metadata.Name, Is.EqualTo(metadata.Name));
        Assert.That(result.Metadata.Version, Is.EqualTo(metadata.Version));
        Assert.That(result.Data, Is.Not.Null);
    }

    [Test]
    public async Task GetAddOnBundleAsync_NotFound_ReturnsNullAsync()
    {
        var metadata = new AddOnMetadata { Name = "MissingAddOn", Version = "1.0.0" };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUri}/addons/{metadata.Name}-{metadata.Version}.zip")
                .Respond(HttpStatusCode.NotFound);

        var downloader = new HttpsAddOnDownloader(mockHttp.ToHttpClient());

        var result = await downloader.GetAddOnBundleAsync(metadata);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetLatestManifestAsync_EmptyAddOnList_ReturnsEmptyManifestAsync()
    {
        var manifest = new AddOnManifest { AddOns = [] };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUri}/manifest.json")
                .Respond("application/json", JsonSerializer.Serialize(manifest));

        var downloader = new HttpsAddOnDownloader(mockHttp.ToHttpClient());

        var result = await downloader.GetLatestManifestAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AddOns, Is.Empty);
    }
}
