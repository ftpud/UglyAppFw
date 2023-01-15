using System.Reflection;
using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.DependencyManager;

[Managed]
public class DependencyManager
{
    
    private List<DependencyEntity> DependencyRepository { get; set; } = new List<DependencyEntity>();

    public DependencyManager()
    {
        AddDependencyToRepo(new DependencyEntity()
        {
            Scope = DependencyEntity.DependencyScope.Singletone,
            Identifier = null,
            DependencyInstance = this,
            ObjectType = this.GetType()
        });

        AppDomain.CurrentDomain.AssemblyLoad += (sender, args) => LoadDependenciesFromAssembly(args.LoadedAssembly);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            LoadDependenciesFromAssembly(assembly);
        }

        Console.WriteLine("Dependencies loaded");
    }

    private void LoadDependenciesFromAssembly(Assembly assembly)
    {
        foreach (var type in GetTypesWithHelpAttribute(assembly, typeof(ManagedAttribute)))
        {
            // DependencyRepository.Add(type, null);
            DependencyEntity dependencyEntity = new DependencyEntity();
            var typeAttributes = (ManagedAttribute)type.GetCustomAttributes(typeof(ManagedAttribute), true).First();
            dependencyEntity.Scope = typeAttributes.Scope;
            dependencyEntity.Identifier = typeAttributes.Identifier;
            dependencyEntity.ObjectType = type;
            AddDependencyToRepo(dependencyEntity);
        }
    }

    private void AddDependencyToRepo(DependencyEntity entity)
    {
        if (!DependencyRepository.Any(e => e.ObjectType == entity.ObjectType && e.Identifier == entity.Identifier))
        {
            DependencyRepository.Add(entity);
        }
    }

    public void InjectDependencies(Object target)
    {
        InjectDependenciesRecursive(target);
        InvokePostConstruct(target);
    }

    private void InjectDependenciesRecursive(Object target)
    {
        var targetType = target.GetType();
        foreach (var fieldInfo in targetType.GetProperties(
                     BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static))
        {
            var fieldAttribute = (InjectAttribute)fieldInfo.GetCustomAttribute(typeof(InjectAttribute), true);
            if (fieldAttribute != null)
            {
                fieldInfo.SetValue(target, GetDependencyInstance(fieldInfo.PropertyType, fieldAttribute));
            }
        }
    }

    public void InvokePostConstruct(Object target)
    {
        foreach (var methodInfo in GetMethodsWithAttribute(typeof(PostConstructAttribute), target.GetType()))
        {
            methodInfo.Invoke(target, new object[] { });
        }
    }

    private IEnumerable<MethodInfo> GetMethodsWithAttribute(Type attributeType, Type target)
    {
        var methods = target.GetMethods(
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        foreach (var methodInfo in methods)
        {
            var methodAttribute = methodInfo.GetCustomAttribute(attributeType, true);
            if (methodAttribute != null)
            {
                yield return methodInfo;
            }
        }
    }

    private DependencyEntity GetDependencyEntityFromRepo(Type targetType, InjectAttribute attribute)
    {
        var matchingDependencies = DependencyRepository
            .Where(dependency => (dependency.Identifier != null && dependency.Identifier == attribute.Identifier) ||
                                 (dependency.Identifier == null && dependency.ObjectType == targetType)).ToList();
        if (matchingDependencies.Count != 1)
        {
            throw new Exception($"Can't find matching dependency for the type {targetType}. " +
                                $"Exact one matching dependency is expected but found {matchingDependencies.Count}");
        }

        return matchingDependencies.First();
    }

    public Object GetDependencyInstance(Type targetType, InjectAttribute attribute)
    {
        var dependency = GetDependencyEntityFromRepo(targetType, attribute);
        if (dependency.Scope == DependencyEntity.DependencyScope.Singletone)
        {
            if (dependency.DependencyInstance == null)
            {
                var instance = Activator.CreateInstance(dependency.ObjectType);
                dependency.DependencyInstance = instance;
                InjectDependencies(instance);
            }

            return dependency.DependencyInstance;
        }
        else
        {
            throw new NotImplementedException();
        }
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