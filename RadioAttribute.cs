namespace MiniFramework.Attributes;

// Marca um enum pra ser renderizado como radio em vez de select
[AttributeUsage(AttributeTargets.Property)]
public class RadioAttribute : Attribute
{
}
