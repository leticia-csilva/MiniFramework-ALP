namespace MiniFramework.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SelectOptionsAttribute : Attribute
{
    public IReadOnlyList<string> Options { get; }

    public SelectOptionsAttribute(params string[] options)
    {
        if (options.Length == 0)
            throw new ArgumentException("Informe ao menos uma opção.", nameof(options));

        Options = options;
    }
}
