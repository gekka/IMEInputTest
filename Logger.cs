using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gekka.IMEInputTest
{
    class Logger
    {
        public static System.Text.StringBuilder sb = new System.Text.StringBuilder();

        public static void WriteLog(string msg)
        {
            lock (sb)
            {
                if (sb.Length > 10000)
                {
                    sb.Remove(0, 9000);
                }
                sb.AppendLine(msg);
            }
        }
    }
}
