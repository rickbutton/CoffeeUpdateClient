using System.Net;
using System.Net.Http;
using System.Text.Json;
using CoffeeUpdater.Models;
using CoffeeUpdater.Services;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace CoffeeUpdater.Tests;

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

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Updated));
        Assert.That(result.Manifest, Is.Not.Null);
        Assert.That(result.Manifest!.AddOns, Has.Count.EqualTo(1));
        Assert.That(result.Manifest.AddOns[0].Name, Is.EqualTo("TestAddOn"));
        Assert.That(result.Manifest.AddOns[0].Version, Is.EqualTo("1.0.0"));
    }

    [Test]
    public async Task GetLatestManifestAsync_ServerError_ReturnsFailedAsync()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUri}/manifest.json")
                .Respond(HttpStatusCode.InternalServerError);

        var downloader = new HttpsAddOnDownloader(mockHttp.ToHttpClient());

        var result = await downloader.GetLatestManifestAsync();

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Failed));
        Assert.That(result.Manifest, Is.Null);
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
    public async Task GetLatestManifestAsync_SecondCallWithETag_ReturnsNotModifiedAsync()
    {
        var manifest = new AddOnManifest
        {
            AddOns = [new AddOnMetadata { Name = "TestAddOn", Version = "1.0.0" }]
        };

        var handler = new ETagMockHandler(JsonSerializer.Serialize(manifest));
        var downloader = new HttpsAddOnDownloader(new HttpClient(handler));

        // First call — should return Updated
        var result1 = await downloader.GetLatestManifestAsync();
        Assert.That(result1.Status, Is.EqualTo(ManifestResult.ResultStatus.Updated));
        Assert.That(result1.Manifest, Is.Not.Null);

        // Second call — should return NotModified (ETag matches)
        var result2 = await downloader.GetLatestManifestAsync();
        Assert.That(result2.Status, Is.EqualTo(ManifestResult.ResultStatus.NotModified));
        Assert.That(result2.Manifest, Is.Not.Null);
        Assert.That(result2.Manifest!.AddOns[0].Name, Is.EqualTo("TestAddOn"));
    }

    private class ETagMockHandler : HttpMessageHandler
    {
        private readonly string _json;
        private const string ETagValue = "\"abc123\"";

        public ETagMockHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.IfNoneMatch.Any(e => e.Tag == ETagValue))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified));
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json)
            };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(ETagValue);
            return Task.FromResult(response);
        }
    }

    [Test]
    public async Task GetLatestManifestAsync_JsonNull_ReturnsFailedAsync()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When($"{BaseUri}/manifest.json")
                .Respond("application/json", "null");

        var downloader = new HttpsAddOnDownloader(mockHttp.ToHttpClient());

        var result = await downloader.GetLatestManifestAsync();

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Failed));
        Assert.That(result.Manifest, Is.Null);
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

        Assert.That(result.Status, Is.EqualTo(ManifestResult.ResultStatus.Updated));
        Assert.That(result.Manifest, Is.Not.Null);
        Assert.That(result.Manifest!.AddOns, Is.Empty);
    }
}
