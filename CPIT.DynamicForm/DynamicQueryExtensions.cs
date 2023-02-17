using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Castle.DynamicLinqQueryBuilder;

namespace CPIT.DynamicForm;

public static class DynamicQueryExtensions
{
    /// <summary>
    ///     Gets or sets a value indicating whether incoming dates in the filter should be parsed as UTC.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [parse dates as UTC]; otherwise, <c>false</c>.
    /// </value>
    public static bool ParseDatesAsUtc { get; set; } = true;

    public static Expression BuildOperatorExpression(this Expression propertyExp, IFilterRule? rule,
        BuildExpressionOptions options, Type type)
    {
        Expression expression;

        switch (rule.Operator.ToLower())
        {
            case "in":
                expression = In(type, rule.Value, propertyExp, options);
                break;
            case "not_in":
                expression = NotIn(type, rule.Value, propertyExp, options);
                break;
            case "equal":
                expression = Equals(type, rule.Value, propertyExp, options);
                break;
            case "not_equal":
                expression = NotEquals(type, rule.Value, propertyExp, options);
                break;
            case "between":
                expression = Between(type, rule.Value, propertyExp, options);
                break;
            case "not_between":
                expression = NotBetween(type, rule.Value, propertyExp, options);
                break;
            case "less":
                expression = LessThan(type, rule.Value, propertyExp, options);
                break;
            case "less_or_equal":
                expression = LessThanOrEqual(type, rule.Value, propertyExp, options);
                break;
            case "greater":
                expression = GreaterThan(type, rule.Value, propertyExp, options);
                break;
            case "greater_or_equal":
                expression = GreaterThanOrEqual(type, rule.Value, propertyExp, options);
                break;
            case "begins_with":
                expression = BeginsWith(type, rule.Value, propertyExp);
                break;
            case "not_begins_with":
                expression = NotBeginsWith(type, rule.Value, propertyExp);
                break;
            case "contains":
                expression = Contains(type, rule.Value, propertyExp);
                break;
            case "not_contains":
                expression = NotContains(type, rule.Value, propertyExp);
                break;
            case "ends_with":
                expression = EndsWith(type, rule.Value, propertyExp);
                break;
            case "not_ends_with":
                expression = NotEndsWith(type, rule.Value, propertyExp);
                break;
            case "is_empty":
                expression = IsEmpty(propertyExp);
                break;
            case "is_not_empty":
                expression = IsNotEmpty(propertyExp);
                break;
            case "is_null":
                expression = IsNull(propertyExp);
                break;
            case "is_not_null":
                expression = IsNotNull(propertyExp);
                break;
            default:
                throw new Exception($"Unknown expression operator: {rule.Operator}");
        }

        return expression;
    }

    private static Expression GetNullCheckExpression(Expression propertyExp)
    {
        var isNullable = !propertyExp.Type.IsValueType || Nullable.GetUnderlyingType(propertyExp.Type) != null;

        if (isNullable)
            return Expression.NotEqual(propertyExp,
                Expression.Constant(propertyExp.Type.GetDefaultValue(), propertyExp.Type));
        return Expression.Equal(Expression.Constant(true, typeof(bool)),
            Expression.Constant(true, typeof(bool)));
    }


    private static Expression IsNull(Expression propertyExp)
    {
        var isNullable = !propertyExp.Type.IsValueType || Nullable.GetUnderlyingType(propertyExp.Type) != null;

        if (isNullable)
        {
            var someValue = Expression.Constant(null, propertyExp.Type);

            Expression exOut = Expression.Equal(propertyExp, someValue);

            return exOut;
        }

        return Expression.Equal(Expression.Constant(true, typeof(bool)),
            Expression.Constant(false, typeof(bool)));
    }

    private static Expression IsNotNull(Expression propertyExp)
    {
        return Expression.Not(IsNull(propertyExp));
    }

    private static Expression IsEmpty(Expression propertyExp)
    {
        var someValue = Expression.Constant(0, typeof(int));

        var nullCheck = GetNullCheckExpression(propertyExp);

        Expression exOut;

        if (IsGenericList(propertyExp.Type))
        {
            exOut = Expression.Property(propertyExp, propertyExp.Type.GetProperty("Count"));

            exOut = Expression.AndAlso(nullCheck, Expression.Equal(exOut, someValue));
        }
        else
        {
            exOut = Expression.Property(propertyExp, typeof(string).GetProperty("Length"));

            exOut = Expression.AndAlso(nullCheck, Expression.Equal(exOut, someValue));
        }

        return exOut;
    }

    private static Expression IsNotEmpty(Expression propertyExp)
    {
        return Expression.Not(IsEmpty(propertyExp));
    }

