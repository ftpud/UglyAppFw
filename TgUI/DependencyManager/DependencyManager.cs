using System.Reflection;
using TgUI.Attributes;
using TgUI.DependencyManager.Attributes;
using TgUI.States;
using TgUI.View;

namespace TgUI.DependencyManager;

[Managed]
public class DependencyManager
{
    private Dictionary<Type, IView> _viewRepository = new Dictionary<Type, IView>();
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
    }

    public void InjectDependencies(Object target)
    {
        foreach (var fieldInfo in target.GetType().GetProperties(
                     BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
        {
            var a = fieldInfo.GetCustomAttributes(typeof(InjectAttribute), false);
            var fieldAttribute = fieldInfo.GetCustomAttribute(typeof(InjectAttribute), true);
            if (fieldAttribute != null)
            {
                if (_dependencyRepository.ContainsKey(fieldInfo.PropertyType))
                {
                    var dependency = _dependencyRepository[fieldInfo.PropertyType];
                    if (dependency == null)
                    {
                        _dependencyRepository[fieldInfo.PropertyType] = Activator.CreateInstance(fieldInfo.PropertyType);
                    }

                    fieldInfo.SetValue(target, _dependencyRepository[fieldInfo.PropertyType]);
                }
                else
                {
                    throw new Exception($"Dependency of type {fieldInfo.PropertyType} can not be found");
                }
            }
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

    public IView GetView(State state)
    {
        if (!_viewRepository.ContainsKey(state.GetType()))
        {
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(state.GetType());
            foreach (System.Attribute attr in attrs)
            {
                if (attr is BindViewAttribute)
                {
                    BindViewAttribute attribute = (BindViewAttribute)attr;
                    var viewType = attribute.ViewType;
                    if (!_viewRepository.ContainsKey(viewType))
                    {
                        _viewRepository.Add(state.GetType(), (IView)Activator.CreateInstance(viewType));
                    }
                }
            }
        }

        return _viewRepository[state.GetType()];
    }
}