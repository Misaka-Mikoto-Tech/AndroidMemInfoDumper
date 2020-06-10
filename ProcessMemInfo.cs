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
        /// <summary>
        /// 综合了 maps 和 showmap 的数据
        /// </summary>
        public string smapsStr { get; private set; }
        public string showmapStr { get; private set; }
        public string gpuMemMaps { get; private set; }

        public string smapsStr_csv { get; private set; }
        public string gpuMemMaps_csv { get; private set; }

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


        private struct SmapsLineInfo
		{
            public string addrStart;
            public string addrEnd;
            public string attr; // rwxp/s， 可读、可写、可运行、私有或共享
            public string mapOffset;
            public string deviceNo;
            public string fileNo;
            public string mappedFile;
            public int size;
            public int rss;
            public int pss;
            public int shared_clean;
            public int shared_dirty;
            public int private_clean;
            public int private_dirty;
            public int referenced;
            public int anonymous;
            public int swap;
            public int kernelPageSize;
            public int MMUPageSize;
            public int Locked;
            public string VmFlags; // rd mr mw me ac 

            /*
            rd  - readable
            wr  - writeable
            ex  - executable
            sh  - shared
            mr  - may read
            mw  - may write
            me  - may execute
            ms  - may share
            gd  - stack segment growns down
            pf  - pure PFN range
            dw  - disabled write to the mapped file
            lo  - pages are locked in memory
            io  - memory mapped I/O area
            sr  - sequential read advise provided
            rr  - random read advise provided
            dc  - do not copy area on fork
            de  - do not expand area on remapping
            ac  - area is accountable
            nr  - swap space is not reserved for the area
            ht  - area uses huge tlb pages
            ar  - architecture specific flag
            dd  - do not include area into core dump
            sd  - soft-dirty flag
            mm  - mixed map area
            hg  - huge page advise flag
            nh  - no-huge page advise flag
            mg  - mergable advise flag
             */
		}

		private string GetCsvOfSmaps(string smapsStr)
		{
            if (string.IsNullOrEmpty(smapsStr))
                return null;

            StringBuilder sb = new StringBuilder((int)(smapsStr.Length * 1.5f));
            return null;
		}
    }
}
