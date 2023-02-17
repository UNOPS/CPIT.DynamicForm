using System.ComponentModel;
using CPIT.DynamicForm.Attributes;

namespace CPIT.DynamicForm;

public enum InputType
{
    [Description("text")] [Operators(Operator.Equal, Operator.NotEqual, Operator.Contains)]
    Text = 1,

    [Description("number")]
    [Operators(Operator.Equal, Operator.NotEqual, Operator.GreaterThan, Operator.LessThan, Operator.GreaterOrEqual,
        Operator.LessOrEqual)]
    Number = 2,

    [Description("select")] [Operators(Operator.IsOneOf, Operator.NotIn)]
    Dropdown = 3,

    [Description("text")] [Operators(Operator.Between)]
    DateTime = 4,

    [Description("text")] [Operators(Operator.Between)]
    Date = 5,

    [Description("radio")] [Operators(Operator.Equal, Operator.NotEqual)]
    Boolean = 6
}