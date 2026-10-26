namespace ValidationGuard.CommonErrors.Tests.Unit;

// Errors an extending library would define.
internal static class SampleErrors
{
    public static readonly PropertyErrorDefinition<int, int, int> NotBetween = new(
        "ACME0001", "Value {value} for {name} must be between {min} and {max}", "value", "min", "max");

    public static readonly PropertyErrorDefinition<string> InvalidFormat = new(
        "ACME0002", "Value for {name} does not match {pattern}", "pattern");

    public static readonly PropertyErrorDefinition Required = new("ACME0003", "Value for {name} is required");

    public static readonly EntityErrorDefinition SlugTaken = new("ACME0004", "{entity} with {name} = {value} is taken");

    public static readonly PropertyErrorDefinition<decimal, string> TooHeavy = new(
        "ACME0005", "Value {weight} {unit} for {name} is too heavy", "weight", "unit");
}

// Definitions with format mistakes, for the validator tests.
internal static class BrokenErrors
{
    public static readonly PropertyErrorDefinition<int> Typo = new("B1", "Value {valeu} for {name} is invalid", "value");

    public static readonly PropertyErrorDefinition<int> Unused = new("B2", "Value for {name} is invalid", "value");

    public static readonly PropertyErrorDefinition Fine = new("B3", "Value for {name} is required");
}
