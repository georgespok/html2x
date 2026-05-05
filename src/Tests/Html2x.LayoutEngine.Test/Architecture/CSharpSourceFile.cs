using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;


internal sealed class CSharpSourceFile
{
    private CSharpSourceFile(string path, SyntaxTree tree, CompilationUnitSyntax root)
    {
        Path = path;
        Tree = tree;
        Root = root;
    }

    public string Path { get; }

    public SyntaxTree Tree { get; }

    public CompilationUnitSyntax Root { get; }

    public static CSharpSourceFile Load(params string[] pathSegments) =>
        Load(ArchitecturePaths.PathFromRoot(pathSegments));

    public static CSharpSourceFile Load(string path)
    {
        var source = File.ReadAllText(path);
        var tree = CSharpSyntaxTree.ParseText(source, path: path);

        return new CSharpSourceFile(path, tree, tree.GetCompilationUnitRoot());
    }

    public void ShouldDeclareNamespace(string expectedNamespace)
    {
        Namespaces().ShouldContain(
            expectedNamespace,
            $"{RelativePath()} should declare namespace {expectedNamespace}.");
    }

    public void ShouldNotDeclareNamespace(string forbiddenNamespace)
    {
        Namespaces().ShouldNotContain(
            forbiddenNamespace,
            $"{RelativePath()} should not declare namespace {forbiddenNamespace}.");
    }

    public void ShouldUseNamespace(string expectedNamespace)
    {
        UsesNamespace(expectedNamespace).ShouldBeTrue(
            $"{RelativePath()} should use namespace {expectedNamespace}.");
    }

    public void ShouldNotUseNamespace(string forbiddenNamespace)
    {
        UsesNamespace(forbiddenNamespace).ShouldBeFalse(
            $"{RelativePath()} should not use namespace {forbiddenNamespace}.");
    }

    public void ShouldNotUseNamespaces(params string[] forbiddenNamespaces)
    {
        foreach (var forbiddenNamespace in forbiddenNamespaces)
        {
            ShouldNotUseNamespace(forbiddenNamespace);
        }
    }

