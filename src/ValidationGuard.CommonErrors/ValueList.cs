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

    public ValueList(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = [.. values];
    }

    public int Count => _values.Length;

    public T this[int index] => _values[index];

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

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
