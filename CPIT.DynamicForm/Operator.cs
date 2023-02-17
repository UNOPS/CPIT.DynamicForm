using System.ComponentModel;

namespace CPIT.DynamicForm;

public enum Operator
{
    [Description("equal")] Equal,

    [Description("not_equal")] NotEqual,

    [Description("less")] LessThan,

    [Description("greater")] GreaterThan,

    [Description("between")] Between,

    [Description("not_in")] NotIn,

    [Description("is_null")] IsNull,

    [Description("is_not_null")] IsNotNull,

    [Description("in")] IsOneOf,

    [Description("contains")] Contains,

    [Description("less_or_equal")] LessOrEqual,

    [Description("greater_or_equal")] GreaterOrEqual
}