    private static Expression Contains(Type type, object value, Expression propertyExp)
    {
        if (value is Array items) value = items.GetValue(0);

        var someValue = Expression.Constant(value.ToString().ToLower(), typeof(string));

        var nullCheck = GetNullCheckExpression(propertyExp);

        var method = propertyExp.Type.GetMethod("Contains", new[] {type});

        Expression exOut = Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));

        exOut = Expression.AndAlso(nullCheck, Expression.Call(exOut, method, someValue));

        return exOut;
    }

    private static Expression NotContains(Type type, object value, Expression propertyExp)
    {
        return Expression.Not(Contains(type, value, propertyExp));
    }

    private static Expression EndsWith(Type type, object value, Expression propertyExp)
    {
        if (value is Array items) value = items.GetValue(0);

        var someValue = Expression.Constant(value.ToString().ToLower(), typeof(string));

        var nullCheck = GetNullCheckExpression(propertyExp);

        var method = propertyExp.Type.GetMethod("EndsWith", new[] {type});

        Expression exOut = Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));

        exOut = Expression.AndAlso(nullCheck, Expression.Call(exOut, method, someValue));

        return exOut;
    }

    private static Expression NotEndsWith(Type type, object value, Expression propertyExp)
    {
        return Expression.Not(EndsWith(type, value, propertyExp));
    }

    private static Expression BeginsWith(Type type, object value, Expression propertyExp)
    {
        if (value is Array items) value = items.GetValue(0);

        var someValue = Expression.Constant(value.ToString().ToLower(), typeof(string));

        var nullCheck = GetNullCheckExpression(propertyExp);

        var method = propertyExp.Type.GetMethod("StartsWith", new[] {type});

        Expression exOut = Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));

        exOut = Expression.AndAlso(nullCheck, Expression.Call(exOut, method, someValue));

        return exOut;
    }

    private static Expression NotBeginsWith(Type type, object value, Expression propertyExp)
    {
        return Expression.Not(BeginsWith(type, value, propertyExp));
    }


    private static Expression NotEquals(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        return Expression.Not(Equals(type, value, propertyExp, options));
    }


    private static Expression Equals(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        Expression someValue = GetConstants(type, value, false, options).First();

        Expression exOut;
        if (type == typeof(string))
        {
            var nullCheck = GetNullCheckExpression(propertyExp);

            exOut = Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            someValue = Expression.Call(someValue, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            exOut = Expression.AndAlso(nullCheck, Expression.Equal(exOut, someValue));
        }
        else if (type == typeof(int) && propertyExp.Type == typeof(string))
        {
            var nullCheck = GetNullCheckExpression(propertyExp);

            exOut = ConvertToNumber(propertyExp);

            exOut =
                Expression.AndAlso(nullCheck,
                    Expression.Equal(exOut, Expression.Convert(someValue, type)));
        }
        else
        {
            exOut = Expression.Equal(propertyExp, Expression.Convert(someValue, propertyExp.Type));
        }

        return exOut;
    }

    private static Expression GetIsNumericCheckExpression(Expression propertyExp)
    {
        var regMethod = typeof(Regex).GetMethod(nameof(Regex.IsMatch),
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] {typeof(string), typeof(string)}, null);

        return Expression.Call(regMethod, propertyExp,
            Expression.Constant("^(([-+]?[0-9]+(\".[0-9]+)?)|([-+]?\".[0-9]+))$"));
    }

    private static Expression GetIsDateCheckExpression(Expression propertyExp)
    {
        var regMethod = typeof(Regex).GetMethod(nameof(Regex.IsMatch),
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] {typeof(string), typeof(string)}, null);

        return Expression.Call(regMethod, propertyExp, Expression.Constant("^\\d{2}/\\d{2}/\\d{4} .*$"));
    }

    private static Expression ConvertToNumber(Expression propertyExp)
    {
        var method = typeof(Convert).GetMethod("ToInt32",
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] {typeof(string)}, null);

        return Expression.Call(method, propertyExp);
    }

    private static Expression ConvertToDate(Expression propertyExp)
    {
        var method = typeof(QueryBuilderExtensions).GetMethod(nameof(QueryBuilderExtensions.ToDateTimeDbFunction),
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] {typeof(string)}, null);

        return Expression.Call(method, propertyExp);
    }

    private static Expression LessThan(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        var someValue = GetConstants(type, value, false, options).First();

        Expression exOut;
        if (type == typeof(int) && propertyExp.Type == typeof(string))
        {
            var nullCheck = GetNullCheckExpression(propertyExp);

            exOut = ConvertToNumber(propertyExp);

            exOut = Expression.AndAlso(nullCheck, Expression.LessThan(exOut, Expression.Convert(someValue, type)));
        }
        else
        {
            exOut = Expression.LessThan(propertyExp, Expression.Convert(someValue, propertyExp.Type));
        }


        return exOut;
    }

    private static Expression LessThanOrEqual(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        var someValue = GetConstants(type, value, false, options).First();

        Expression exOut;
        if (type == typeof(int) && propertyExp.Type == typeof(string))
        {
            var nullCheck = GetNullCheckExpression(propertyExp);

            exOut = ConvertToNumber(propertyExp);

            exOut = Expression.AndAlso(nullCheck,
                Expression.LessThanOrEqual(exOut, Expression.Convert(someValue, type)));
        }
        else
        {
            exOut = Expression.LessThanOrEqual(propertyExp, Expression.Convert(someValue, propertyExp.Type));
        }


        return exOut;
    }

    private static Expression GreaterThan(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        var someValue = GetConstants(type, value, false, options).First();

        Expression exOut;
        if (type == typeof(int) && propertyExp.Type == typeof(string))
        {
            var nullCheck = GetNullCheckExpression(propertyExp);

            exOut = ConvertToNumber(propertyExp);

            exOut = Expression.AndAlso(nullCheck, Expression.GreaterThan(exOut, Expression.Convert(someValue, type)));
        }
        else
        {
            exOut = Expression.GreaterThan(propertyExp, Expression.Convert(someValue, propertyExp.Type));
        }

        return exOut;
    }

    private static Expression GreaterThanOrEqual(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        var someValue = GetConstants(type, value, false, options).First();

        Expression exOut;
        if (type == typeof(int) && propertyExp.Type == typeof(string))
        {
            var nullCheck = GetNullCheckExpression(propertyExp);

            exOut = ConvertToNumber(propertyExp);

            exOut =
                Expression.AndAlso(nullCheck,
                    Expression.GreaterThanOrEqual(exOut, Expression.Convert(someValue, type)));
        }
        else
        {
            exOut = Expression.GreaterThanOrEqual(propertyExp, Expression.Convert(someValue, propertyExp.Type));
        }

        return exOut;
    }

    private static Expression Between(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        var someValue = GetConstants(type, value, true, options);
        Expression exOut;

        if (type == typeof(DateTime) && propertyExp.Type == typeof(string))
        {
            exOut = ConvertToDate(propertyExp);

            Expression exBelow =
                Expression.GreaterThanOrEqual(exOut, Expression.Convert(someValue[0], type));
            Expression exAbove =
                Expression.LessThanOrEqual(exOut, Expression.Convert(someValue[1], type));

            exOut = Expression.And(exBelow, exAbove);
        }

        else
        {
            Expression exBelow =
                Expression.GreaterThanOrEqual(propertyExp, Expression.Convert(someValue[0], propertyExp.Type));
            Expression exAbove =
                Expression.LessThanOrEqual(propertyExp, Expression.Convert(someValue[1], propertyExp.Type));

            exOut = Expression.And(exBelow, exAbove);
        }

        return exOut;
    }

    private static Expression NotBetween(Type type, object value, Expression propertyExp,
        BuildExpressionOptions options)
    {
        return Expression.Not(Between(type, value, propertyExp, options));
    }

    private static Expression In(this Type type, object value, Expression propertyExp, BuildExpressionOptions options)
    {
        var someValues = GetConstants(type, value, true, options);

        var nullCheck = GetNullCheckExpression(propertyExp);

        if (IsGenericList(propertyExp.Type))
        {
            var genericType = propertyExp.Type.GetGenericArguments().First();
            var method = propertyExp.Type.GetMethod("Contains", new[] {genericType});
            Expression exOut;

            if (someValues.Count > 1)
            {
                exOut = Expression.Call(propertyExp, method, Expression.Convert(someValues[0], genericType));
                var counter = 1;
                while (counter < someValues.Count)
                {
                    exOut = Expression.Or(exOut,
                        Expression.Call(propertyExp, method, Expression.Convert(someValues[counter], genericType)));
                    counter++;
                }
            }
            else
            {
                exOut = Expression.Call(propertyExp, method, Expression.Convert(someValues.First(), genericType));
            }


            return Expression.AndAlso(nullCheck, exOut);
        }
        else
        {
            Expression exOut;

            if (someValues.Count > 1)
            {
                if (type == typeof(string))
                {
                    exOut = Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                    var somevalue = Expression.Call(someValues[0],
                        typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                    exOut = Expression.Equal(exOut, somevalue);
                    var counter = 1;
                    while (counter < someValues.Count)
                    {
                        var nextvalue = Expression.Call(someValues[counter],
                            typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                        exOut = Expression.Or(exOut,
                            Expression.Equal(
                                Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                                nextvalue));
                        counter++;
                    }
                }
                else
                {
                    exOut = Expression.Equal(propertyExp, Expression.Convert(someValues[0], propertyExp.Type));
                    var counter = 1;
                    while (counter < someValues.Count)
                    {
                        exOut = Expression.Or(exOut,
                            Expression.Equal(propertyExp,
                                Expression.Convert(someValues[counter], propertyExp.Type)));
                        counter++;
                    }
                }
            }
            else
            {
                if (type == typeof(string))
                {
                    exOut = Expression.Call(propertyExp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                    var somevalue = Expression.Call(someValues.First(),
                        typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                    exOut = Expression.Equal(exOut, somevalue);
                }
                else
                {
                    exOut = Expression.Equal(propertyExp, Expression.Convert(someValues.First(), propertyExp.Type));
                }
            }


            return Expression.AndAlso(nullCheck, exOut);
        }
    }

    private static Expression NotIn(Type type, object value, Expression propertyExp, BuildExpressionOptions options)
    {
        return Expression.Not(In(type, value, propertyExp, options));
    }

    private static bool IsGenericList(this Type o)
    {
        var isGenericList = false;

        var oType = o;

        if (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                                    oType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                                    oType.GetGenericTypeDefinition() == typeof(List<>)))
            isGenericList = true;

        return isGenericList;
    }

    private static object GetDefaultValue(this Type type)
    {
        return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static List<ConstantExpression> GetConstants(Type type, object value, bool isCollection,
        BuildExpressionOptions options)
    {
        if (type == typeof(DateTime) && (options.ParseDatesAsUtc || ParseDatesAsUtc))
        {
            DateTime tDate;
            if (isCollection)
            {
                if (!(value is string) && value is IEnumerable list)
                {
                    var constants = new List<ConstantExpression>();

                    foreach (var item in list)
                    {
                        var date = DateTime.TryParse(item.ToString().Trim(), options.CultureInfo,
                            DateTimeStyles.AdjustToUniversal, out tDate)
                            ? (DateTime?)
                            tDate
                            : null;
                        constants.Add(Expression.Constant(DateTime.SpecifyKind(date.Value, DateTimeKind.Utc), type));
                    }

                    return constants;
                }

                var vals =
                    value.ToString().Split(new[] {",", "[", "]", "\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(
                            p =>
                                DateTime.TryParse(p.Trim(), options.CultureInfo,
                                    DateTimeStyles.AdjustToUniversal, out tDate)
                                    ? (DateTime?)
                                    tDate
                                    : null).Select(p =>
                            Expression.Constant(p, type));
                return vals.ToList();
            }

            if (value is Array items) value = items.GetValue(0);
            return new List<ConstantExpression>
            {
                Expression.Constant(DateTime.TryParse(value.ToString().Trim(), options.CultureInfo,
                    DateTimeStyles.AdjustToUniversal, out tDate)
                    ? (DateTime?)
                    tDate
                    : null)
            };
        }

        if (isCollection)
        {
            var tc = TypeDescriptor.GetConverter(type);
            if (type == typeof(string))
            {
                if (!(value is string) && value is IEnumerable list)
                {
                    var expressions = new List<ConstantExpression>();

                    foreach (var item in list)
                        expressions.Add(Expression.Constant(tc.ConvertFromString(item.ToString()), type));

                    return expressions;
                }

                var bracketSplit = value.ToString()
                    .Split(new[] {"[", "]"}, StringSplitOptions.RemoveEmptyEntries);
                var vals =
                    bracketSplit.SelectMany(v => v.Split(new[] {",", "\r\n"}, StringSplitOptions.None))
                        .Select(p => tc.ConvertFromString(null, options.CultureInfo, p.Trim())).Select(p =>
                            Expression.Constant(p, type));
                return vals.Distinct().ToList();
            }
            else
            {
                if (!(value is string) && value is IEnumerable list)
                {
                    var expressions = new List<ConstantExpression>();

                    foreach (var item in list)
                        expressions.Add(Expression.Constant(tc.ConvertFromString(item.ToString()), type));

                    return expressions;
                }

                var vals =
                    value.ToString().Split(new[] {",", "[", "]", "\r\n"},
                            StringSplitOptions.RemoveEmptyEntries)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => tc.ConvertFromString(null, options.CultureInfo, p.Trim())).Select(p =>
                            Expression.Constant(p, type));
                return vals.ToList();
            }
        }
        else
        {
            var tc = TypeDescriptor.GetConverter(type);
            if (value is Array items) value = items.GetValue(0);

            return new List<ConstantExpression>
            {
                Expression.Constant(tc.ConvertFromString(null, options.CultureInfo, value.ToString().Trim()))
            };
        }
    }
}