    public void ShouldContainType(string typeName, string accessibility, bool? isSealed = null)
    {
        var declaration = TypeDeclarations()
            .FirstOrDefault(type => type.Identifier.ValueText.Equals(typeName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare type {typeName}.");
        AccessibilityOf(declaration).ShouldBe(accessibility, $"{typeName} should be {accessibility}.");
        if (isSealed is not null)
        {
            declaration.Modifiers.Any(SyntaxKind.SealedKeyword).ShouldBe(
                isSealed.Value,
                $"{typeName} sealed modifier mismatch.");
        }
    }

    public void ShouldNotContainPublicType(string typeName)
    {
        var declaration = TypeDeclarations()
            .FirstOrDefault(type => type.Identifier.ValueText.Equals(typeName, StringComparison.Ordinal));

        if (declaration is null)
        {
            return;
        }

        AccessibilityOf(declaration).ShouldNotBe("public", $"{typeName} should not be public.");
    }

    public void ShouldContainRecordStruct(string typeName, string accessibility)
    {
        var declaration = Root.DescendantNodes()
            .OfType<RecordDeclarationSyntax>()
            .FirstOrDefault(type => type.Identifier.ValueText.Equals(typeName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare record {typeName}.");
        declaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword).ShouldBeTrue($"{typeName} should be a record struct.");
        AccessibilityOf(declaration).ShouldBe(accessibility, $"{typeName} should be {accessibility}.");
    }

    public void ShouldContainEnum(string typeName, string accessibility)
    {
        var declaration = Root.DescendantNodes()
            .OfType<EnumDeclarationSyntax>()
            .FirstOrDefault(type => type.Identifier.ValueText.Equals(typeName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare enum {typeName}.");
        AccessibilityOf(declaration).ShouldBe(accessibility, $"{typeName} should be {accessibility}.");
    }

    public void ShouldContainEnumMembers(string enumName, params string[] memberNames)
    {
        var declaration = Root.DescendantNodes()
            .OfType<EnumDeclarationSyntax>()
            .FirstOrDefault(type => type.Identifier.ValueText.Equals(enumName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare enum {enumName}.");
        var actualMembers = declaration.Members
            .Select(static member => member.Identifier.ValueText)
            .ToArray();

        foreach (var memberName in memberNames)
        {
            actualMembers.ShouldContain(memberName, $"{enumName} should contain enum member {memberName}.");
        }
    }

    public void ShouldContainProperty(string propertyName, string? propertyType = null, string? accessibility = null)
    {
        var declaration = Root.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(property => property.Identifier.ValueText.Equals(propertyName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare property {propertyName}.");
        AssertPropertyShape(declaration, propertyName, propertyType, accessibility);
    }

    public void ShouldContainPropertyInType(
        string typeName,
        string propertyName,
        string? propertyType = null,
        string? accessibility = null)
    {
        var type = FindType(typeName);
        var declaration = type.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(property => property.Identifier.ValueText.Equals(propertyName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare property {typeName}.{propertyName}.");
        AssertPropertyShape(declaration, $"{typeName}.{propertyName}", propertyType, accessibility);
    }

    private void AssertPropertyShape(
        PropertyDeclarationSyntax declaration,
        string propertyDisplayName,
        string? propertyType,
        string? accessibility)
    {
        if (propertyType is not null)
        {
            NormalizeType(declaration.Type.ToString()).ShouldBe(
                NormalizeType(propertyType),
                $"{propertyDisplayName} should have type {propertyType}.");
        }

        if (accessibility is not null)
        {
            AccessibilityOf(declaration).ShouldBe(accessibility, $"{propertyDisplayName} should be {accessibility}.");
        }
    }

    public void ShouldNotContainProperty(string propertyName, string? propertyType = null, string? accessibility = null)
    {
        var matches = Root.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => property.Identifier.ValueText.Equals(propertyName, StringComparison.Ordinal))
            .Where(property => propertyType is null || NormalizeType(property.Type.ToString()) == NormalizeType(propertyType))
            .Where(property => accessibility is null || AccessibilityOf(property) == accessibility)
            .ToArray();

        matches.ShouldBeEmpty($"{RelativePath()} should not declare matching property {propertyName}.");
    }

    public void ShouldNotContainPropertyInType(string typeName, string propertyName, string? propertyType = null, string? accessibility = null)
    {
        var type = FindType(typeName);
        var matches = type.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => property.Identifier.ValueText.Equals(propertyName, StringComparison.Ordinal))
            .Where(property => propertyType is null || NormalizeType(property.Type.ToString()) == NormalizeType(propertyType))
            .Where(property => accessibility is null || AccessibilityOf(property) == accessibility)
            .ToArray();

        matches.ShouldBeEmpty($"{typeName} should not declare matching property {propertyName}.");
    }

    public void ShouldContainMethod(string methodName, string? returnType = null, string? accessibility = null)
    {
        var declaration = Root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(method => method.Identifier.ValueText.Equals(methodName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare method {methodName}.");
        AssertMethodShape(declaration, methodName, returnType, accessibility);
    }

    public void ShouldContainMethodInType(
        string typeName,
        string methodName,
        string? returnType = null,
        string? accessibility = null)
    {
        var type = FindType(typeName);
        var declaration = type.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(method => method.Identifier.ValueText.Equals(methodName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare method {typeName}.{methodName}.");
        AssertMethodShape(declaration, $"{typeName}.{methodName}", returnType, accessibility);
    }

    private void AssertMethodShape(
        MethodDeclarationSyntax declaration,
        string methodDisplayName,
        string? returnType,
        string? accessibility)
    {
        if (returnType is not null)
        {
            NormalizeType(declaration.ReturnType.ToString()).ShouldBe(
                NormalizeType(returnType),
                $"{methodDisplayName} should return {returnType}.");
        }

        if (accessibility is not null)
        {
            AccessibilityOf(declaration).ShouldBe(accessibility, $"{methodDisplayName} should be {accessibility}.");
        }
    }

    public void ShouldContainConstructor(string typeName, string accessibility)
    {
        var declaration = Root.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(constructor => constructor.Identifier.ValueText.Equals(typeName, StringComparison.Ordinal));

        declaration.ShouldNotBeNull($"{RelativePath()} should declare constructor {typeName}.");
        AccessibilityOf(declaration).ShouldBe(accessibility, $"{typeName} constructor should be {accessibility}.");
    }

    public void ShouldHaveParameter(string methodOrConstructorName, string parameterName, string parameterType)
    {
        var parameter = Root.DescendantNodes()
            .OfType<BaseMethodDeclarationSyntax>()
            .Where(method => MethodName(method).Equals(methodOrConstructorName, StringComparison.Ordinal))
            .SelectMany(method => method.ParameterList.Parameters)
            .FirstOrDefault(parameter => parameter.Identifier.ValueText.Equals(parameterName, StringComparison.Ordinal));

        parameter.ShouldNotBeNull($"{RelativePath()} should declare parameter {parameterName}.");
        NormalizeType(parameter.Type?.ToString() ?? string.Empty).ShouldBe(
            NormalizeType(parameterType),
            $"{parameterName} should have type {parameterType}.");
    }

    public void ShouldNotHavePublicConstructorParameter(string typeName, string parameterType)
    {
        var matches = Root.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>()
            .Where(constructor => constructor.Identifier.ValueText.Equals(typeName, StringComparison.Ordinal))
            .Where(constructor => AccessibilityOf(constructor) == "public")
            .SelectMany(constructor => constructor.ParameterList.Parameters)
            .Where(parameter => NormalizeType(parameter.Type?.ToString() ?? string.Empty) == NormalizeType(parameterType))
            .ToArray();

        matches.ShouldBeEmpty($"{typeName} should not expose public constructor parameter type {parameterType}.");
    }

    public void ShouldUseIdentifier(string identifier)
    {
        EnsureSimpleIdentifier(identifier);
        UsesIdentifier(identifier).ShouldBeTrue($"{RelativePath()} should use identifier {identifier}.");
    }

    public void ShouldNotUseIdentifier(string identifier)
    {
        EnsureSimpleIdentifier(identifier);
        UsesIdentifier(identifier).ShouldBeFalse($"{RelativePath()} should not use identifier {identifier}.");
    }

    public void ShouldNotUseIdentifiers(params string[] identifiers)
    {
        foreach (var identifier in identifiers)
        {
            ShouldNotUseIdentifier(identifier);
        }
    }

    public void ShouldNotUseObjectType()
    {
        Root.DescendantNodes()
            .OfType<PredefinedTypeSyntax>()
            .Any(type => type.Keyword.IsKind(SyntaxKind.ObjectKeyword))
            .ShouldBeFalse($"{RelativePath()} should not use arbitrary object values.");
    }

    public void ShouldConstructType(string typeName)
    {
        ConstructsType(typeName).ShouldBeTrue($"{RelativePath()} should construct {typeName}.");
    }

    public void ShouldNotConstructType(string typeName)
    {
        ConstructsType(typeName).ShouldBeFalse($"{RelativePath()} should not construct {typeName}.");
    }

    public void ShouldInvoke(string memberName)
    {
        Invokes(memberName).ShouldBeTrue($"{RelativePath()} should invoke {memberName}.");
    }

    public void ShouldNotInvoke(string memberName)
    {
        Invokes(memberName).ShouldBeFalse($"{RelativePath()} should not invoke {memberName}.");
    }

    public void ShouldNotInvokeMemberOn(string receiverName, string memberName)
    {
        var invocation = FindInvocationMemberOn(receiverName, memberName);
        invocation.ShouldBeNull(
            invocation is null
                ? null
                : $"{RelativePath()}:{LineNumber(invocation)} should not invoke {receiverName}.{memberName}.");
    }

    public void ShouldInvokeMemberOn(string receiverName, string memberName)
    {
        var invocation = FindInvocationMemberOn(receiverName, memberName);
        invocation.ShouldNotBeNull($"{RelativePath()} should invoke {receiverName}.{memberName}.");
    }

    public void ShouldAssignToMember(string memberName)
    {
        AssignsToMember(memberName).ShouldBeTrue($"{RelativePath()} should assign to {memberName}.");
    }

    public void ShouldNotAssignToMember(string memberName)
    {
        AssignsToMember(memberName).ShouldBeFalse($"{RelativePath()} should not assign to {memberName}.");
    }

    public void ShouldContainStringLiteral(string value)
    {
        StringLiterals().ShouldContain(value, $"{RelativePath()} should contain string literal {value}.");
    }

    public void ShouldNotContainStringLiteral(string value)
    {
        StringLiterals().ShouldNotContain(value, $"{RelativePath()} should not contain string literal {value}.");
    }

    public void ShouldContainFriendAssemblies(params string[] expectedAssemblies) =>
        FriendAssemblies().ShouldBeSet(expectedAssemblies);

    public IReadOnlyList<string> FriendAssemblies()
    {
        return Root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(static attribute => attribute.Name.ToString().EndsWith("InternalsVisibleTo", StringComparison.Ordinal))
            .Select(attribute => attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression)
            .OfType<LiteralExpressionSyntax>()
            .Select(static expression => expression.Token.ValueText)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private IEnumerable<string> Namespaces() =>
        Root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(static declaration => declaration.Name.ToString());

    private IEnumerable<BaseTypeDeclarationSyntax> TypeDeclarations() =>
        Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>();

    private BaseTypeDeclarationSyntax FindType(string typeName)
    {
        var type = TypeDeclarations()
            .FirstOrDefault(type => type.Identifier.ValueText.Equals(typeName, StringComparison.Ordinal));

        type.ShouldNotBeNull($"{RelativePath()} should declare type {typeName}.");
        return type;
    }

    private bool UsesNamespace(string namespaceName)
    {
        return Root.Usings.Any(usingDirective =>
            usingDirective.Name?.ToString().StartsWith(namespaceName, StringComparison.Ordinal) == true) ||
            Root.DescendantNodes()
                .OfType<QualifiedNameSyntax>()
                .Any(name => name.ToString().StartsWith(namespaceName, StringComparison.Ordinal));
    }

    private bool UsesIdentifier(string identifier) =>
        Root.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(node => node.Identifier.ValueText.Equals(identifier, StringComparison.Ordinal));

    private bool ConstructsType(string typeName) =>
        Root.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Any(node => LastTypeName(node.Type).Equals(typeName, StringComparison.Ordinal));

    private bool Invokes(string memberName) =>
        Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => InvocationName(invocation).Equals(memberName, StringComparison.Ordinal));

    public bool InvokesMemberOn(string receiverName, string memberName) =>
        FindInvocationMemberOn(receiverName, memberName) is not null;

    private InvocationExpressionSyntax? FindInvocationMemberOn(string receiverName, string memberName) =>
        Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText.Equals(memberName, StringComparison.Ordinal) &&
                MemberName(memberAccess.Expression).Equals(receiverName, StringComparison.Ordinal));

    private bool AssignsToMember(string memberName) =>
        Root.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Any(assignment => MemberName(assignment.Left).Equals(memberName, StringComparison.Ordinal));

    private IReadOnlyList<string> StringLiterals() =>
        Root.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(static literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
            .Select(static literal => literal.Token.ValueText)
            .ToArray();

    private string RelativePath() =>
        System.IO.Path.GetRelativePath(ArchitecturePaths.RepoRoot(), Path);

    private int LineNumber(SyntaxNode node) =>
        Tree.GetLineSpan(node.Span).StartLinePosition.Line + 1;

    private static string MethodName(BaseMethodDeclarationSyntax declaration) =>
        declaration switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
            _ => string.Empty
        };

    private static string InvocationName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => invocation.Expression.ToString()
        };

    private static string MemberName(ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => expression.ToString()
        };

    private static string LastTypeName(TypeSyntax type) =>
        type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => type.ToString()
        };

    private static void EnsureSimpleIdentifier(string identifier)
    {
        if (!SyntaxFacts.IsValidIdentifier(identifier))
        {
            throw new ArgumentException(
                $"Identifier assertions accept one C# identifier token, not '{identifier}'. Use a member, syntax, or semantic assertion for compound patterns.",
                nameof(identifier));
        }
    }

    private static string AccessibilityOf(MemberDeclarationSyntax declaration)
    {
        if (declaration.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return "public";
        }

        if (declaration.Modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            return "private";
        }

        if (declaration.Modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return "protected";
        }

        if (declaration.Modifiers.Any(SyntaxKind.InternalKeyword))
        {
            return "internal";
        }

        return declaration.Parent is BaseTypeDeclarationSyntax ? "private" : "internal";
    }

    private static string NormalizeType(string typeName) =>
        Regex.Replace(typeName, @"\s+", string.Empty);
}
