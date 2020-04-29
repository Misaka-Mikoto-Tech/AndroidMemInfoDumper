using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidMemInfoDumper
{
    /// <summary>
    /// 查找 adb.exe 的工具类
    /// </summary>
    public static class ADBFinder
    {
        public const int MAX_FIND_COUNT = 5;
        public static string adbPath { get; private set; }
        public static bool finded { get; private set; }

        public static string Find()
        {
            if (FindDefaultADBPath())
            {
                finded = true;
                return adbPath;
            }

            Console.WriteLine("没有找到 adb.exe 路径，请手动输入:");
            for (int i = 0; i < MAX_FIND_COUNT; i++)
            {
                string path = Console.ReadLine();
                if (File.Exists(path))
                {
                    finded = true;
                    adbPath = path;
                    break;
                }

                Console.WriteLine("路径不正确");
            }

            return adbPath;
        }

        private static  bool FindDefaultADBPath()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "where";
            psi.Arguments = "adb.exe";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Process p = Process.Start(psi);
            p.OutputDataReceived += FindAdb_OutputReceivedEventHandler;
            p.BeginOutputReadLine();
            p.WaitForExit();

            if (!File.Exists(adbPath))
                adbPath = null;

            return !string.IsNullOrEmpty(adbPath);
        }

        private static void FindAdb_OutputReceivedEventHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                adbPath = e.Data;
        }
    }
}
