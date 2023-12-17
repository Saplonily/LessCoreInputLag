namespace Celeste.Mod.LessCoreInputLag;

public abstract class Solution
{
    public abstract void Load();
    public abstract void Unload();
}

public enum SolutionEnum
{
    None,
    GetKeyboardState,
    RawInput,
    GetAsyncKeyState
}