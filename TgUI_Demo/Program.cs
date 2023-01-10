using TgUI;
using TgUI_Demo.ViewModel;

Console.WriteLine("Todo list Example");
TgApplication tgApplication = new TgApplication(File.ReadAllText("token.txt"), typeof(MainPageViewModel));
tgApplication.Start();
Console.ReadLine();