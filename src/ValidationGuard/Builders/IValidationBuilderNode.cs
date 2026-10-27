namespace ValidationGuard;

// The non-generic view of a builder that materialization works with across generic child types.
internal interface IValidationBuilderNode
{
    // Counts this builder's and its descendants' entries, so the result array is sized exactly, and remembers the
    // count so Render can skip empty subtrees.
    int CountEntries();

    // Marks this builder and its descendants completed: no entry or nested builder can be added afterwards.
    void Complete();

    // Completes this builder's and its descendants' entries in place and writes them into sink from position, with
    // their localized formats at the same positions in formats (each entry keeps the format it was added with, which the
    // root orders by), returning the next position. The writer holds the fragments collected from the parent builders;
    // this builder appends its own while it and its children render.
    int Render(ValidationEntry[] sink, string[] formats, int position, ValidationMessageWriter writer);
}
