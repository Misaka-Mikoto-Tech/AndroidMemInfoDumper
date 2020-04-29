using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidMemInfoDumper
{
    /// <summary>
    /// 进程内存和显存信息
    /// </summary>
    public class ProcessMemInfo
    {
        const string MAPS_FILE_PATH_TEMPLATE = "/proc/{0}/maps";
        const string SMAPS_FILE_PATH_TEMPLATE = "/proc/{0}/smaps";
        const string GPU_MEM_FILE_PATH_TEMPLATE = "/d/kgsl/proc/{0}/mem";
        const string SHELL_CAT_FILE_TEMPLATE = @"su
cat {0}
exit
exit
exit";
        const string SHELL_SHOWMAP_TEMPLATE = @"su
showmap -a {0}
exit
exit
exit";

        /// <summary>
        /// 解析 showmap 行数据的正则
        /// </summary>
        static Regex s_regShowmapLine = new Regex(@"");

        /// <summary>
        /// 等待进程退出的最长时间
        /// </summary>
        const int MAX_WAIT_TIME = 1000 * 5;

        public string mapsStr { get; private set; }
        public string smapsStr { get; private set; }
        public string showmapStr { get; private set; }
        public string gpuMemMaps { get; private set; }

        public ProcessMemInfo(int pid)
        {
            mapsStr = GetAndroidFileContent(string.Format(MAPS_FILE_PATH_TEMPLATE, pid));
            smapsStr = GetAndroidFileContent(string.Format(SMAPS_FILE_PATH_TEMPLATE, pid));
            gpuMemMaps = GetAndroidFileContent(string.Format(GPU_MEM_FILE_PATH_TEMPLATE, pid));

            showmapStr = ExecuteShellAndGetOutput(string.Format(SHELL_SHOWMAP_TEMPLATE, pid));
        }

        private string GetAndroidFileContent(string path)
        {
            string inputStr = string.Format(SHELL_CAT_FILE_TEMPLATE, path);
            return ExecuteShellAndGetOutput(inputStr);
        }

        private string ExecuteShellAndGetOutput(string inputStr)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = ADBFinder.adbPath;
            psi.Arguments = "shell";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;

            StringBuilder sb = new StringBuilder(1024 * 1024);
            using (Process p = Process.Start(psi))
            {
                p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (e.Data == null)
                        return;

                    // shell ps 的输出内容不自带换行符因此可以存储到List中.....
                    sb.AppendLine(e.Data);
                };
                p.BeginOutputReadLine();
                StreamWriter sw = p.StandardInput;
                sw.Write(inputStr);
                p.WaitForExit(MAX_WAIT_TIME);
            }

            return sb.ToString();
        }
    }
}
