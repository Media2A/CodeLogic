using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeLogic
{

    public partial class CodeLogic_Framework
    {
        public enum LogType
        {
            TRACE,
            DEBUG,
            INFO,
            WARNING,
            WARN,
            ERROR,
            FATAL,
            CUSTOM
        }
        public static void AddLogEntry(LogType logType, string logString)
        {
            string LogPath = CodeLogic_Defaults.GetLogFilePath();

            // Build log string

            switch (logType)
            {
                case LogType.TRACE:
                    break;
                case LogType.DEBUG:
                    break;
                case LogType.INFO:
                    break;
                case LogType.WARNING:
                    break;
                case LogType.WARN:
                    break;
                case LogType.ERROR:
                    break;
                case LogType.FATAL:
                    break;
                case LogType.CUSTOM:
                    break;
                default:
                    break;
            }

            string[] lines = { $"{System.Environment.NewLine}{ DateTime.Now.ToString() }: [{ logType }] [{logType.GetTypeCode().ToString()}], { logString }" };

            File.AppendAllLines(LogPath + "log.txt", lines);

        }
    }
}
