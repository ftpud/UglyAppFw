using System.Reflection;
using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.DependencyManager;

[Managed]
public class DependencyManager
{
    private Dictionary<Type, Object?> _dependencyRepository = new Dictionary<Type, object?>();

    public DependencyManager()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var type in GetTypesWithHelpAttribute(assembly, typeof(ManagedAttribute)))
            {
                _dependencyRepository.Add(type, null);
            }
        }

        _dependencyRepository[this.GetType()] = this;
    }

    public void InjectDependencies(Object target)
    {
        var type = target.GetType();
        foreach (var fieldInfo in type.GetProperties(
                     BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static))
        {
            var fieldAttribute = fieldInfo.GetCustomAttribute(typeof(InjectAttribute), true);
            if (fieldAttribute != null)
            {
                fieldInfo.SetValue(target, GetDependencyForType(fieldInfo.PropertyType));
            }
        }
    }

    public Object GetDependencyForType(Type type)
    {
        if (_dependencyRepository.ContainsKey(type))
        {
            var dependency = _dependencyRepository[type];
            if (dependency == null)
            {
                _dependencyRepository[type] = Activator.CreateInstance(type);
                InjectDependencies(_dependencyRepository[type]);
            }

            return _dependencyRepository[type];
        }

        throw new Exception($"Dependency of type {type} can not be found");
    }

    static IEnumerable<Type> GetTypesWithHelpAttribute(Assembly assembly, Type attributeType)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.GetCustomAttributes(attributeType, true).Length > 0)
            {
                yield return type;
            }
        }
    }
}
