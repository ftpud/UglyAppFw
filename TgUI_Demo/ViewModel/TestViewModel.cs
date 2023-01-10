using System.Diagnostics.Metrics;
using Telegram.Bot.Types;
using TgUI_Demo.Model;
using TgUI_Demo.View;
using TgUI.Attributes;
using TgUI.Entity;
using TgUIForm.Elements;

namespace TgUI_Demo.ViewModel;

[TgUI.Attributes.View(typeof(TestView))]
public class TestViewModel : TgUI.Entity.ViewModel
{
    public SessionContext ctx => Context; 

    public String someValue {
        get { return _someValue; }
        set { SetProperty(ref _someValue, value); }
    }
    private String _someValue = "Initital value";
    
    
    public int number {
        get { return _number; }
        set { SetProperty(ref _number, value); }
    }
    private int _number = 0;
    
    private int counter = 0;

    public override void Initialize()
    {
        Console.WriteLine($"New Pushed for {Context.CurrentUserId}");
        TestModel.Online.CollectionChanged += (sender, args) => PropertyChanged();
        
        base.Initialize();
    }

    [CallBack(Trigger = "/increase")]
    public void callBack1(Update update)
    {
        counter++;
        someValue = counter.ToString();
        TestModel.Online.Add(Context.CurrentUserId + " i++");
    }
    
    [CallBack(Trigger = "/decrease")]
    public void callBack2(Update update)
    {
        counter--;
        someValue = counter.ToString();
        TestModel.Online.Add(Context.CurrentUserId + " i--");
    }
    
    [CallBack(Trigger = "/sharedInc")]
    public void callBack3(Update update)
    {
        TestModel.SharedValue++;
        PropertyChanged();
        //TestModel.Online.Add(Context.CurrentUserId + " j++");
    }
    
    [CallBack(Trigger = "/sharedDec")]
    public void callBack4(Update update)
    {
        TestModel.SharedValue--;
        PropertyChanged();
        //TestModel.Online.Add(Context.CurrentUserId + " j--");
    }
    
    [CallBack(Trigger = "/page2")]
    public void callBack5(Update update)
    {
        TestModel.Online.Add(Context.CurrentUserId + " page2");
        Push(new Page2ViewModel());
    }
    
    [CallBack(Trigger = "/number")]
    public void callBack6(Update update)
    {
        int num;
        Push(new UiTextDialog("Введите число", s => number = int.Parse(s), s => int.TryParse(s, out num) ?"":"Неее, число плес"));
    }
    
    [CallBack(Trigger = "/picker")]
    public void callBack7(Update update)
    {
        
        var model = new Dictionary<string, Object>()
        {
            {"ноль", 0},
            {"один", 1}, 
            {"кек", 1488}, 
        };
        
        Push(new UiPickerDialog("Число плес", model, (s, i) => number = (int)i ));
    }
    
}