using TgUI;
using TgUI_Demo.ViewModel;

Console.WriteLine("Hello, World!");
TgApplication tgApplication = new TgApplication(File.ReadAllText("token.txt"), typeof(TestViewModel));
tgApplication.Start();
Console.ReadLine();