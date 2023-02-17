namespace CPIT.DynamicForm;

/// <summary>
///     Defines the inputs to be filtered against in the UI component of Query Builder
/// </summary>
public class InputDefinition
{
    public string Label { get; set; }

    public string Field { get; set; }

    public string Type { get; set; }

    public string Input { get; set; }

    public string? Source { get; set; }

    public object Values { get; set; }

    public List<string>? Operators { get; set; }

    public string Id { get; set; }
}