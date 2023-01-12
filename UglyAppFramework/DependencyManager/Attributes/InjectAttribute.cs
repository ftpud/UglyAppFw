namespace UglyAppFramework.DependencyManager.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class InjectAttribute : Attribute
{
    public String? Identifier { get; set; }
}