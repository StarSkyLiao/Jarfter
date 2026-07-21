using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Jarfter.Net.Protocol.Generator;

/// <summary>
/// 为标记了 <c>NetRequestAttribute</c> 的 partial record 自动实现网络请求协议接口.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class NetRequestGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "Jarfter.Net.Protocol.Message.NetRequestAttribute";

    private static readonly DiagnosticDescriptor s_MustBeRecord = new DiagnosticDescriptor("JNP001", "网络请求协议必须是 record",
        "标记了 NetRequestAttribute 的类型“{0}”必须是 record", "Jarfter.Net.Protocol.Generator",
        DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor s_MustBePartial = new DiagnosticDescriptor("JNP002",
        "网络请求协议必须是 partial", "标记了 NetRequestAttribute 的 record“{0}”必须声明为 partial", "Jarfter.Net.Protocol.Generator",
        DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor s_ContainingTypeMustBePartial = new DiagnosticDescriptor("JNP003",
        "包含网络请求协议的类型必须是 partial", "包含网络请求协议“{0}”的类型“{1}”必须声明为 partial", "Jarfter.Net.Protocol.Generator",
        DiagnosticSeverity.Error, true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ProtocolTarget> targets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeMetadataName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (attributeContext, _) => new ProtocolTarget(
                    (INamedTypeSymbol)attributeContext.TargetSymbol,
                    attributeContext.Attributes[0]))
            .Where(static target => target is not null);

        context.RegisterSourceOutput(targets, static (sourceProductionContext, target) =>
        {
            if (!target.TypeSymbol.IsRecord)
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    s_MustBeRecord,
                    target.TypeSymbol.Locations.FirstOrDefault(),
                    target.TypeSymbol.Name));
                return;
            }

            if (!IsPartial(target.TypeSymbol))
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    s_MustBePartial,
                    target.TypeSymbol.Locations.FirstOrDefault(),
                    target.TypeSymbol.Name));
                return;
            }

            INamedTypeSymbol? containingType = target.TypeSymbol.ContainingType;
            while (containingType is not null)
            {
                if (!IsPartial(containingType))
                {
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        s_ContainingTypeMustBePartial,
                        target.TypeSymbol.Locations.FirstOrDefault(),
                        target.TypeSymbol.Name,
                        containingType.Name));
                    return;
                }

                containingType = containingType.ContainingType;
            }

            sourceProductionContext.AddSource(
                GetHintName(target.TypeSymbol),
                GenerateSource(target.TypeSymbol, GetResponseType(target.AttributeData)));
        });
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol) => typeSymbol.DeclaringSyntaxReferences
        .Select(static reference => reference.GetSyntax())
        .OfType<TypeDeclarationSyntax>()
        .All(static declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static ITypeSymbol? GetResponseType(AttributeData attributeData) =>
        attributeData.ConstructorArguments.Length == 0
            ? null
            : attributeData.ConstructorArguments[0].Value as ITypeSymbol;

    private static string GetHintName(INamedTypeSymbol typeSymbol) =>
        string.Concat(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Select(static character => char.IsLetterOrDigit(character) ? character : '_')) + ".NetRequest.g.cs";

    private static string GenerateSource(INamedTypeSymbol typeSymbol, ITypeSymbol? responseType)
    {
        StringBuilder builder = new StringBuilder();

        if (!typeSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append("namespace ")
                .Append(typeSymbol.ContainingNamespace.ToDisplayString())
                .AppendLine(";")
                .AppendLine();
        }

        ImmutableArray<INamedTypeSymbol> containingTypes = GetContainingTypes(typeSymbol);
        foreach (INamedTypeSymbol containingType in containingTypes)
        {
            AppendTypeDeclaration(builder, containingType, false);
            builder.AppendLine();
            builder.AppendLine("{");
        }

        AppendTypeDeclaration(builder, typeSymbol, true, responseType);
        builder.AppendLine();
        builder.AppendLine("{");
        AppendInterfaceImplementation(builder, typeSymbol, responseType);
        builder.AppendLine("}");

        for (int index = containingTypes.Length - 1; index >= 0; index--)
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    private static ImmutableArray<INamedTypeSymbol> GetContainingTypes(INamedTypeSymbol typeSymbol)
    {
        ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        INamedTypeSymbol? containingType = typeSymbol.ContainingType;
        while (containingType is not null)
        {
            builder.Add(containingType);
            containingType = containingType.ContainingType;
        }

        builder.Reverse();
        return builder.ToImmutable();
    }

    private static void AppendTypeDeclaration(
        StringBuilder builder,
        INamedTypeSymbol typeSymbol,
        bool isRecord,
        ITypeSymbol? responseType = null)
    {
        builder.Append(GetAccessibility(typeSymbol.DeclaredAccessibility));

        if (typeSymbol.IsStatic)
        {
            builder.Append("static ");
        }

        if (typeSymbol is { TypeKind: TypeKind.Struct, IsReadOnly: true })
        {
            builder.Append("readonly ");
        }

        builder.Append("partial ");
        builder.Append(isRecord
            ? typeSymbol.TypeKind == TypeKind.Struct ? "record struct " : "record "
            : GetTypeKeyword(typeSymbol));
        builder.Append(EscapeIdentifier(typeSymbol.Name));
        AppendTypeParameters(builder, typeSymbol);

        if (isRecord)
        {
            builder.Append(" : ")
                .Append(GetInterfaceType(typeSymbol, responseType));
        }

        AppendTypeParameterConstraints(builder, typeSymbol);
    }

    private static string GetAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public ",
        Accessibility.Internal => "internal ",
        Accessibility.Private => "private ",
        Accessibility.Protected => "protected ",
        Accessibility.ProtectedOrInternal => "protected internal ",
        Accessibility.ProtectedAndInternal => "private protected ",
        _ => string.Empty
    };

    private static string GetTypeKeyword(INamedTypeSymbol typeSymbol) => typeSymbol.TypeKind switch
    {
        TypeKind.Class => "class ",
        TypeKind.Struct => "struct ",
        TypeKind.Interface => "interface ",
        _ => throw new InvalidOperationException($"Unsupported containing type kind: {typeSymbol.TypeKind}.")
    };

    private static void AppendTypeParameters(StringBuilder builder, INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeParameters.Length == 0) return;

        builder.Append('<');
        builder.Append(string.Join(", ", typeSymbol.TypeParameters.Select(static parameter => EscapeIdentifier(parameter.Name))));
        builder.Append('>');
    }

    private static void AppendTypeParameterConstraints(StringBuilder builder, INamedTypeSymbol typeSymbol)
    {
        foreach (ITypeParameterSymbol typeParameter in typeSymbol.TypeParameters)
        {
            ImmutableArray<string>.Builder constraints = ImmutableArray.CreateBuilder<string>();
            if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints.Add(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
            }
            else if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            else if (typeParameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(typeParameter.ConstraintTypes.Select(static type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            if (typeParameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count == 0) continue;

            builder.Append(" where ")
                .Append(EscapeIdentifier(typeParameter.Name))
                .Append(" : ")
                .Append(string.Join(", ", constraints));
        }
    }

    private static void AppendInterfaceImplementation(StringBuilder builder, INamedTypeSymbol typeSymbol, ITypeSymbol? responseType)
    {
        string requestType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string interfaceType = GetInterfaceType(typeSymbol, responseType);

        builder.Append("    static string ")
            .Append(interfaceType)
            .Append(".MessageName => typeof(")
            .Append(requestType)
            .Append(").FullName ?? typeof(")
            .Append(requestType)
            .AppendLine(").Name;");
        builder.AppendLine();
        builder.Append("    static global::System.Text.Json.JsonElement ")
            .Append(interfaceType)
            .Append(".SerializeToElement(")
            .Append(interfaceType)
            .Append(" request) => global::System.Text.Json.JsonSerializer.SerializeToElement((")
            .Append(requestType)
            .AppendLine(")request);");
    }

    private static string GetInterfaceType(INamedTypeSymbol typeSymbol, ITypeSymbol? responseType)
    {
        string requestType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return responseType is null
            ? $"global::Jarfter.Net.Protocol.Message.INetRequest<{requestType}>"
            : $"global::Jarfter.Net.Protocol.Message.INetRequest<{requestType}, {responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
    }

    private static string EscapeIdentifier(string identifier) => SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
        ? "@" + identifier
        : identifier;

    private sealed class ProtocolTarget(INamedTypeSymbol typeSymbol, AttributeData attributeData)
    {
        public INamedTypeSymbol TypeSymbol { get; } = typeSymbol;

        public AttributeData AttributeData { get; } = attributeData;
    }
}
