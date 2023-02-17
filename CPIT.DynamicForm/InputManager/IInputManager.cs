using System.Linq.Expressions;
using Castle.DynamicLinqQueryBuilder;
using CPIT.DynamicForm.Attributes;

namespace CPIT.DynamicForm.InputManager;

public interface IInputManager
{
    List<DynamicQueryInputAttribute> GetInputs();

    Expression BuildExpression(Expression expression, IFilterRule? rule,
        BuildExpressionOptions options, Type type, string property);
}