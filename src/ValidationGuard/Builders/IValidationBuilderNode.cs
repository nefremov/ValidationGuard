namespace ValidationGuard;

// The non-generic view of a builder that materialization works with across generic child types.
internal interface IValidationBuilderNode
{
    // Counts this builder's and its descendants' entries, so the result array is sized exactly, and remembers the
    // count so Render can skip empty subtrees.
    int CountEntries();

    // Completes this builder's and its descendants' entries in place and writes them into sink from position,
    // returning the next position. The writer holds the fragments collected from the parent builders; this builder
    // appends its own while it and its children render.
    int Render(ValidationEntry[] sink, int position, ValidationMessageWriter writer);
}
