using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LocalDCRSimulator {
    class Diagnostics
    {
        public static string LogFile = "Logfile";
        public static int MaxLogsBeforeCheck = 50;
        public static double MaxLogFileSizeMB = 4;
        private static int count = 0;

        public static void Log(string s)
        {
            string str = FormatLogString("Log", s);
            System.Diagnostics.Debug.Write(str);
            WriteToLog(str);
        }

        public static void Warning(string s, [CallerMemberName] string caller = "", [CallerLineNumber] int n = 0)
        {
            string str = FormatLogString("Warning (" + caller + ", " + n + ")", s);
            System.Diagnostics.Debug.Write(str);
            WriteToLog(str);
        }

        public static void Error(string s, [CallerMemberName] string caller = "", [CallerLineNumber] int n = 0)
        {
            string str = FormatLogString("\nError (" + caller + ", " + n + ")", s + "\n");
            System.Diagnostics.Debug.Write(str);
            WriteToLog(str);
        }

        private static string FormatLogString(string prefix, string message)
        {
            DateTime dt = DateTime.Now;
            return string.Format("[{0,4}/{1,2}/{2,2} : {3,2}-{4,2}-{5,2}-{6,4}] {7}: {8}\n", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, prefix, message);
        }

        private static void WriteToLog(string logmessage)
        {
            string path = Path.Combine(Environment.CurrentDirectory, LogFile);
            if (count >= MaxLogsBeforeCheck && File.Exists(path))
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Length >= (1028 << 10) * 4)
                {
                    List<string> content = File.ReadAllText(path).Split('\n').ToList();
                    File.Delete(path);
                    File.WriteAllLines(path, content.GetRange((int)Math.Ceiling(((float)content.Count) / 2.0f), content.Count / 2 - 1));
                }
                count = 0;
            }
            if (File.Exists(path))
            {
                StreamWriter sw = File.AppendText(path);
                sw.Write(logmessage);
                sw.Close();
            } else {
                File.WriteAllText(path, logmessage);
            }
            count++;
        }
    }
}
