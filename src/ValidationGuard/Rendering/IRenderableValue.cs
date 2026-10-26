namespace ValidationGuard;

/// <summary>
/// A placeholder value that writes itself into a detail through the <see cref="ValidationMessageWriter"/>, for
/// example a list writing each item with <see cref="ValidationMessageWriter.WriteValue{T}"/> so items are formatted and
/// quoted like any other value.
/// </summary>
public interface IRenderableValue
{
    void Render(ValidationMessageWriter writer);
}
