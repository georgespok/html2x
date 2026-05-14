namespace Html2x.Architecture.Test.Support;

internal readonly record struct DocumentTopic(string Value)
{
    public static DocumentTopic Text(string value) => new(value);

    public static DocumentTopic Type<T>() => new(typeof(T).Name);

    public static DocumentTopic Type(Type type) => new(type.Name);

    public static DocumentTopic FullType<T>() =>
        new(typeof(T).FullName ?? throw new InvalidOperationException($"{typeof(T).Name} has no full name."));

    public static DocumentTopic NamespaceOf<T>() =>
        new(typeof(T).Namespace ?? throw new InvalidOperationException($"{typeof(T).Name} has no namespace."));

    public static DocumentTopic NamespaceSegmentOf<T>() =>
        new(NamespaceOf<T>().Value.Split('.').Last());

    public static DocumentTopic NamespaceSegmentOf(Type type) =>
        new((type.Namespace ?? throw new InvalidOperationException($"{type.Name} has no namespace."))
            .Split('.')
            .Last());

    public static DocumentTopic AssemblyOf<T>() =>
        new(typeof(T).Assembly.GetName().Name ??
            throw new InvalidOperationException($"{typeof(T).Name} has no assembly name."));

    public static DocumentTopic Constant(string value) => new(value);

    public static implicit operator DocumentTopic(string value) => new(value);

    public override string ToString() => Value;
}
