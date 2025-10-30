﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Panel.Select
{
    public static class Filog
    {
        private static readonly string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Panel.Select.Logs", "trace_log.txt"
        );

        public static int Log(string mssg)
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:HH:mm:ss.fff} - {mssg}{Environment.NewLine}");
            return 0;
        }
    }
}
