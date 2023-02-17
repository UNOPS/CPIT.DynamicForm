namespace CPIT.DynamicForm.InputManager;

public class EntityManagerAttribute : Attribute
{
    public EntityManagerAttribute(string key)
    {
        Key = key;
    }

    public string Key { get; set; }
}