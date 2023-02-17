namespace CPIT.DynamicForm.Attributes;

public class OperatorsAttribute : Attribute
{
    public OperatorsAttribute(params Operator[] operators)
    {
        Operators = operators;
    }

    public Operator[] Operators { get; set; }
}