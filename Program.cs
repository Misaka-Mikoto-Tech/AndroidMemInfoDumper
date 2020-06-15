using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidMemInfoDumper
{
    class Program
    {
        const string PROCESS_NAME = "com.tencent.qq";

        static void Main(string[] args)
        {
            string processName = PROCESS_NAME;
            if (args.Length > 0)
                processName = args[0];

            string adbPath = ADBFinder.Find();
            if (string.IsNullOrEmpty(adbPath))
                return;

            ProcessInfo pi = new ProcessInfo(processName);
            if(pi.pid == 0)
            {
                Console.WriteLine($"没有找到进程{processName}");
                return;
            }

            string arch = pi.isArm64 ? "arm64" : "arm32";
            Console.WriteLine($"process \"{processName}(pid:{pi.pid})\" is {arch}");

            ProcessMemInfo pmi = new ProcessMemInfo(pi.pid);

            string dir = Environment.CurrentDirectory;
            File.WriteAllText($"{dir}/maps.txt", pmi.mapsStr);
            File.WriteAllText($"{dir}/smaps.txt", pmi.smapsStr);
            File.WriteAllText($"{dir}/gpemem.txt", pmi.gpuMemMaps);
            File.WriteAllText($"{dir}/showmap.txt", pmi.showmapStr);
            File.WriteAllText($"{dir}/smaps.csv", pmi.smapsStr_csv);
            File.WriteAllText($"{dir}/gpemem.csv", pmi.gpuMemMaps_csv);

            Console.WriteLine("数据已写入文件");
        }

        
    }
}
