using System.Reflection;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.LessCoreInputLag;

public unsafe class LessCoreInputLagModule : EverestModule
{
    public static LessCoreInputLagModule Instance { get; private set; }

    public override Type SettingsType => typeof(LessCoreInputLagSettings);
    public static LessCoreInputLagSettings Settings => (LessCoreInputLagSettings)Instance._Settings;

    public bool CorrectPlatform { get; private set; }

    private Solution solution;
    private Hook eveNativeHook;

    public override void Load()
    {
        Instance = this;
        CorrectPlatform = Everest.Flags.IsFNA &&
            Environment.OSVersion.Platform is PlatformID.Win32NT &&
            Environment.Version.Major == 7;
        if (CorrectPlatform)
        {
            MethodInfo method = typeof(EverestModuleAssemblyContext).GetMethod("LoadUnmanagedDll", BindingFlags.NonPublic | BindingFlags.Instance)!;
            eveNativeHook = new(method, HookLoadUnmanagedDll);

            SwitchToSolution(Settings.Solution);
        }
    }

    private delegate IntPtr orig_LoadUnmanagedDll(EverestModuleAssemblyContext self, string name);
    private IntPtr HookLoadUnmanagedDll(orig_LoadUnmanagedDll orig, EverestModuleAssemblyContext self, string name)
    {
        if (name == "user32.dll")
            return IntPtr.Zero;
        return orig(self, name);
    }

    public override void Unload()
    {
        if (CorrectPlatform)
        {
            solution?.Unload();
            eveNativeHook.Dispose();
        }
    }

    public void SwitchToSolution(SolutionEnum solutionEnum)
    {
        Logger.Log(LogLevel.Info, nameof(LessCoreInputLag), $"Using solution {solutionEnum}.");
        Solution s = Mapper.SolutionEnumToInstance(solutionEnum);
        solution?.Unload();
        s?.Load();
        solution = s;
    }
}