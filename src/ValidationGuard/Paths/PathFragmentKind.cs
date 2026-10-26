namespace ValidationGuard;

/// <summary>What a <see cref="PathFragment"/> addresses.</summary>
public enum PathFragmentKind : byte
{
    /// <summary>A property or field; its name is converted by the builder's naming convention.</summary>
    Member,

    /// <summary>A list or array index, rendered verbatim.</summary>
    Index,

    /// <summary>A dictionary key, rendered verbatim (escaped as JSON Pointer or quoted in a readable path).</summary>
    Key,
}
