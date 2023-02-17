namespace CPIT.DynamicForm.Attributes;

public class DynamicQueryInputAttribute : Attribute
{
    public string? Source { get; set; }
    public string InputManager { get; set; }
    public InputType Type { get; set; }
    public string Label { get; set; }
    public string Id { get; set; }
}