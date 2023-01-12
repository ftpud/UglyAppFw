namespace UglyAppFramework.DependencyManager.Attributes;

public class ManagedAttribute : Attribute
{
    public DependencyEntity.DependencyScope Scope { get; set; }

    public String Identifier { get; set; }
    public ManagedAttribute()
    {
        Scope = DependencyEntity.DependencyScope.Singletone;
    }
}