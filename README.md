# Dynamic forms

It is a .Net library which helps you to create dynamic forms in your .Net app

## Before You Begin

Currently, the library only contains a dynamic query builder, and will have the dynamic form logic later.

## How To Use
#### Use the CustomBuildQuery function

The CustomBuildQuery function accepts three required parameters:

- Query: the query that you need to append the custom query to.
- Rule: the rules to build the query, see the `QueryBuilderFilterRule` class as an example of the rule object. In case you need to query a collection inside your domain class, make sure to set the `Field` as `CollectionName.{Id}`.
- InputManagerCollection: this is an instance from `InputManagerCollection`, in case of complicated queries, implement `IInputManager` interface for a property and define the logic to build the query for it.

#### Having an issue with DateTime conversion?
You should define how you are going to convert the string field to date, this depends on your database type. The following example is for a postgres DB:
- Add this function in any extension class
```
public static ModelBuilder AddSqlFunctions(this ModelBuilder modelBuilder)
        {
            modelBuilder.HasDbFunction(() => ToDateTimeDbFunction(default))
                .HasTranslation(args => new SqlFunctionExpression(
                        functionName: "to_date",
                        arguments: args.Append(new SqlFragmentExpression("'mm/dd/yyyy'")),
                        nullable: true,
                        argumentsPropagateNullability: new[] { false, true, false },
                    type: typeof(DateTime),
                    typeMapping: null));

            return modelBuilder;
        }
```
- Add the SQL function to your DBContext, inside OnModelCreating
```
builder.AddSqlFunctions();

```
