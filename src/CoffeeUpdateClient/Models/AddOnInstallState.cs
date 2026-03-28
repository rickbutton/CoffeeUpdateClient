namespace CoffeeUpdateClient.Models;

public class AddOnInstallState
{
    public AddOnMetadata? LocalAddOn { get; }
    public AddOnMetadata RemoteAddOn { get; }
    public bool HasLocalError { get; }

    public AddOnInstallState(AddOnMetadata? localAddOn, AddOnMetadata remoteAddOn, bool hasLocalError = false)
    {
        LocalAddOn = localAddOn;
        RemoteAddOn = remoteAddOn;
        HasLocalError = hasLocalError;

        if (LocalAddOn != null && LocalAddOn.Name != RemoteAddOn.Name)
        {
            throw new InvalidOperationException($"LocalAddOn name {LocalAddOn.Name} doesn't match expected RemoteAddon name {RemoteAddOn.Name}");
        }
    }

    public string Name => RemoteAddOn.Name;

    public bool IsInstalled => LocalAddOn != null;

    public bool IsUpdated => LocalAddOn?.Version == RemoteAddOn.Version;
}
