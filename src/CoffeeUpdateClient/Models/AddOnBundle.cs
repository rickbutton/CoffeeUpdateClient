using System.IO;

namespace CoffeeUpdateClient.Models;

public class AddOnBundle
{
    public AddOnMetadata Metadata { get; }
    public Stream Data { get; }

    public AddOnBundle(AddOnMetadata metadata, Stream data)
    {
        Metadata = metadata;
        Data = data;
    }
}