namespace CoffeeUpdater.Models;

public class ManifestResult
{
    public enum ResultStatus
    {
        Updated,
        NotModified,
        Failed,
    }

    public ResultStatus Status { get; }
    public AddOnManifest? Manifest { get; }

    private ManifestResult(ResultStatus status, AddOnManifest? manifest)
    {
        Status = status;
        Manifest = manifest;
    }

    public static ManifestResult Updated(AddOnManifest manifest) => new(ResultStatus.Updated, manifest);
    public static ManifestResult NotModified(AddOnManifest manifest) => new(ResultStatus.NotModified, manifest);
    public static ManifestResult Failed() => new(ResultStatus.Failed, null);
}
