using System.ComponentModel;
using System.Reflection;

namespace CPIT.DynamicForm;

public static class QueryBuilderExtensions
{
    public static TAttribute GetAttribute<TAttribute>(this Enum value)
        where TAttribute : Attribute
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);

        if (string.IsNullOrWhiteSpace(name)) return null;

        return type.GetField(name) // I prefer to get attributes this way
            .GetCustomAttributes(false)
            .OfType<TAttribute>()
            .SingleOrDefault();
    }

    public static string GetDescription(this Enum value)
    {
        var attribute = value.GetAttribute<DescriptionAttribute>();
        return attribute.Description;
    }

    public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));

        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }

    public static DateTime ToDateTimeDbFunction(this string s)
    {
        throw new NotSupportedException("You should implement ToDateTimeDbFunction function in your project");
    }
}