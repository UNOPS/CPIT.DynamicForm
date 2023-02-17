namespace CPIT.DynamicForm.InputManager;

public interface IInputManagerCollection
{
    public IInputManager GetManager(string key);
}