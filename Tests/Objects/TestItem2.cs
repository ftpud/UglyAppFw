using UglyAppFramework.DependencyManager.Attributes;

namespace Tests;

[Managed(Identifier = "test2")]
public class TestItem2 : ITestItemBase
{
    public string GetTestString()
    {
        return "Test2";
    }
}