namespace CoffeeUpdateClient.Models;

public class AddOnInstallState
{
    public AddOnMetadata? LocalAddOn { get; }
    public AddOnMetadata RemoteAddOn { get; }

    public AddOnInstallState(AddOnMetadata? localAddOn, AddOnMetadata remoteAddOn)
    {
        LocalAddOn = localAddOn;
        RemoteAddOn = remoteAddOn;

        if (LocalAddOn != null && LocalAddOn.Name != RemoteAddOn.Name)
        {
            throw new InvalidOperationException($"LocalAddOn name ${LocalAddOn.Name} doesn't match expected RemoteAddon name ${RemoteAddOn.Name}");
        }
    }

    public string Name => RemoteAddOn.Name;

    public bool IsInstalled
    {
        get
        {
            return LocalAddOn != null;
        }
    }

    public bool IsUpdated
    {
        get
        {
            return LocalAddOn?.Version == RemoteAddOn.Version;
        }
    }
}