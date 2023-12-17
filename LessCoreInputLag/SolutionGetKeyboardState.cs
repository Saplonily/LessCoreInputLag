using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.LessCoreInputLag;

public unsafe class SolutionGetKeyboardState : Solution
{
    private byte* keyStates;
    private Hook hookPullEvent;

    [DllImport("user32.dll")]
    private static extern int GetKeyboardState(byte* lpKeyState);

    public override void Load()
    {
        keyStates = (byte*)NativeMemory.AllocZeroed(256);
        Type fnaPlatformType = typeof(Vector2).Assembly.GetType("Microsoft.Xna.Framework.SDL2_FNAPlatform");
        hookPullEvent = new(fnaPlatformType.GetMethod("PollEvents"), HookPollEvents);
        On.Monocle.MInput.KeyboardData.Update += KeyboardData_Update;
    }

    private void KeyboardData_Update(On.Monocle.MInput.KeyboardData.orig_Update orig, MInput.KeyboardData self)
    {
        self.PreviousState = self.CurrentState;
        KeyboardState ks = new();
        {
            for (int i = 0; i < 256; i++)
            {
                if (keyStates[i] > 1)
                {
                    var k = Mapper.VKKeysToXNAKeys((ushort)i);
                    if (k is not null)
                    {
                        SetState(&ks, (int)k.Value);
                    }
                }
            }
        }
        self.CurrentState = ks;

        static void SetState(KeyboardState* ks, int key)
        {
            uint mask = 1U << key;
            uint* ptr = (uint*)ks;
            ptr += key >> 5;
            *ptr |= mask;
        }
    }
    public delegate void orig_PollEvents(Game game, ref GraphicsAdapter ga, bool[] ticd, ref bool tis);
    public void HookPollEvents(orig_PollEvents orig, Game game, ref GraphicsAdapter ga, bool[] ticd, ref bool tis)
    {
        orig(game, ref ga, ticd, ref tis);
        GetKeyboardState(keyStates);
    }

    public override void Unload()
    {
        hookPullEvent.Dispose();
        On.Monocle.MInput.KeyboardData.Update -= KeyboardData_Update;
    }
}