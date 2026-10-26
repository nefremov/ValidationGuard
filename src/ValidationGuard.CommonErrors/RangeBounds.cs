namespace ValidationGuard.CommonErrors;

/// <summary>Which ends of a range <c>min..max</c> belong to it, for <see cref="CommonErrorExtensions.AddOutOfRange{TContext, TValue}"/>.</summary>
public enum RangeBounds
{
    /// <summary><c>[min, max]</c>: <c>min &lt;= x &lt;= max</c>.</summary>
    Inclusive,

    /// <summary><c>(min, max)</c>: <c>min &lt; x &lt; max</c>.</summary>
    Exclusive,

    /// <summary><c>[min, max)</c>: <c>min &lt;= x &lt; max</c>.</summary>
    MinInclusive,

    /// <summary><c>(min, max]</c>: <c>min &lt; x &lt;= max</c>.</summary>
    MaxInclusive,
}
