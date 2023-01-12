using UglyAppFramework.DependencyManager.Attributes;

namespace Tests;

[Managed(Identifier = "test1")]
public class TestItem1 : ITestItemBase
{
    [Inject(Identifier = "test2")] public ITestItemBase Test2Obj { get; set; }
    public string GetTestString()
    {
        return "Test1";
    }
}