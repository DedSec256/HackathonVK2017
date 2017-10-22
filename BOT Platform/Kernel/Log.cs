using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BOT_Platform.Kernel
{
    public static class Log
    {
        static Log()
        {
            if (!Directory.Exists(DIRECTORY_PATH)) Directory.CreateDirectory(DIRECTORY_PATH);
        }

        private const string DIRECTORY_PATH = @"Data\System";
        public static readonly string logFile = DIRECTORY_PATH + @"\log.txt";
        static object lockObj = new object();
        static void InLog(object text, string filePath)
        {
            lock (lockObj)
            {
                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    sw.Write(text.ToString());
                }
            }
        }
        public static void WriteLog(string text, string botFilePath)
        {
            botFilePath = botFilePath + $@"\{logFile}";
            Task.Run(() => InLog(text, botFilePath));
        }
    }
}
