using UglyAppFramework.Interfaces;

namespace UglyAppFramework;

public class UglyApp
{
    public static void Start(IUglyLoader loader)
    {
        UglyApp app = new UglyApp();
        app.Load(loader);
    }

    private DependencyManager.DependencyManager _dependencyManager;
    
    public UglyApp()
    {
        _dependencyManager = new DependencyManager.DependencyManager();
    }

    public void Load(IUglyLoader loader)
    {
        _dependencyManager.InjectDependencies(loader);
        loader.Load();
    }
}