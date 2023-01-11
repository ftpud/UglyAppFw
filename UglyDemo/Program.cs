using UglyAppFramework;
using UglyDemo;
using UglyDemo.ViewModel;
using UglyTgApplication;

Console.WriteLine("Hello, World!");
UglyTgApp.Start(File.ReadAllText("token.txt"), typeof(MainPageViewModel));

Console.ReadLine();