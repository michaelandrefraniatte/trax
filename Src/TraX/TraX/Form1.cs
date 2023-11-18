using Microsoft.Win32.SafeHandles;
using System;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using controller;
namespace TraX
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("WiimotePairing.dll", EntryPoint = "connect")]
        private static extern bool connect();
        [DllImport("WiimotePairing.dll", EntryPoint = "disconnect")]
        private static extern bool disconnect();
        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid gHid);
        [DllImport("hid.dll")]
        private extern static bool HidD_SetOutputReport(IntPtr HidDeviceObject, byte[] lpReportBuffer, uint ReportBufferLength);
        [DllImport("setupapi.dll")]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, UInt32 Flags);
        [DllImport("setupapi.dll")]
        private static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInvo, ref Guid interfaceClassGuid, Int32 memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);
        [DllImport("setupapi.dll")]
        private static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, UInt32 deviceInterfaceDetailDataSize, out UInt32 requiredSize, IntPtr deviceInfoData);
        [DllImport("setupapi.dll")]
        private static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, UInt32 deviceInterfaceDetailDataSize, out UInt32 requiredSize, IntPtr deviceInfoData);
        [DllImport("Kernel32.dll")]
        private static extern SafeFileHandle CreateFile(string fileName, [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess, [MarshalAs(UnmanagedType.U4)] FileShare fileShare, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, [MarshalAs(UnmanagedType.U4)] uint flags, IntPtr template);
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        private static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        [DllImport("User32.dll")]
        private static extern bool GetCursorPos(out int x, out int y);
        [DllImport("user32.dll")]
        public static extern void SetCursorPos(int X, int Y);
        private const double REGISTER_IR = 0x04b00030, REGISTER_EXTENSION_INIT_1 = 0x04a400f0, REGISTER_EXTENSION_INIT_2 = 0x04a400fb, REGISTER_EXTENSION_TYPE = 0x04a400fa, REGISTER_EXTENSION_CALIBRATION = 0x04a40020, REGISTER_MOTIONPLUS_INIT = 0x04a600fe;
        private static double irx0, iry0, irx1, iry1, irx, iry, irxc, iryc, mWSIRSensors0X, mWSIRSensors0Y, mWSIRSensors1X, mWSIRSensors1Y, mWSButtonStateIRX, mWSButtonStateIRY, mousex, mousey, mWSIRSensors0Xcam, mWSIRSensors0Ycam, mWSIRSensors1Xcam, mWSIRSensors1Ycam, mWSIRSensorsXcam, mWSIRSensorsYcam, viewpower05x, viewpower1x, viewpower2x = 10f, viewpower3x, viewpower05y, viewpower1y, viewpower2y = 10f, viewpower3y, dzx, dzy, lowsensx = 1f, lowsensy = 1f, centery = 160f, increasetrackirx = 1.0f, increasetrackiry = 1.0f;
        private static bool mWSIR1found, mWSIR0found, running, mWSButtonStateA, Getstate;
        private static byte[] buff = new byte[] { 0x55 }, mBuff = new byte[22], aBuffer = new byte[22];
        private const byte Type = 0x12, IR = 0x13, WriteMemory = 0x16, ReadMemory = 0x16, IRExtensionAccel = 0x37;
        private static uint CurrentResolution = 0;
        private static FileStream mStream;
        private static SafeFileHandle handle = null;
        private static ThreadStart threadstart;
        private static Thread thread;
        private const double dz = 10f;
        private static double mmousex, mmousey;
        private delegate bool ConsoleEventDelegate(int eventType);
        MouseHook mouseHook = new MouseHook();
        public static IntPtr Param;
        public static int MouseHookX, MouseHookY, MouseHookWheel, MouseDesktopHookX, MouseDesktopHookY;
        public static bool MouseHookLeftButton, MouseHookRightButton, MouseHookDoubleClick, MouseHookMiddleButton;
        private static ScpBus scpBus;
        private static X360Controller controller;
        public static int WidthI = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        public static int HeightI = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
        public static Form1 form = (Form1)Application.OpenForms["Form1"];
        private static System.Collections.Generic.List<double> valListX = new System.Collections.Generic.List<double>(), valListY = new System.Collections.Generic.List<double>();
        private static int[] wd = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        private static int[] wu = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        private static void valchanged(int n, bool val)
        {
            if (val)
            {
                if (wd[n] <= 1)
                {
                    wd[n] = wd[n] + 1;
                }
                wu[n] = 0;
            }
            else
            {
                if (wu[n] <= 1)
                {
                    wu[n] = wu[n] + 1;
                }
                wd[n] = 0;
            }
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            mouseHook.Hook += new MouseHook.MouseHookCallback(mouseHook_Hook);
            mouseHook.Install();
            Task.Run(() => Start());
        }
        private void Start()
        {
            scpBus = new ScpBus();
            scpBus.PlugIn(1);
            controller = new X360Controller();
            running = true;
            connectingWiimote();
            Task.Run(() => taskD());
            Thread.Sleep(100);
            Task.Run(() => taskX());
            this.WindowState = FormWindowState.Minimized;
        }
        private static void connectingWiimote()
        {
            do
                Thread.Sleep(1);
            while (!connect());
            do
                Thread.Sleep(1);
            while (!ScanWiimote());
        }
        private static void taskX()
        {
            while (running)
            {
                mWSButtonStateA = (aBuffer[2] & 0x08) != 0;
                valchanged(0, mWSButtonStateA);
                if (wd[0] == 1 & !Getstate)
                {
                    using (System.IO.StreamReader createdfile = new System.IO.StreamReader("TraX.txt"))
                    {
                        createdfile.ReadLine();
                        viewpower05x = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        viewpower1x = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        viewpower2x = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        viewpower3x = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        viewpower05y = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        viewpower1y = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        viewpower2y = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        viewpower3y = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        dzx = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        dzy = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        lowsensx = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        lowsensy = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        centery = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        increasetrackirx = Convert.ToDouble(createdfile.ReadLine());
                        createdfile.ReadLine();
                        increasetrackiry = Convert.ToDouble(createdfile.ReadLine());
                    }
                    Getstate = true;
                }
                else
                {
                    if (wd[0] == 1 & Getstate)
                    {
                        Getstate = false;
                        for (int i = 1; i <= 36; i++)
                        {
                            wd[i] = 2;
                            wu[i] = 2;
                            Thread.Sleep(1);
                        }
                        controller.Buttons &= ~X360Buttons.LeftBumper;
                        controller.Buttons &= ~X360Buttons.RightBumper;
                        controller.Buttons &= ~X360Buttons.Left;
                        controller.Buttons &= ~X360Buttons.X;
                        controller.Buttons &= ~X360Buttons.RightStick;
                        controller.Buttons &= ~X360Buttons.Start;
                        controller.RightStickX = 0;
                        controller.RightStickY = 0;
                        controller.LeftStickX = 0;
                        controller.LeftStickY = 0;
                        scpBus.Report(1, controller.GetReport());
                    }
                }
                if (Getstate)
                {
                    mWSIRSensors0X = aBuffer[6] | ((aBuffer[8] >> 4) & 0x03) << 8;
                    mWSIRSensors0Y = aBuffer[7] | ((aBuffer[8] >> 6) & 0x03) << 8;
                    mWSIRSensors1X = aBuffer[9] | ((aBuffer[8] >> 0) & 0x03) << 8;
                    mWSIRSensors1Y = aBuffer[10] | ((aBuffer[8] >> 2) & 0x03) << 8;
                    mWSIR0found = mWSIRSensors0X > 0f & mWSIRSensors0X <= 1024f & mWSIRSensors0Y > 0f & mWSIRSensors0Y <= 768f;
                    mWSIR1found = mWSIRSensors1X > 0f & mWSIRSensors1X <= 1024f & mWSIRSensors1Y > 0f & mWSIRSensors1Y <= 768f;
                    if (mWSIR0found)
                    {
                        mWSIRSensors0Xcam = mWSIRSensors0X - 512f;
                        mWSIRSensors0Ycam = mWSIRSensors0Y - 384f;
                    }
                    if (mWSIR1found)
                    {
                        mWSIRSensors1Xcam = mWSIRSensors1X - 512f;
                        mWSIRSensors1Ycam = mWSIRSensors1Y - 384f;
                    }
                    if (mWSIR0found & mWSIR1found)
                    {
                        mWSIRSensorsXcam = (mWSIRSensors0Xcam + mWSIRSensors1Xcam) / 2f;
                        mWSIRSensorsYcam = (mWSIRSensors0Ycam + mWSIRSensors1Ycam) / 2f;
                    }
                    if (mWSIR0found)
                    {
                        irx0 = 2 * mWSIRSensors0Xcam - mWSIRSensorsXcam;
                        iry0 = 2 * mWSIRSensors0Ycam - mWSIRSensorsYcam;
                    }
                    if (mWSIR1found)
                    {
                        irx1 = 2 * mWSIRSensors1Xcam - mWSIRSensorsXcam;
                        iry1 = 2 * mWSIRSensors1Ycam - mWSIRSensorsYcam;
                    }
                    irxc = irx0 + irx1;
                    iryc = iry0 + iry1;
                    mWSButtonStateIRX = irxc;
                    mWSButtonStateIRY = iryc * 2f;
                    irx = increasetrackirx * mWSButtonStateIRX * (1024f / 1360f);
                    iry = increasetrackiry * (mWSButtonStateIRY + centery >= 0 ? Scale(mWSButtonStateIRY + centery, 0f, 1360f + centery, 0f, 1024f) : Scale(mWSButtonStateIRY + centery, -1360f + centery, 0f, -1024f, 0f));
                    if (irx >= 1024f)
                        irx = 1024f;
                    if (irx <= -1024f)
                        irx = -1024f;
                    if (iry >= 1024f)
                        iry = 1024f;
                    if (iry <= -1024f)
                        iry = -1024f;
                    if (irx > 0f)
                        mousex = Scale(Math.Pow(irx, 3f) / Math.Pow(1024f, 2f) * viewpower3x + Math.Pow(irx, 2f) / Math.Pow(1024f, 1f) * viewpower2x + Math.Pow(irx, 1f) / Math.Pow(1024f, 0f) * viewpower1x + Math.Pow(irx, 0.5f) * Math.Pow(1024f, 0.5f) * viewpower05x, 0f, 1024f, (dzx / 100f) * 1024f, 1024f);
                    if (irx < 0f)
                        mousex = Scale(-Math.Pow(-irx, 3f) / Math.Pow(1024f, 2f) * viewpower3x - Math.Pow(-irx, 2f) / Math.Pow(1024f, 1f) * viewpower2x - Math.Pow(-irx, 1f) / Math.Pow(1024f, 0f) * viewpower1x - Math.Pow(-irx, 0.5f) * Math.Pow(1024f, 0.5f) * viewpower05x, -1024f, 0f, -1024f, -(dzx / 100f) * 1024f);
                    if (iry > 0f)
                        mousey = Scale(Math.Pow(iry, 3f) / Math.Pow(1024f, 2f) * viewpower3y + Math.Pow(iry, 2f) / Math.Pow(1024f, 1f) * viewpower2y + Math.Pow(iry, 1f) / Math.Pow(1024f, 0f) * viewpower1y + Math.Pow(iry, 0.5f) * Math.Pow(1024f, 0.5f) * viewpower05y, 0f, 1024f, (dzy / 100f) * 1024f, 1024f);
                    if (iry < 0f)
                        mousey = Scale(-Math.Pow(-iry, 3f) / Math.Pow(1024f, 2f) * viewpower3y - Math.Pow(-iry, 2f) / Math.Pow(1024f, 1f) * viewpower2y - Math.Pow(-iry, 1f) / Math.Pow(1024f, 0f) * viewpower1y - Math.Pow(-iry, 0.5f) * Math.Pow(1024f, 0.5f) * viewpower05y, -1024f, 0f, -1024f, -(dzy / 100f) * 1024f);
                    valchanged(3, GetAsyncKeyState(System.Windows.Forms.Keys.W));
                    if (wd[3] == 1)
                        controller.Buttons ^= X360Buttons.Left;
                    if (wu[3] == 1)
                        controller.Buttons &= ~X360Buttons.Left;
                    valchanged(2, GetAsyncKeyState(System.Windows.Forms.Keys.LShiftKey));
                    if (wd[2] == 1)
                        controller.Buttons ^= X360Buttons.LeftStick;
                    if (wu[2] == 1)
                        controller.Buttons &= ~X360Buttons.LeftStick;
                    valchanged(10, GetAsyncKeyState(System.Windows.Forms.Keys.Space));
                    if (wd[10] == 1)
                        controller.Buttons ^= X360Buttons.A;
                    if (wu[10] == 1)
                        controller.Buttons &= ~X360Buttons.A;
                    valchanged(4, GetAsyncKeyState(System.Windows.Forms.Keys.V));
                    if (wd[4] == 1)
                        controller.Buttons ^= X360Buttons.RightStick;
                    if (wu[4] == 1)
                        controller.Buttons &= ~X360Buttons.RightStick;
                    valchanged(13, GetAsyncKeyState(System.Windows.Forms.Keys.R));
                    if (wd[13] == 1)
                        controller.Buttons ^= X360Buttons.X;
                    if (wu[13] == 1)
                        controller.Buttons &= ~X360Buttons.X;
                    valchanged(20, GetAsyncKeyState(System.Windows.Forms.Keys.Tab));
                    if (wd[20] == 1)
                        controller.Buttons ^= X360Buttons.Back;
                    if (wu[20] == 1)
                        controller.Buttons &= ~X360Buttons.Back;
                    valchanged(1, GetAsyncKeyState(System.Windows.Forms.Keys.C));
                    if (wd[1] == 1)
                        controller.Buttons ^= X360Buttons.B;
                    if (wu[1] == 1)
                        controller.Buttons &= ~X360Buttons.B;
                    valchanged(22, MouseHookMiddleButton);
                    if (wd[22] == 1)
                        controller.Buttons ^= X360Buttons.X;
                    if (wu[22] == 1)
                        controller.Buttons &= ~X360Buttons.X;
                    valchanged(12, GetAsyncKeyState(System.Windows.Forms.Keys.A));
                    if (wd[12] == 1)
                        controller.Buttons ^= X360Buttons.Right;
                    if (wu[12] == 1)
                        controller.Buttons &= ~X360Buttons.Right;
                    valchanged(24, GetAsyncKeyState(System.Windows.Forms.Keys.E));
                    if (wd[24] == 1)
                        controller.Buttons ^= X360Buttons.Down;
                    if (wu[24] == 1)
                        controller.Buttons &= ~X360Buttons.Down;
                    valchanged(16, GetAsyncKeyState(System.Windows.Forms.Keys.F));
                    if (wd[16] == 1)
                        controller.Buttons ^= X360Buttons.Up;
                    if (wu[16] == 1)
                        controller.Buttons &= ~X360Buttons.Up;
                    valchanged(25, Math.Abs(MouseHookWheel) >= 100);
                    if (wd[25] == 1)
                        controller.Buttons ^= X360Buttons.Y;
                    if (wu[25] == 1)
                        controller.Buttons &= ~X360Buttons.Y;
                    valchanged(26, GetAsyncKeyState(System.Windows.Forms.Keys.Escape));
                    if (wd[26] == 1)
                        controller.Buttons ^= X360Buttons.Start;
                    if (wu[26] == 1)
                        controller.Buttons &= ~X360Buttons.Start;
                    valchanged(14, GetAsyncKeyState(System.Windows.Forms.Keys.G));
                    if (wd[14] == 1)
                        controller.Buttons ^= X360Buttons.RightBumper;
                    if (wu[14] == 1)
                        controller.Buttons &= ~X360Buttons.RightBumper;
                    valchanged(15, GetAsyncKeyState(System.Windows.Forms.Keys.T));
                    if (wd[15] == 1)
                        controller.Buttons ^= X360Buttons.LeftBumper;
                    if (wu[15] == 1)
                        controller.Buttons &= ~X360Buttons.LeftBumper;
                    valchanged(9, MouseHookRightButton);
                    if (wd[9] == 1)
                        controller.LeftTrigger = 255;
                    if (wu[9] == 1)
                        controller.LeftTrigger = 0;
                    valchanged(11, MouseHookLeftButton);
                    if (wd[11] == 1)
                        controller.RightTrigger = 255;
                    if (wu[11] == 1)
                        controller.RightTrigger = 0;
                    valchanged(5, GetAsyncKeyState(System.Windows.Forms.Keys.D));
                    if (wd[5] == 1)
                        controller.LeftStickX = 32767;
                    if (wu[5] == 1)
                        controller.LeftStickX = 0;
                    valchanged(6, GetAsyncKeyState(System.Windows.Forms.Keys.Q));
                    if (wd[6] == 1)
                        controller.LeftStickX = -32767;
                    if (wu[6] == 1)
                        controller.LeftStickX = 0;
                    valchanged(7, GetAsyncKeyState(System.Windows.Forms.Keys.Z));
                    if (wd[7] == 1)
                        controller.LeftStickY = 32767;
                    if (wu[7] == 1)
                        controller.LeftStickY = 0;
                    valchanged(8, GetAsyncKeyState(System.Windows.Forms.Keys.S));
                    if (wd[8] == 1)
                        controller.LeftStickY = -32767;
                    if (wu[8] == 1)
                        controller.LeftStickY = 0;
                    irx = -Scale(MouseHookX - WidthI / 2f, -WidthI / 2f, WidthI / 2f, -1024f, 1024f);
                    iry = Scale(MouseHookY - HeightI / 2f, -HeightI / 2f, HeightI / 2f, -1024f, 1024f);
                    if (irx >= 1024f)
                        irx = 1024f;
                    if (irx <= -1024f)
                        irx = -1024f;
                    if (iry >= 1024f)
                        iry = 1024f;
                    if (iry <= -1024f)
                        iry = -1024f;
                    if (irx > 0f)
                        mmousex = Scale((Math.Pow(irx, 2f) / Math.Pow(1024f, 1f) + Math.Pow(irx, 1f) / Math.Pow(1024f, 0f)) / 2f, 0f, 1024f, (dz / 100f) * 1024f, 1024f);
                    if (irx < 0f)
                        mmousex = Scale(-(Math.Pow(-irx, 2f) / Math.Pow(1024f, 1f) + Math.Pow(-irx, 1f) / Math.Pow(1024f, 0f)) / 2f, -1024f, 0f, -1024f, -(dz / 100f) * 1024f);
                    if (iry > 0f)
                        mmousey = Scale((Math.Pow(iry, 2f) / Math.Pow(1024f, 1f) + Math.Pow(iry, 1f) / Math.Pow(1024f, 0f)) / 2f, 0f, 1024f, (dz / 100f) * 1024f, 1024f);
                    if (iry < 0f)
                        mmousey = Scale(-(Math.Pow(-iry, 2f) / Math.Pow(1024f, 1f) + Math.Pow(-iry, 1f) / Math.Pow(1024f, 0f)) / 2f, -1024f, 0f, -1024f, -(dz / 100f) * 1024f);
                    controller.RightStickX = (short)(Math.Abs(-mmousex * 32767 / 1024f) <= 32767 ? -mmousex * 32767 / 1024f : Math.Sign(-mmousex) * 32767);
                    controller.RightStickY = (short)(Math.Abs(-mmousey * 32767 / 1024f) <= 32767 ? -mmousey * 32767 / 1024f : Math.Sign(-mmousey) * 32767);
                    controller.LeftStickX = (short)(Math.Abs(-mousex * 32767f / lowsensx / 1024f) <= 32767f / lowsensx ? -mousex * 32767f / lowsensx / 1024f : Math.Sign(-mousex) * 32767f / lowsensx);
                    controller.LeftStickY = (short)(Math.Abs(-mousey * 32767f / lowsensy / 1024f) <= 32767f / lowsensy ? -mousey * 32767f / lowsensy / 1024f : Math.Sign(-mousey) * 32767f / lowsensy);
                    if (mousey < 0f)
                    {
                        controller.LeftTrigger = (byte)Math.Abs(mousey * 255f / lowsensy / 1024f);
                        controller.RightTrigger = 0;
                    }
                    if (mousey > 0f)
                    {
                        controller.RightTrigger = (byte)Math.Abs(mousey * 255f / lowsensy / 1024f);
                        controller.LeftTrigger = 0;
                    }
                    scpBus.Report(1, controller.GetReport());
                }
                MouseHookWheel = 0;
                Thread.Sleep(1);
            }
        }
        private void mouseHook_Hook(MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            MouseHookX = mouseStruct.pt.x;
            MouseHookY = mouseStruct.pt.y;
            if (MouseHook.MouseMessages.WM_RBUTTONDOWN == (MouseHook.MouseMessages)Param)
                MouseHookRightButton = true;
            if (MouseHook.MouseMessages.WM_RBUTTONUP == (MouseHook.MouseMessages)Param)
                MouseHookRightButton = false;
            if (MouseHook.MouseMessages.WM_LBUTTONDOWN == (MouseHook.MouseMessages)Param)
                MouseHookLeftButton = true;
            if (MouseHook.MouseMessages.WM_LBUTTONUP == (MouseHook.MouseMessages)Param)
                MouseHookLeftButton = false;
            if (MouseHook.MouseMessages.WM_MBUTTONDOWN == (MouseHook.MouseMessages)Param)
                MouseHookMiddleButton = true;
            if (MouseHook.MouseMessages.WM_MBUTTONUP == (MouseHook.MouseMessages)Param)
                MouseHookMiddleButton = false;
            if (MouseHook.MouseMessages.WM_LBUTTONDBLCLK == (MouseHook.MouseMessages)Param)
                MouseHookDoubleClick = true;
            else
                MouseHookDoubleClick = false;
            if (MouseHook.MouseMessages.WM_MOUSEWHEEL == (MouseHook.MouseMessages)Param)
            {
                MouseHookWheel = (int)mouseStruct.mouseData;
            }
            else
            {
                MouseHookWheel = 0;
            }
        }
        private static double Scale(double value, double min, double max, double minScale, double maxScale)
        {
            double scaled = minScale + (double)(value - min) / (max - min) * (maxScale - minScale);
            return scaled;
        }
        private static void taskD()
        {
            while (running)
            {
                try
                {
                    mStream.Read(aBuffer, 0, 22);
                }
                catch { }
            }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            running = false;
            Thread.Sleep(100);
            scpBus.Unplug(1);
            Thread.Sleep(100);
            mouseHook.Hook -= new MouseHook.MouseHookCallback(mouseHook_Hook);
            mouseHook.Uninstall();
            Thread.Sleep(100);
            threadstart = new ThreadStart(FormClose);
            thread = new Thread(threadstart);
            thread.Start();
        }
        private static void FormClose()
        {
            try
            {
                mStream.Close();
                handle.Close();
                disconnect();
            }
            catch { }
        }
        private const string vendor_id = "57e", vendor_id_ = "057e", product_r1 = "0330", product_r2 = "0306", product_l = "2006";
        private enum EFileAttributes : uint
        {
            Overlapped = 0x40000000,
            Normal = 0x80
        };
        struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr RESERVED;
        }
        struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public UInt32 cbSize;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }
        private static bool ScanWiimote()
        {
            int index = 0;
            Guid guid;
            HidD_GetHidGuid(out guid);
            IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, null, new IntPtr(), 0x00000010);
            SP_DEVICE_INTERFACE_DATA diData = new SP_DEVICE_INTERFACE_DATA();
            diData.cbSize = Marshal.SizeOf(diData);
            while (SetupDiEnumDeviceInterfaces(hDevInfo, new IntPtr(), ref guid, index, ref diData))
            {
                UInt32 size;
                SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, new IntPtr(), 0, out size, new IntPtr());
                SP_DEVICE_INTERFACE_DETAIL_DATA diDetail = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                diDetail.cbSize = 5;
                if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, ref diDetail, size, out size, new IntPtr()))
                {
                    if ((diDetail.DevicePath.Contains(vendor_id) | diDetail.DevicePath.Contains(vendor_id_)) & (diDetail.DevicePath.Contains(product_r1) | diDetail.DevicePath.Contains(product_r2)))
                    {
                        WiimoteFound(diDetail.DevicePath);
                        WiimoteFound(diDetail.DevicePath);
                        WiimoteFound(diDetail.DevicePath);
                        return true;
                    }
                }
                index++;
            }
            return false;
        }
        private static void WiimoteFound(string path)
        {
            do
            {
                handle = CreateFile(path, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, (uint)EFileAttributes.Overlapped, IntPtr.Zero);
                WriteData(handle, IR, (int)REGISTER_IR, new byte[] { 0x08 }, 1);
                WriteData(handle, Type, (int)REGISTER_EXTENSION_INIT_1, new byte[] { 0x55 }, 1);
                WriteData(handle, Type, (int)REGISTER_EXTENSION_INIT_2, new byte[] { 0x00 }, 1);
                WriteData(handle, Type, (int)REGISTER_MOTIONPLUS_INIT, new byte[] { 0x04 }, 1);
                ReadData(handle, 0x0016, 7);
                ReadData(handle, (int)REGISTER_EXTENSION_TYPE, 6);
                ReadData(handle, (int)REGISTER_EXTENSION_CALIBRATION, 16);
                ReadData(handle, (int)REGISTER_EXTENSION_CALIBRATION, 32);
            }
            while (handle.IsInvalid);
            mStream = new FileStream(handle, FileAccess.ReadWrite, 22, true);
        }
        private static void ReadData(SafeFileHandle _hFile, int address, short size)
        {
            mBuff[0] = (byte)ReadMemory;
            mBuff[1] = (byte)((address & 0xff000000) >> 24);
            mBuff[2] = (byte)((address & 0x00ff0000) >> 16);
            mBuff[3] = (byte)((address & 0x0000ff00) >> 8);
            mBuff[4] = (byte)(address & 0x000000ff);
            mBuff[5] = (byte)((size & 0xff00) >> 8);
            mBuff[6] = (byte)(size & 0xff);
            HidD_SetOutputReport(_hFile.DangerousGetHandle(), mBuff, 22);
        }
        private static void WriteData(SafeFileHandle _hFile, byte mbuff, int address, byte[] buff, short size)
        {
            mBuff[0] = (byte)mbuff;
            mBuff[1] = (byte)(0x04);
            mBuff[2] = (byte)IRExtensionAccel;
            Array.Copy(buff, 0, mBuff, 3, 1);
            HidD_SetOutputReport(_hFile.DangerousGetHandle(), mBuff, 22);
            mBuff[0] = (byte)WriteMemory;
            mBuff[1] = (byte)(((address & 0xff000000) >> 24));
            mBuff[2] = (byte)((address & 0x00ff0000) >> 16);
            mBuff[3] = (byte)((address & 0x0000ff00) >> 8);
            mBuff[4] = (byte)((address & 0x000000ff) >> 0);
            mBuff[5] = (byte)size;
            Array.Copy(buff, 0, mBuff, 6, 1);
            HidD_SetOutputReport(_hFile.DangerousGetHandle(), mBuff, 22);
        }
    }
    class MouseHook
    {
        private delegate IntPtr MouseHookHandler(int nCode, IntPtr wParam, IntPtr lParam);
        private MouseHookHandler hookHandler;
        public delegate void MouseHookCallback(MSLLHOOKSTRUCT mouseStruct);
        public event MouseHookCallback LeftButtonDown;
        public event MouseHookCallback LeftButtonUp;
        public event MouseHookCallback RightButtonDown;
        public event MouseHookCallback RightButtonUp;
        public event MouseHookCallback MouseMove;
        public event MouseHookCallback MouseWheel;
        public event MouseHookCallback DoubleClick;
        public event MouseHookCallback MiddleButtonDown;
        public event MouseHookCallback MiddleButtonUp;
        public event MouseHookCallback Hook;
        private IntPtr hookID = IntPtr.Zero;
        public void Install()
        {
            hookHandler = HookFunc;
            hookID = SetHook(hookHandler);
        }
        public void Uninstall()
        {
            if (hookID == IntPtr.Zero)
                return;
            UnhookWindowsHookEx(hookID);
            hookID = IntPtr.Zero;
        }
        ~MouseHook()
        {
            Uninstall();
        }
        private IntPtr SetHook(MouseHookHandler proc)
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(module.ModuleName), 0);
        }
        private IntPtr HookFunc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Form1.Param = wParam;
                if (MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
                    if (LeftButtonDown != null)
                        LeftButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_LBUTTONUP == (MouseMessages)wParam)
                    if (LeftButtonUp != null)
                        LeftButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_RBUTTONDOWN == (MouseMessages)wParam)
                    if (RightButtonDown != null)
                        RightButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_RBUTTONUP == (MouseMessages)wParam)
                    if (RightButtonUp != null)
                        RightButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
                    if (MouseMove != null)
                        MouseMove((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MOUSEWHEEL == (MouseMessages)wParam)
                    if (MouseWheel != null)
                        MouseWheel((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_LBUTTONDBLCLK == (MouseMessages)wParam)
                    if (DoubleClick != null)
                        DoubleClick((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MBUTTONDOWN == (MouseMessages)wParam)
                    if (MiddleButtonDown != null)
                        MiddleButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MBUTTONUP == (MouseMessages)wParam)
                    if (MiddleButtonUp != null)
                        MiddleButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                Hook((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }
        private const int WH_MOUSE_LL = 14;
        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, MouseHookHandler lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}