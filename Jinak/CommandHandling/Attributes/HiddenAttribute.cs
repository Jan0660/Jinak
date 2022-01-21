namespace Jinak.Attributes;

[AttributeUsage(AttributeTargets.Class |
                AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class HiddenAttribute : Attribute
{
}