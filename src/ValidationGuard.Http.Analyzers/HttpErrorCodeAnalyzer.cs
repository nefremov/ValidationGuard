using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ValidationGuard.Http.Analyzers;

/// <summary>
/// Checks HTTP error code declarations at compile time: <c>[assembly: HttpErrorCode]</c> and
/// <c>[assembly: HttpErrorCodeOverride]</c> attributes and <c>ErrorCodeMapBuilder</c> calls with constant arguments,
/// for blank codes, statuses outside 400-599, problem types that are not absolute URIs, codes declared twice (in the
/// assembly or by two referenced assemblies), and overrides of undeclared codes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HttpErrorCodeAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeMetadataName = "ValidationGuard.Http.HttpErrorCodeAttribute";
    private const string OverrideAttributeMetadataName = "ValidationGuard.Http.HttpErrorCodeOverrideAttribute";
    private const string BuilderMetadataName = "ValidationGuard.Http.ErrorCodeMapBuilder";
    private const int MinStatus = 400;
    private const int MaxStatus = 599;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.EmptyCode,
        DiagnosticDescriptors.StatusOutOfRange,
        DiagnosticDescriptors.InvalidType,
        DiagnosticDescriptors.DuplicateCode,
        DiagnosticDescriptors.DuplicateReferencedCode,
        DiagnosticDescriptors.UnknownOverride,
        DiagnosticDescriptors.DuplicateOverride);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var attributeType = context.Compilation.GetTypeByMetadataName(AttributeMetadataName);
        if (attributeType is null)
        {
            return;
        }

        var overrideType = context.Compilation.GetTypeByMetadataName(OverrideAttributeMetadataName);
        var builderType = context.Compilation.GetTypeByMetadataName(BuilderMetadataName);

        context.RegisterSyntaxNodeAction(c => AnalyzeAttribute(c, attributeType, overrideType), SyntaxKind.Attribute);
        context.RegisterCompilationEndAction(c => AnalyzeDeclarations(c, attributeType, overrideType));

        if (builderType is not null)
        {
            context.RegisterOperationAction(c => AnalyzeInvocation(c, builderType), OperationKind.Invocation);
        }
    }

    private static void AnalyzeAttribute(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol attributeType,
        INamedTypeSymbol? overrideType)
    {
        var attribute = (AttributeSyntax)context.Node;
        if (attribute.ArgumentList is null)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor)
        {
            return;
        }

        var type = constructor.ContainingType;
        if (!SymbolEqualityComparer.Default.Equals(type, attributeType)
            && !SymbolEqualityComparer.Default.Equals(type, overrideType))
        {
            return;
        }

        var position = 0;
        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            string? name;
            if (argument.NameEquals is not null)
            {
                name = argument.NameEquals.Name.Identifier.ValueText;
            }
            else if (argument.NameColon is not null)
            {
                name = argument.NameColon.Name.Identifier.ValueText;
            }
            else
            {
                name = position switch
                {
                    0 => "code",
                    1 => "status",
                    _ => null,
                };
                position++;
            }

            var constant = context.SemanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
            if (constant.HasValue)
            {
                CheckValue(context.ReportDiagnostic, name, constant.Value, argument.Expression.GetLocation());
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol builderType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (method.Name is not ("Add" or "Override")
            || !SymbolEqualityComparer.Default.Equals(method.ContainingType, builderType))
        {
            return;
        }

        foreach (var argument in invocation.Arguments)
        {
            if (argument.ArgumentKind == ArgumentKind.DefaultValue)
            {
                continue;
            }

            var constant = argument.Value.ConstantValue;
            if (constant.HasValue)
            {
                CheckValue(context.ReportDiagnostic, argument.Parameter?.Name, constant.Value, argument.Value.Syntax.GetLocation());
            }
        }
    }

    private static void AnalyzeDeclarations(
        CompilationAnalysisContext context,
        INamedTypeSymbol attributeType,
        INamedTypeSymbol? overrideType)
    {
        var compilation = context.Compilation;
        var attributeAssemblyName = attributeType.ContainingAssembly.Identity.Name;
        var referenced = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (!ReferencesAssembly(assembly, attributeAssemblyName))
            {
                continue;
            }

            foreach (var attribute in assembly.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)
                    || !TryGetCode(attribute, out var code))
                {
                    continue;
                }

                if (!referenced.TryGetValue(code, out var owner))
                {
                    referenced.Add(code, assembly.Name);
                }
                else if (!string.Equals(owner, assembly.Name, StringComparison.Ordinal))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateReferencedCode, Location.None, code, owner, assembly.Name));
                }
            }
        }

        var declared = new HashSet<string>(StringComparer.Ordinal);
        var overridden = new HashSet<string>(StringComparer.Ordinal);
        var overrides = new List<KeyValuePair<string, AttributeData>>();

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;

            if (SymbolEqualityComparer.Default.Equals(attributeClass, attributeType))
            {
                if (!TryGetCode(attribute, out var code))
                {
                    continue;
                }

                if (!declared.Add(code))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateCode, GetLocation(attribute, context.CancellationToken), code));
                }
                else if (referenced.TryGetValue(code, out var owner))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateReferencedCode,
                        GetLocation(attribute, context.CancellationToken),
                        code,
                        owner,
                        compilation.AssemblyName));
                }
            }
            else if (overrideType is not null && SymbolEqualityComparer.Default.Equals(attributeClass, overrideType))
            {
                if (!TryGetCode(attribute, out var code))
                {
                    continue;
                }

                if (!overridden.Add(code))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateOverride, GetLocation(attribute, context.CancellationToken), code));
                }
                else
                {
                    overrides.Add(new KeyValuePair<string, AttributeData>(code, attribute));
                }
            }
        }

        foreach (var pair in overrides)
        {
            if (!declared.Contains(pair.Key) && !referenced.ContainsKey(pair.Key))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnknownOverride, GetLocation(pair.Value, context.CancellationToken), pair.Key));
            }
        }
    }

    private static void CheckValue(Action<Diagnostic> report, string? name, object? value, Location location)
    {
        switch (name)
        {
            case "code":
                if (value is not string code || string.IsNullOrWhiteSpace(code))
                {
                    report(Diagnostic.Create(DiagnosticDescriptors.EmptyCode, location));
                }

                break;

            case "status":
                if (value is int status && status is < MinStatus or > MaxStatus)
                {
                    report(Diagnostic.Create(DiagnosticDescriptors.StatusOutOfRange, location, status));
                }

                break;

            case "type":
            case "Type":
                if (value is string type && !IsAbsoluteTypeUri(type))
                {
                    report(Diagnostic.Create(DiagnosticDescriptors.InvalidType, location, type));
                }

                break;
        }
    }

    private static bool IsAbsoluteTypeUri(string type) =>
        Uri.TryCreate(type, UriKind.Absolute, out var uri) && !uri.IsFile;

    private static bool ReferencesAssembly(IAssemblySymbol assembly, string assemblyName)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var reference in module.ReferencedAssemblySymbols)
            {
                if (string.Equals(reference.Identity.Name, assemblyName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetCode(AttributeData attribute, out string code)
    {
        if (attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is string value
            && !string.IsNullOrWhiteSpace(value))
        {
            code = value;
            return true;
        }

        code = string.Empty;
        return false;
    }

    private static Location GetLocation(AttributeData attribute, CancellationToken cancellationToken) =>
        attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None;
}
