#pragma warning disable CA2014
#pragma warning disable CS0649

using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.LessCoreInputLag;

public unsafe class SolutionRawInput : Solution
{
    private List<Keys> keys = new();
    private Hook hookPullEvent;

    public override void Load()
    {
        Type fnaPlatformType = typeof(Vector2).Assembly.GetType("Microsoft.Xna.Framework.SDL2_FNAPlatform");
        hookPullEvent = new(fnaPlatformType.GetMethod("PollEvents"), modPollEvents);
        InitRawInputDevices();
        On.Monocle.MInput.KeyboardData.Update += KeyboardData_Update;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }
    private struct RAWINPUT
    {
        public RAWINPUTHEADER header;
        public RAWKEYBOARD keyboard;
    }
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public nint hDevice;
        public nint wParam;
    }

    public delegate void orig_PollEvents(Game game, ref GraphicsAdapter ga, bool[] ticd, ref bool tis);
    public void modPollEvents(orig_PollEvents orig, Game game, ref GraphicsAdapter ga, bool[] ticd, ref bool tis)
    {
        if (Engine.Instance.IsActive)
        {
            MSG msg = new();
            while (PeekMessageW(&msg, 0, 0xff, 0xff, 1) != 0)
            {
                uint dwSize;
                GetRawInputData(msg.lParam, 0x10000003, null, &dwSize, (uint)sizeof(RAWINPUTHEADER));
                byte* lpb = stackalloc byte[(int)dwSize];

                if (GetRawInputData(msg.lParam, 0x10000003, lpb, &dwSize, (uint)sizeof(RAWINPUTHEADER)) != dwSize)
                    Log("GetRawInputData does not return correct size !\n");

                RAWINPUT* raw = (RAWINPUT*)lpb;

                if (raw->header.dwType == 1)
                {
                    RAWKEYBOARD* k = &raw->keyboard;
                    bool isPressed = (k->Flags & 1) == 0;
                    Keys? key = Mapper.VKKeysToXNAKeys(k->VKey);
                    if (key.HasValue)
                    {
                        if (isPressed && !keys.Contains(key.Value))
                            keys.Add(key.Value);
                        if (!isPressed)
                            keys.Remove(key.Value);
                    }
                }
            }
            orig(game, ref ga, ticd, ref tis);
            return;
        }
        orig(game, ref ga, ticd, ref tis);
        return;

        [DllImport("user32.dll")]
        static extern int PeekMessageW(MSG* msg, nint hwnd, uint msgFilterMin, uint msgFilterMax, uint removeMsg);

        [DllImport("user32.dll")]
        static extern uint GetRawInputData(nint hRawInput, uint uiCommand, byte* pData, uint* pcbSize, uint cbSizeHeader);
    }

    private void KeyboardData_Update(On.Monocle.MInput.KeyboardData.orig_Update orig, MInput.KeyboardData self)
    {
        self.PreviousState = self.CurrentState;
        KeyboardState ks = new();
        foreach (var item in keys)
            SetState(&ks, (int)item);
        self.CurrentState = ks;

        static void SetState(KeyboardState* ks, int key)
        {
            uint mask = 1U << key;
            uint* ptr = (uint*)ks;
            ptr += key >> 5;
            *ptr |= mask;
        }
    }

    public override void Unload()
    {
        hookPullEvent.Dispose();
        On.Monocle.MInput.KeyboardData.Update -= KeyboardData_Update;
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public nint hwndTarget;
    }
    private bool InitRawInputDevices()
    {
        RAWINPUTDEVICE rid = new();
        rid.usUsagePage = 0x01;
        rid.usUsage = 0x06;
        rid.dwFlags = 0x00;
        rid.hwndTarget = 0;

        int r = RegisterRawInputDevices(&rid, 1, 2 + 2 + 4 + 8);
        if (r == 0)
        {
            Log("Failed to initialize RawInputDevices.");
            return false;
        }

        return true;
        [DllImport("user32.dll")]
        static extern int RegisterRawInputDevices(RAWINPUTDEVICE* rid, uint numDevices, uint cbSize);
    }

    private void Log(string msg)
        => Logger.Log(LogLevel.Info, nameof(LessCoreInputLag), msg);
}
