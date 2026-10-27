using System.Collections;

namespace ValidationGuard.CommonErrors;

/// <summary>
/// A list of values used as one placeholder value, such as the allowed values of a property. In a detail it writes
/// each value like any other placeholder value (current culture, the placeholder's format specifier, strings quoted
/// by default), separated by <c>", "</c>; in JSON it is an array of the values.
/// </summary>
public sealed class ValueList<T> : IReadOnlyList<T>, IRenderableValue
{
    private readonly T[] _values;

    /// <summary>Creates a list of a copy of <paramref name="values"/>.</summary>
    public ValueList(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = [.. values];
    }

    /// <inheritdoc />
    public int Count => _values.Length;

    /// <inheritdoc />
    public T this[int index] => _values[index];

    /// <summary>Writes the values separated by <c>", "</c>, each as a placeholder value.</summary>
    public void Render(ValidationMessageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        for (var i = 0; i < _values.Length; i++)
        {
            if (i > 0)
            {
                writer.WriteText(", ");
            }

            writer.WriteValue<T>(_values[i]);
        }
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
