using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicLinqQueryBuilder;
using CPIT.DynamicForm.Attributes;
using CPIT.DynamicForm.InputManager;

namespace CPIT.DynamicForm;

using System;
using System.Collections.Generic;
using System.Linq;

public static class CustomQueryBuilder
{
    /// <summary>
    ///     Gets the filtered collection after applying the provided filter rules.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="queryable">The queryable.</param>
    /// <param name="filterRule">The filter rule.</param>
    /// <param name="useIndexedProperty">Whether or not to use indexed property</param>
    /// <param name="indexedPropertyName">The indexable property to use</param>
    /// <param name="inputManagerCollection">inputManagerCollection</param>
    /// <returns>Filtered IQueryable</returns>
    public static IQueryable<T> CustomBuildQuery<T>(this IQueryable<T> queryable, IFilterRule? filterRule,
        IInputManagerCollection inputManagerCollection, bool useIndexedProperty = false,
        string? indexedPropertyName = null)
    {
        return BuildQuery(queryable, filterRule, inputManagerCollection, useIndexedProperty,
            indexedPropertyName);
    }

    /// <summary>
    ///     Gets the filtered collection after applying the provided filter rules.
    ///     Returns the string representation for diagnostic purposes.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="queryable">The queryable.</param>
    /// <param name="filterRule">The filter rule.</param>
    /// <param name="inputManagerCollection"></param>
    /// <param name="useIndexedProperty">Whether or not to use indexed property</param>
    /// <param name="indexedPropertyName">The indexable property to use</param>
    /// <returns>Filtered IQueryable.</returns>
    private static IQueryable<T> BuildQuery<T>(this IQueryable<T> queryable, IFilterRule? filterRule,
        IInputManagerCollection inputManagerCollection, bool useIndexedProperty = false,
        string? indexedPropertyName = null)
    {
        return BuildQuery(queryable, filterRule,
            new BuildExpressionOptions
                {UseIndexedProperty = useIndexedProperty, IndexedPropertyName = indexedPropertyName},
            inputManagerCollection);
    }


    /// <summary>
    ///     Gets the filtered collection after applying the provided filter rules.
    ///     Returns the string representation for diagnostic purposes.
    /// </summary>
    /// <typeparam name="T">The generic type.</typeparam>
    /// <param name="queryable">The queryable.</param>
    /// <param name="filterRule">The filter rule.</param>
    /// <param name="options">The options to use when building the expression</param>
    /// <param name="inputManagerCollection"></param>
    /// <returns>Filtered IQueryable.</returns>
    private static IQueryable<T> BuildQuery<T>(this IQueryable<T> queryable, IFilterRule? filterRule,
        BuildExpressionOptions options, IInputManagerCollection inputManagerCollection)
    {
        var expression = BuildExpressionLambda<T>(filterRule, options, inputManagerCollection);

        var whereCallExpression = Expression.Call(
            typeof(Queryable),
            "Where",
            new[] {queryable.ElementType},
            queryable.Expression,
            expression);

        var filteredResults = queryable.Provider.CreateQuery<T>(whereCallExpression);

        return filteredResults;
    }

    /// <summary>
    ///     Builds a predicate that returns whether an input test object passes the filter rule.
    /// </summary>
    /// <typeparam name="T">The generic type of the input object to test.</typeparam>
    /// <param name="filterRule">The filter rule.</param>
    /// <param name="options">The options to use when building the expression</param>
    /// <param name="inputManagerCollection"></param>
    /// <returns>A predicate function implementing the filter rule</returns>
    private static Func<T, bool> BuildPredicate<T>(this IFilterRule? filterRule, BuildExpressionOptions options,
        IInputManagerCollection inputManagerCollection)
    {
        var expression = BuildExpressionLambda<T>(filterRule, options, inputManagerCollection);

        return expression.Compile();
    }

    /// <summary>
    ///     Builds an expression lambda for the filter rule.
    /// </summary>
    /// <typeparam name="T">The generic type of the input object to test.</typeparam>
    /// <param name="filterRule">The filter rule.</param>
    /// <param name="inputManagerCollection"></param>
    /// <param name="options">The options to use when building the expression</param>
    /// <returns>An expression lambda that implements the filter rule</returns>
    private static Expression<Func<T, bool>>? BuildExpressionLambda<T>(this IFilterRule? filterRule,
        BuildExpressionOptions options, IInputManagerCollection inputManagerCollection)
    {
        if (filterRule == null)
        {
            return null;
        }

        var pe = Expression.Parameter(typeof(T), "item");

        var expressionTree = BuildExpressionTree(pe, filterRule, options, inputManagerCollection);
        
        return Expression.Lambda<Func<T, bool>>(expressionTree, pe);
    }

