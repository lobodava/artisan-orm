using System.Collections.Immutable;
using System.Resources;
using Microsoft.CodeAnalysis;

[assembly: NeutralResourcesLanguage("en")]

namespace Artisan.Orm.Analyzers;

internal static class Diagnostics
{
    private static readonly ResourceManager Resources = new ResourceManager(
        "Artisan.Orm.Analyzers.AnalyzerResources", typeof(Diagnostics).Assembly);

    internal static LocalizableString Text(string key) =>
        new LocalizableResourceString(key, Resources, typeof(Diagnostics));

    private static DiagnosticDescriptor Rule(string id, string key) => new DiagnosticDescriptor(
        id, Text(key + "Title"), Text(key + "Message"), "Artisan.Mapping",
        DiagnosticSeverity.Error, isEnabledByDefault: true, description: Text(key + "Description"));

    internal static readonly DiagnosticDescriptor MissingMapper = Rule("ART001", "MissingMapper");
    internal static readonly DiagnosticDescriptor MissingMethod = Rule("ART002", "MissingMethod");
    internal static readonly DiagnosticDescriptor InvalidSignature = Rule("ART003", "InvalidSignature");
    internal static readonly DiagnosticDescriptor DuplicateMapper = Rule("ART004", "DuplicateMapper");
    internal static readonly DiagnosticDescriptor AmbiguousMethod = Rule("ART005", "AmbiguousMethod");
    internal static readonly DiagnosticDescriptor RequiredByCall = Rule("ART006", "RequiredByCall");
    internal static readonly DiagnosticDescriptor InvalidRegistration = Rule("ART007", "InvalidRegistration");

    internal static ImmutableArray<DiagnosticDescriptor> All { get; } = ImmutableArray.Create(
        MissingMapper, MissingMethod, InvalidSignature, DuplicateMapper,
        AmbiguousMethod, RequiredByCall, InvalidRegistration);
}
