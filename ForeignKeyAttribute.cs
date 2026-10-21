namespace MiniFramework.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    public Type ReferencedModel { get; }

    public ForeignKeyAttribute(Type referencedModel)
    {
        ReferencedModel = referencedModel;
    }
}