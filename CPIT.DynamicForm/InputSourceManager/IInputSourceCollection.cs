using System.Reflection;
using CPIT.DynamicForm.InputManager;
using Microsoft.EntityFrameworkCore;

namespace CPIT.DynamicForm.InputSourceManager;

public interface IInputSourceCollection
{
    public IInputSourceManager? GetManager(string? key);
}