    private static Expression BuildExpressionTree(ParameterExpression pe, IFilterRule? rule,
        BuildExpressionOptions options, IInputManagerCollection inputManagerCollection)
    {
        if (rule?.Rules != null && rule.Rules.Any())
        {
            var expressions =
                rule.Rules.Select(childRule => BuildExpressionTree(pe, childRule, options, inputManagerCollection))
                    .Where(expression => true)
                    .ToList();

            var expressionTree = expressions.First();

            var counter = 1;
            while (counter < expressions.Count)
            {
                if (rule.Condition != null)
                {
                    expressionTree = rule.Condition.ToLower() == "or"
                        ? Expression.Or(expressionTree, expressions[counter])
                        : Expression.And(expressionTree, expressions[counter]);
                }
                
                counter++;
            }

            return expressionTree;
        }

        if (rule?.Field != null)
        {
            Type type;

            switch (rule.Type.ToLower())
            {
                case "integer":
                    type = typeof(int);
                    break;
                case "double":
                    type = typeof(double);
                    break;
                case "string":
                    type = typeof(string);
                    break;
                case "date":
                case "datetime":
                    type = typeof(DateTime);
                    break;
                case "boolean":
                    type = typeof(bool);
                    break;
                default:
                    throw new Exception($"Unexpected data type {rule.Type}");
            }

            if (options.UseIndexedProperty)
            {
                var propertyExp =
                    Expression.Property(pe, options.IndexedPropertyName, Expression.Constant(rule.Field));
                return propertyExp.BuildOperatorExpression(rule, options, type);
            }

            var propertyList = rule.Field.Split('.');
            if (propertyList.Length > 1)
            {
                var propertyCollectionEnumerator = propertyList.AsEnumerable().GetEnumerator();

                return BuildNestedExpression(pe, propertyCollectionEnumerator, rule, options, type, null,
                    inputManagerCollection);
            }

            {
                var propertyExp = Expression.Property(pe, rule.Field);
                return propertyExp.BuildOperatorExpression(rule, options, type);
            }
        }

        return null;
    }

     private static Expression BuildNestedExpression(Expression expression,
            IEnumerator<string> propertyCollectionEnumerator, IFilterRule rule, BuildExpressionOptions options,
            Type type, IInputManager inputManager, IInputManagerCollection inputManagerCollection)
        {
            while (propertyCollectionEnumerator.MoveNext())
            {
                var propertyName = propertyCollectionEnumerator.Current;

                var property = expression.Type.GetProperty(propertyName);
                if (property == null && inputManager != null)
                {
                    return inputManager.BuildExpression(expression, rule, options, type, propertyName);
                }

                expression = Expression.Property(expression, property);

                var propertyType = property.PropertyType;
                var enumerable = propertyType.GetInterface("IEnumerable");
                if (propertyType != typeof(string) && enumerable != null)
                {
                    var elementType = propertyType.GetGenericArguments()[0];
                    var predicateFnType = typeof(Func<,>).MakeGenericType(elementType, typeof(bool));
                    var parameterExpression = Expression.Parameter(elementType);

                    var inputAttribute = property.GetCustomAttribute<DynamicQueryInputAttribute>();
                    if (inputAttribute.InputManager != null)
                    {
                        inputManager = inputManagerCollection.GetManager(inputAttribute.InputManager);
                    }

                    Expression body = BuildNestedExpression(parameterExpression, propertyCollectionEnumerator, rule,
                        options, type, inputManager, inputManagerCollection);

                    var predicate = Expression.Lambda(predicateFnType, body, parameterExpression);

                    var queryable = Expression.Call(typeof(Queryable), "AsQueryable", new[] {elementType}, expression);
                    
                    return Expression.Call(
                        typeof(Queryable),
                        "Any",
                        new[] {elementType},
                        queryable,
                        predicate
                    );
                }
            }

            return expression.BuildOperatorExpression(rule, options, type);
        }

}