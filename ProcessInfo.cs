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
    /// Android设备进程信息
    /// </summary>
    class ProcessInfo
    {
        public string processName { get; private set; }
        public int pid { get; private set; }
        public PSLineInfo psInfo { get; private set; }
        public bool isArm64 { get; private set; }
        public int zygotePid { get; private set; }
        public int zygote64Pid { get; private set; }

        public List<PSLineInfo> psList { get; private set; }

        private static Regex s_regPsLine = new Regex(@"(.+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\w+)\s+(.+)");

        /*
         * USER           PID  PPID     VSZ    RSS WCHAN            ADDR S NAME
         * root             1     0   59944   3928 0                   0 S init
         * root             2     0       0      0 0                   0 S [kthreadd]
         */

        /// <summary>
        /// PS 输出的进程信息格式化
        /// </summary>
        public class PSLineInfo
        {
            public string user;
            public int pid;
            public int ppid;
            public int vsz;
            public int rss;
            public int wchan;
            public int addr;
            public char s;
            public string name;

            public string line;

            public override string ToString()
            {
                return name;
            }
        }

        public ProcessInfo(string processName)
        {
            this.processName = processName;
            if (string.IsNullOrEmpty(ADBFinder.adbPath))
                return;

            GetProcessList();
            CheckPsList();
        }

        private void GetProcessList()
        {
            psList = new List<PSLineInfo>();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = ADBFinder.adbPath;
            psi.Arguments = "shell ps";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            List<string> strLst = new List<string>(1024);
            using (Process p = Process.Start(psi))
            {
                p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (e.Data == null)
                        return;

                    // shell ps 的输出内容不自带换行符因此可以存储到List中.....
                    strLst.Add(e.Data);
                };
                p.BeginOutputReadLine();
                p.WaitForExit();
            }

            foreach (string line in strLst)
            {
                PSLineInfo info = GetPSLineInfo(line.Trim());
                if (info != null)
                    psList.Add(info);
            }
        }

        private void CheckPsList()
        {
            foreach(var info in psList)
            {
                if (info.name == "zygote")
                    zygotePid = info.pid;
                else if (info.name == "zygote64")
                    zygote64Pid = info.pid;
            }

            foreach(var info in psList)
            {
                if(info.name == processName)
                {
                    psInfo = info;
                    pid = info.pid;
                    if (info.ppid == zygote64Pid)
                        isArm64 = true;

                    break;
                }
            }
        }

        /// <summary>
        /// 从 shell ps 读取的一行中解析 pid
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private PSLineInfo GetPSLineInfo(string line)
        {
            var match = s_regPsLine.Match(line);
            if(!match.Success)
                return null;

            var groups = match.Groups;
            PSLineInfo info = new PSLineInfo();
            info.line = line;
            info.user = groups[1].Value;
            info.pid = int.Parse(groups[2].Value);
            info.ppid = int.Parse(groups[3].Value);
            info.vsz = int.Parse(groups[4].Value);
            info.rss = int.Parse(groups[5].Value);
            info.wchan = int.Parse(groups[6].Value);
            info.addr = int.Parse(groups[7].Value);
            info.s = groups[8].Value[0];
            info.name = groups[9].Value;

            return info;
        }
    }
}
