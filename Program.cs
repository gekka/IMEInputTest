using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gekka.IMEInputTest
{
    internal static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool useMessageFilter = false;
            if (MessageBox.Show("IMessageFilterをつかう？", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Filter.Add();
                useMessageFilter = true;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            Application.Run(new Form1() { UserProcessKeyPreview = !useMessageFilter });
        }
    }
}
