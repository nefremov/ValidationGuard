namespace ValidationGuard;

// A builder for a nested scope: it holds only its own fragments, relative to its parent, and no reference to it; the
// parent passes its rendered prefix down when entries are produced.
internal sealed class NestedValidationBuilder<TContext>(ValidationOptions options, PathFragment[] fragments)
    : ValidationBuilderBase<TContext>(options, fragments);
