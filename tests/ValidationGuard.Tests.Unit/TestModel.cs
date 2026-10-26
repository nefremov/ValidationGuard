namespace ValidationGuard.Tests.Unit;

// The document validated by the builder and metadata tests.
public sealed class Order
{
    public Customer Customer { get; init; } = new();

    public Address HomeAddress { get; init; } = new();

    public List<Line> Lines { get; init; } = [];

    public Dictionary<string, string> Tags { get; init; } = new();

    public Node Tree { get; init; } = new();
}

public sealed class Customer
{
    public string? FirstName { get; init; }
}

public sealed class Address
{
    public string? PostalCode { get; init; }
}

public sealed class Line
{
    public string? Sku { get; init; }

    public LineDetail Detail { get; init; } = new();
}

public sealed class LineDetail
{
    public string? Note { get; init; }
}

// A recursive shape, for paths of any depth.
public sealed class Node
{
    public Node? Next { get; init; }

    public string? Value { get; init; }
}

// Property metadata that also writes {other} as another property's path, and counts {name} writes.
internal sealed class ProbeMetadata(PathFragment[] property, PathFragment[]? other = null) : PropertyValidationMetadata(property)
{
    public int NameWrites { get; private set; }

    protected override bool TryWrite(ReadOnlySpan<char> key, ValidationMessageWriter writer)
    {
        if (key is "other")
        {
            writer.WriteText(writer.GetClrPath(other));
            return true;
        }

        NameWrites += key is "name" ? 1 : 0;
        return base.TryWrite(key, writer);
    }
}
