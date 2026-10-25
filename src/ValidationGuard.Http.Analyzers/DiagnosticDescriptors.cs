using Microsoft.CodeAnalysis;

namespace ValidationGuard.Http.Analyzers;

internal static class DiagnosticDescriptors
{
    private const string Category = "ValidationGuard.Http";

    public static readonly DiagnosticDescriptor EmptyCode = new(
        id: "VGH001",
        title: "HTTP error code must not be empty",
        messageFormat: "The HTTP error code must be a non-empty string",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Error codes are the keys of the code-to-status map and must identify a validation failure.");

    public static readonly DiagnosticDescriptor StatusOutOfRange = new(
        id: "VGH002",
        title: "HTTP error status must be 4xx or 5xx",
        messageFormat: "Status {0} is not a client or server error status; use a value from 400 to 599",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A validation problem response must carry a client or server error status.");

    public static readonly DiagnosticDescriptor InvalidType = new(
        id: "VGH003",
        title: "HTTP error type must be an absolute URI",
        messageFormat: "Type '{0}' is not an absolute URI; Problem Details 'type' must be absolute, for example 'https://example.com/errors/code'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "RFC 9457 identifies a problem type by URI; relative references and file paths resolve differently per client.");

    public static readonly DiagnosticDescriptor DuplicateCode = new(
        id: "VGH004",
        title: "Duplicate HTTP error code",
        messageFormat: "HTTP error code '{0}' is declared more than once in this assembly",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Registering an assembly that declares a code twice throws at startup.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor DuplicateReferencedCode = new(
        id: "VGH005",
        title: "HTTP error code declared by multiple assemblies",
        messageFormat: "HTTP error code '{0}' is declared by both '{1}' and '{2}'; registering both assemblies throws at startup",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Codes share one process-wide namespace, so two assemblies declaring the same code cannot both be registered.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor UnknownOverride = new(
        id: "VGH006",
        title: "Override targets an undeclared HTTP error code",
        messageFormat: "HTTP error code '{0}' is overridden but not declared in this assembly or any referenced assembly",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An override replaces an existing mapping; building the map throws when the overridden code is not registered.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor DuplicateOverride = new(
        id: "VGH007",
        title: "Duplicate HTTP error code override",
        messageFormat: "HTTP error code '{0}' is overridden more than once",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Registering an assembly that overrides a code twice throws at startup.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);
}
