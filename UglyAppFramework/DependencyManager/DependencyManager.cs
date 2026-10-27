using System.Reflection;
using System.Runtime.CompilerServices;
using UglyAppFramework.DependencyManager.Attributes;

namespace UglyAppFramework.DependencyManager;

[Managed]
public class DependencyManager
{
    
    private static readonly AsyncLocal<DependencyManager?> CurrentScope = new();
    private readonly object _sync = new();
    // Objects are injected once, whether on construction (scope) or later (e.g. on push).
    private readonly ConditionalWeakTable<object, object> _injected = new();

    // The manager whose scope encloses the running code, if any. See Enter.
    public static DependencyManager? Current => CurrentScope.Value;

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
    }

    // Within the returned scope (flows across awaits), new states are injected on construction.
    public IDisposable Enter()
    {
        var previous = CurrentScope.Value;
        CurrentScope.Value = this;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly DependencyManager? _previous;
        public Scope(DependencyManager? previous) => _previous = previous;
        public void Dispose() => CurrentScope.Value = _previous;
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
        lock (_sync)
        {
            if (!DependencyRepository.Any(e => e.ObjectType == entity.ObjectType && e.Identifier == entity.Identifier))
                DependencyRepository.Add(entity);
        }
    }

    public void InjectDependencies(Object target)
    {
        lock (_sync)
        {
            if (_injected.TryGetValue(target, out _)) return;
            _injected.Add(target, this);
            InjectDependenciesRecursive(target);
            InvokePostConstruct(target);
        }
    }

    private void InjectDependenciesRecursive(Object target)
    {
        for (var targetType = target.GetType(); targetType != null; targetType = targetType.BaseType)
        {
            foreach (var property in targetType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance |
                         BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var attribute = property.GetCustomAttribute<InjectAttribute>(true);
                if (attribute != null)
                    property.SetValue(target, GetDependencyInstance(property.PropertyType, attribute));
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
        // Fall back to a single concrete implementation of an interface or base type.
        if (matchingDependencies.Count == 0 && attribute.Identifier == null)
            matchingDependencies = DependencyRepository
                .Where(dependency => dependency.Identifier == null && !dependency.ObjectType.IsAbstract &&
                                     targetType.IsAssignableFrom(dependency.ObjectType)).ToList();
        if (matchingDependencies.Count != 1)
        {
            throw new Exception($"Can't find matching dependency for the type {targetType}. " +
                                $"Exact one matching dependency is expected but found {matchingDependencies.Count}");
        }

        return matchingDependencies.First();
    }

    public Object GetDependencyInstance(Type targetType, InjectAttribute attribute)
    {
        lock (_sync)
        {
            var dependency = GetDependencyEntityFromRepo(targetType, attribute);
            if (dependency.Scope == DependencyEntity.DependencyScope.Prototype)
            {
                var prototype = Activator.CreateInstance(dependency.ObjectType)!;
                InjectDependencies(prototype);
                return prototype;
            }
            if (dependency.DependencyInstance == null)
            {
                var instance = Activator.CreateInstance(dependency.ObjectType)!;
                dependency.DependencyInstance = instance;
                try { InjectDependencies(instance); }
                catch
                {
                    dependency.DependencyInstance = null;
                    throw;
                }
            }
            return dependency.DependencyInstance;
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