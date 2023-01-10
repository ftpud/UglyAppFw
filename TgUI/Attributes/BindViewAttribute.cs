using TgUI.View;

namespace TgUI.Attributes;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class BindViewAttribute : Attribute
{
    public Type ViewType { get; set; }

    public BindViewAttribute(Type view)
    {
        ViewType = view;
    }
}