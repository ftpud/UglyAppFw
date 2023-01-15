namespace UglyAppFramework.DependencyManager;

public class DependencyEntity
{
    public Type ObjectType { get; set; }

    public String? Identifier { get; set; }
    
    public DependencyScope Scope { get; set; }
    
    public Object? DependencyInstance { get; set; }
    
    public enum DependencyScope
    {
        Singletone,
        Prototype
    }

}