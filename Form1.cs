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
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;

    public partial class Form1 : Form
    {
        public Form1()
        {
            this.ClientSize = new Size(500, 200);

            Label lbl = new Label() { AutoSize = true };
            var path = System.IO.Path.Combine(Environment.SystemDirectory, "TextInputFramework.dll");
            if (System.IO.File.Exists(path))
            {
                lbl.Text = "TextInputFramework.dll ver=" + System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
            }

            this.Controls.Add(lbl);

            System.Random rnd = new Random();
            int w = 100;
            int h = 30;
            int x = 0;
            int y = lbl.Height + 5;

            while (y < this.ClientSize.Height - h)
            {
                x = 5;
                while (x < this.ClientSize.Width - w)
                {
                    Control ctl;
                    switch ((rnd.Next() & 0x07))
                    {
                    case 0:
                        ctl = new Button() { Text = "ぼたん" };
                        break;
                    //case 1:
                    //    var combo = new ComboBox() { Text = "こんぼ", DropDownStyle = ComboBoxStyle.DropDown };
                    //    combo.Items.Add("a");
                    //    combo.Items.Add("あ");
                    //    combo.Items.Add("亜");

                    //    ctl = combo;
                    //    break;
                    case 2:
                        ctl = new RichTextBox() { Text = "りっち", Height = 25 };
                        break;
                    case 7:
                        ctl = new TextBox() { Text = "読み取り専用", ImeMode = ImeMode.Hiragana, ReadOnly = true, ForeColor = Color.Red, BackColor = SystemColors.Window };
                        break;
                    default:
                        ctl = new TextBox() { Text = "てきすと", ImeMode = ImeMode.Hiragana, ReadOnly = false };
                        break;
                    }

                    ctl.Width = w;
                    ctl.Left = x;
                    ctl.Top = y;
                    ctl.Tag = this.Controls.Count;

                    ctl.TextChanged += (s, e) =>
                    {
                        Logger.WriteLog($">TEXTCHAGNE {ctl.Tag} Text={ctl.Text}");
                        if (ctl.Text.Contains("+") || ctl.Text.Contains("＋") || ctl.Text.Contains("₊"))
                        {
                            if (System.Diagnostics.Debugger.IsAttached)
                            {
                                System.Diagnostics.Debug.WriteLine(Logger.sb.ToString());
                                Debugger.Break();
                            }
                            else
                            {
                                cancellSource.Cancel();
                                MessageBox.Show("失敗です", "", MessageBoxButtons.OKCancel);
                            }
                        }
                    };
                    ctl.LostFocus += (s, e) =>
                    {
                        Logger.WriteLog("%FOCUS-" + ctl.GetType().Name + " " + ctl.Tag.ToString());
                    };
                    ctl.GotFocus += (s, e) =>
                    {
                        Logger.WriteLog("%FOCUS+" + ctl.GetType().Name + " " + ctl.Tag.ToString());
                    };

                    this.Controls.Add(ctl);
                    x += w + 10;
                }

                y += h;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (taskTest == null)
            {
                if (MessageBox.Show("自動入力させる？", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    taskTest = TestClass.InputTestAsync(this, this.Controls[0], cancellSource.Token);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!cancellSource.IsCancellationRequested && taskTest != null)
            {
                e.Cancel = true;
                cancellSource.Cancel();
                taskTest?.ContinueWith((t) => this.BeginInvoke(this.Close));
                return;
            }
            base.OnClosing(e);
        }

        private System.Threading.CancellationTokenSource cancellSource = new System.Threading.CancellationTokenSource();
        private System.Threading.Tasks.Task taskTest;

        public bool UserProcessKeyPreview = false;
        Filter filter = new Filter();

        protected override bool ProcessKeyPreview(ref Message msg)
        {
            if (UserProcessKeyPreview)
            {//filterを登録せずにProcessKeyPreviewで試す場合
                return filter.PreFilterMessage(ref msg);
            }
            else
            {
                return false;
            }
        }
    }
}
