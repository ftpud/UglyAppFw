using UglyAppFramework.DependencyManager.Attributes;

namespace Tests;

[Managed]
public class TestItemNoId : ITestItemBase
{
    public string GetTestString()
    {
        return "NoId";
    }
}