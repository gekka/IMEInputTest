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


namespace Gekka.IMEInputTest
{
    using System;
    using System.Windows.Forms;
    using System.Threading.Tasks;

    static class TestClass
    {
        /// <summary>
        /// ランダムに"A"と"+"のキー入力をエミュレートする
        /// </summary>
        public static async Task InputTestAsync(Form frm, Control label, System.Threading.CancellationToken token)
        {
            //nugetでCSWin32をいれておく
            //NativeMethods.txtにはSendInputとMapVirtualKeyとImmGetVirtualKey

            try
            {
                Random rnd = new Random();

                await Task.Delay(1000, token);

                // 入力テストを１万回
                for (int i = 1; i <= 10000; i++)
                {
                    using (var cts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        cts.CancelAfter(TimeSpan.FromSeconds(10));
                        await NewMethod(frm, label, rnd, i, cts.Token);
                    }

                }

            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static async Task NewMethod(Form frm, Control label, Random rnd, int i, System.Threading.CancellationToken token)
        {
            IntPtr hwndForm = frm.Handle;

            await Task.Delay(rnd.Next(100, 500), token);

            Logger.WriteLog("!!ACTIVATE");
            frm.Activate();
            if (frm.ActiveControl is TextBox txb && txb.TextLength > 10)
            {
                txb.Clear();
            }
            if (frm.ActiveControl is ComboBox combo)
            {
                combo.DroppedDown = (rnd.Next() & 1) == 1;
            }

            frm.Text = i.ToString();
            label.Text = i.ToString();


            var targetControl = frm.ActiveControl;
            var hwnd = (Windows.Win32.Foundation.HWND)frm.ActiveControl.Handle;

            void checkActiveControl(Control targetControl)
            {
                if (targetControl != frm.ActiveControl)
                {
                    //MessageBox.Show("タイミング");

                    return;
                }
            }

            Logger.WriteLog(">A↓↑");
            await SendDownUpAsync(hwndForm, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY.VK_A, token);
            Logger.WriteLog(">A↓↑#");

            Logger.WriteLog(">DELAY1 " + DateTime.Now.ToString("HH:mm:ss.fff"));
            await Task.Delay(rnd.Next(10, 100), token);
            Logger.WriteLog(">DELAY1#");
            checkActiveControl(targetControl);

            var context = Windows.Win32.PInvoke.ImmGetContext(hwnd);
            if (!context.IsNull)
            {
                Windows.Win32.PInvoke.ImmReleaseContext(hwnd, context);
                for (int x = rnd.Next(0, 5); x >= 1; x--)
                {
                    Logger.WriteLog(">CONV");
                    await SendDownUpAsync(hwndForm, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY.VK_CONVERT, token);
                    Logger.WriteLog(">CONV#");
                }

                Logger.WriteLog(">DELAY2 " + DateTime.Now.ToString("HH:mm:ss.fff"));
                await Task.Delay(rnd.Next(10, 100), token);
                Logger.WriteLog(">DELAY2# ");
                checkActiveControl(targetControl);
            }


            bool isShift = (rnd.Next() & 0x03) == 0;
            if (isShift)
            {
                Logger.WriteLog(">SHIFT↓");
                await SendAsync(hwndForm, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY.VK_SHIFT, false, token);
                Logger.WriteLog(">SHIFT↓#");
                checkActiveControl(targetControl);
            }

            Logger.WriteLog(">ADD↓");
            await SendAsync(hwndForm, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY.VK_ADD, false, token);
            Logger.WriteLog(">ADD↓#");
            checkActiveControl(targetControl);

            Logger.WriteLog(">DELAY3 " + DateTime.Now.ToString("HH:mm:ss.fff"));
            await Task.Delay(rnd.Next(10, 100), token);
            Logger.WriteLog(">DELAY3#");

            Logger.WriteLog(">ADD↑");
            await SendAsync(hwndForm, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY.VK_ADD, true, token);
            Logger.WriteLog(">ADD↑#");
            if (isShift)
            {
                Logger.WriteLog(">SHIFT↑");
                await SendAsync(hwndForm, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY.VK_SHIFT, true, token);
            }
        }


        private static readonly int sizeInput = System.Runtime.InteropServices.Marshal.SizeOf<Windows.Win32.UI.Input.KeyboardAndMouse.INPUT>();

        private static async Task<uint> SendDownUpAsync(IntPtr hwnd, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY vk, System.Threading.CancellationToken token)
        {
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                var inputs = new Windows.Win32.UI.Input.KeyboardAndMouse.INPUT[2];
                inputs[0] = new Windows.Win32.UI.Input.KeyboardAndMouse.INPUT();
                inputs[0].type = Windows.Win32.UI.Input.KeyboardAndMouse.INPUT_TYPE.INPUT_KEYBOARD;
                inputs[0].Anonymous.ki.wVk = vk;
                inputs[0].Anonymous.ki.wScan = (ushort)Windows.Win32.PInvoke.MapVirtualKey((uint)vk, Windows.Win32.UI.Input.KeyboardAndMouse.MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
                inputs[0].Anonymous.ki.dwFlags = Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_SCANCODE;

                inputs[1] = inputs[0];
                inputs[1].Anonymous.ki.dwFlags |= Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;

                return Send(hwnd, inputs);
            });
        }

        private static async Task<uint> SendAsync(IntPtr hwnd, Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY vk, bool isUp, System.Threading.CancellationToken token)
        {
            return await Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return 0u;
                }


                var inputs = new Windows.Win32.UI.Input.KeyboardAndMouse.INPUT[1];
                inputs[0] = new Windows.Win32.UI.Input.KeyboardAndMouse.INPUT();
                inputs[0].type = Windows.Win32.UI.Input.KeyboardAndMouse.INPUT_TYPE.INPUT_KEYBOARD;
                inputs[0].Anonymous.ki.wVk = vk;
                inputs[0].Anonymous.ki.wScan = (ushort)Windows.Win32.PInvoke.MapVirtualKey((uint)vk, Windows.Win32.UI.Input.KeyboardAndMouse.MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
                if (isUp)
                {
                    inputs[0].Anonymous.ki.dwFlags = Windows.Win32.UI.Input.KeyboardAndMouse.KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;
                }
                return Send(hwnd, inputs);
            });
        }

        private static unsafe uint Send(IntPtr hwnd, Windows.Win32.UI.Input.KeyboardAndMouse.INPUT[] inputs)
        {
            int retry = 10;
            while (retry-- > 0 && hwnd != Windows.Win32.PInvoke.GetForegroundWindow())
            {
                Windows.Win32.PInvoke.SetForegroundWindow((Windows.Win32.Foundation.HWND)hwnd);
                System.Threading.Thread.Sleep(10);
            }

            if (retry <= 0)
            {//フォーカス切り替え失敗
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Break();
                }
                else
                {
                    MessageBox.Show("フォーカス切り替えできなかったのでSendInputが失敗しました");
                }
                return 0;
            }


            fixed (void* p = inputs)
            {
                Windows.Win32.UI.Input.KeyboardAndMouse.INPUT* pInputs = (Windows.Win32.UI.Input.KeyboardAndMouse.INPUT*)p;
                var ret = Windows.Win32.PInvoke.SendInput((uint)inputs.Length, pInputs, sizeInput);
                if (ret == 0)
                {
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                    else
                    {
                        MessageBox.Show("SendInputが失敗しました");
                    }
                }
                return ret;
            }
        }

    }
}