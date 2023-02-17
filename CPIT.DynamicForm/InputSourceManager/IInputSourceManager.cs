namespace CPIT.DynamicForm.InputSourceManager;

public interface IInputSourceManager
{
    IEnumerable<DropdownModel> GetSourceValues();
}

public class DropdownModel
{
    public int Id { get; set; }
    public string Name { get; set; }
}