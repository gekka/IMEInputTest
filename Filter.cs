// MIT License
// 
// Copyright (c) 2025 gekka
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE

#nullable enable

namespace Gekka.IMEInputTest
{
    //nugetでCSWin32をいれておく

    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    sealed class Filter : System.Windows.Forms.IMessageFilter
    {
        public static Filter? Singleton => _singleton;
        private static Filter? _singleton;

        /// <summary>メッセージフィルターにこのフィルターを登録するr</summary>
        public static void Add()
        {
            lock (lockobject)
            {
                if (_singleton != null)
                {
                    return;
                }
                _singleton = new Filter();
                System.Windows.Forms.Application.AddMessageFilter(_singleton);
            }
        }

        /// <summary>メッセージフィルターからこのフィルターを登録解除する</summary>
        public static void Remove()
        {
            lock (lockobject)
            {
                if (_singleton == null)
                {
                    return;
                }
                System.Windows.Forms.Application.RemoveMessageFilter(_singleton);
                _singleton = null;
            }
        }

        #region

        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;

        private static object lockobject = new object();

        private static Keys GetKey(ref Message msg)
        {
            var vk = (Keys)msg.WParam.ToInt32();
            if (vk == Keys.ProcessKey)
            {
                vk = (Keys)Windows.Win32.PInvoke.ImmGetVirtualKey((Windows.Win32.Foundation.HWND)msg.HWnd);
            }
            return vk;
        }

        private static void CommitIME(IntPtr hwnd) => CommitIME((Windows.Win32.Foundation.HWND)hwnd);

        private static unsafe void CommitIME(Windows.Win32.Foundation.HWND hwnd)
        {
            var hIMC = Windows.Win32.PInvoke.ImmGetContext(hwnd);
            if (hIMC.IsNull)
            {
                Logger.WriteLog(">IME context not open");
                return;
            }

            try
            {
                if (Windows.Win32.PInvoke.ImmGetOpenStatus(hIMC) == 0)
                {
                    Windows.Win32.PInvoke.ImmReleaseContext(hwnd, hIMC);
                    return;
                }

                Logger.WriteLog(">IME context open");

                //int len = Windows.Win32.PInvoke.ImmGetCompositionString(hIMC, Windows.Win32.UI.Input.Ime.IME_COMPOSITION_STRING.GCS_COMPSTR, null, 0);
                //var bs = new byte[len];
                //fixed (byte* pb = bs)
                //{
                //    len = Windows.Win32.PInvoke.ImmGetCompositionString(hIMC, Windows.Win32.UI.Input.Ime.IME_COMPOSITION_STRING.GCS_COMPSTR, pb, (uint)bs.Length);
                //    var text = System.Text.Encoding.Unicode.GetString(bs);
                //    Logger.WriteLog(">IME " + text);
                //}

                //現在のIMEの状態で確定
                var ret = Windows.Win32.PInvoke.ImmNotifyIME(hIMC, Windows.Win32.UI.Input.Ime.NOTIFY_IME_ACTION.NI_COMPOSITIONSTR, Windows.Win32.UI.Input.Ime.NOTIFY_IME_INDEX.CPS_COMPLETE, 0);
                if (ret == false)
                {
                    var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

                    Logger.WriteLog("IME確定失敗" + err);
                }
            }
            finally
            {
                var ret = Windows.Win32.PInvoke.ImmReleaseContext(hwnd, hIMC);
                if (ret == false)
                {
                    var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

                    Logger.WriteLog("IME context close 失敗" + err);
                }
                Logger.WriteLog(">IME context close");
            }
        }

        public bool PreFilterMessage(ref Message msg)
        {
            var wm = (WM)msg.Msg;
            switch (wm)
            {
            case WM.WM_ACTIVATE:
                break;
            case WM.WM_SETFOCUS:
                var c = Control.FromChildHandle(msg.HWnd);
                if (c == null)
                {
                    this.FocusTarget = null;
                }
                else
                {
                    this.FocusTarget = new ActiveTarget() { HWnd = msg.HWnd, Type = c.GetType() };
                }
                break;
            case WM.WM_KILLFOCUS:
                this.FocusTarget = null;
                break;
            default:
                break;
            }

            bool isDown = msg.Msg == (int)WM_KEYDOWN;
            bool isUP = msg.Msg == (int)WM_KEYUP;
            if (!isDown && !isUP)
            {
                return false;
            }

            var vk = GetKey(ref msg);
            if (vk != Keys.Add)
            {
                return false;
            }


            Control target = System.Windows.Forms.Control.FromChildHandle(msg.HWnd);
            Form? frm = target?.FindForm();
            if (frm == null || target == null)
            {
                return false;
            }

            Logger.WriteLog(">+" + (isDown ? "↓" : "↑"));
            if (isDown)
            {
                //押された時点でキャンセル
                CommitIME(msg.HWnd);
            }
            else
            {
                //キーを押した状態でフォーカス移動させた後にKeyupの場合があるよ
                CommitIME(msg.HWnd);

                var isShift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                MoveNext(frm, target, isShift);
            }
            return true;
        }

        private static void MoveNextAsync(Form frm, Control target, bool isShift)
        {
            // 新MS-IMEだとデッドロックするよ…
            // 新MS-IMEがメインスレッドを使った後に開放していないっぽいよ
            // Task使ってもBeginInvokeでもメインスレッドがロックされてるのでやっぱりデッドロックするよ
            frm.BeginInvoke(new Action(async () =>
            {
                await Task.Delay(10);
                MoveNext(frm, target, isShift);
            }));
        }

        private static void MoveNext(Form frm, Control target, bool isShift)
        {
            try
            {
                if (target.Parent is ContainerControl cc)
                {
                    Logger.WriteLog(">MOVE" + (isShift ? "←" : "→"));
                    cc.SelectNextControl(target, !isShift, true, true, true);
                    Logger.WriteLog(">MOVED");
                }
            }
            catch
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public ActiveTarget? FocusTarget = null;
        public ActiveTarget? ActiveTarget = null;

        #endregion
    }

    struct ActiveTarget
    {
        public IntPtr HWnd;
        public Type Type;
    }

    enum WM : ushort
    {
        WM_NULL = 0,
        WM_CREATE = 0x0001,
        WM_DESTROY = 0x0002,
        WM_MOVE = 0x0003,

        WM_SIZE = 0x0005,
        WM_ACTIVATE = 0x0006,
        WM_SETFOCUS = 0x0007,
        WM_KILLFOCUS = 0x0008,

        WM_ENABLE = 0x000A,
        WM_SETREDRAW = 0x000B,
        WM_SETTEXT = 0x000C,
        WM_GETTEXT = 0x000D,
        WM_GETTEXTLENGTH = 0x000E,
        WM_PAINT = 0x000F,
        WM_CLOSE = 0x0010,
        WM_QUERYENDSESSION = 0x0011,
        WM_QUIT = 0x0012,

        WM_KEYFIRST = 0x0100,
        WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101,
        WM_CHAR = 0x0102,
    }

}
