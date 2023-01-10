using System.Collections.ObjectModel;

namespace TgUI_Demo.Model;

public class TestModel
{
    public static ObservableCollection<String> Online { get; set; } = new ObservableCollection<string>();
    public static int SharedValue { get; set; }
}