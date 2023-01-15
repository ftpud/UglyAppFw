using System.Security.Cryptography.X509Certificates;
using UglyAppFramework;
using UglyAppFramework.DependencyManager.Attributes;
using UglyAppFramework.Interfaces;

namespace Tests;

public class Tests
{
    class TestClass : IUglyLoader
    {
        [Inject(Identifier = "test1")] public ITestItemBase? Test1Instance { get; set; }
        [Inject(Identifier = "test2")] public ITestItemBase? Test2Instance { get; set; }
        [Inject(Identifier = "test1")] public TestItem1? SameRef { get; set; }
        [Inject] public TestItemNoId? NoIdObj { get; set; }

        public void Load()
        {
        }
    }

    [Test]
    public void IoC_Test()
    {
        var testClass = new TestClass();
        UglyApp.Start(testClass);
        Assert.That(testClass.Test1Instance.GetTestString(), Is.EqualTo("Test1"));
        Assert.That(testClass.Test2Instance.GetTestString(), Is.EqualTo("Test2"));
        Assert.That(testClass.Test1Instance, Is.EqualTo(testClass.SameRef));
        Assert.That(testClass.SameRef.Test2Obj.GetTestString(), Is.EqualTo("Test2"));
        Assert.That(testClass.NoIdObj.GetTestString(), Is.EqualTo("NoId"));
    }
    
    [SetUp]
    public void Setup()
    {
    }
}