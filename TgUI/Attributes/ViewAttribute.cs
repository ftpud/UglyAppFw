using TgUI.View;

namespace TgUI.Attributes;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class ViewAttribute : Attribute
{
    public Type ViewType { get; set; }

    public ViewAttribute(Type view)
    {
        ViewType = view;
    }
}