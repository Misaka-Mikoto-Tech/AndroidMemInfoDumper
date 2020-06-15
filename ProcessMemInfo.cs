using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

            smapsStr_csv = GetCsvOfSmaps(smapsStr);
            gpuMemMaps_csv = GetCsvOfGPUMem(gpuMemMaps);
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


        private class SmapsLineInfo
		{
            public string addrStart;
            public string addrEnd;
            public string attr; // rwxp/s， 可读、可写、可运行、私有或共享
            public string mapOffset;
            public string deviceNo;
            public string fileNo;
            public string mappedFile;

			public bool isStack;
			public bool isSo;
			public bool isDalvik;
			public bool isBss;
			public bool isKgsl;

			public string size;
			public string kernelPageSize;
			public string MMUPageSize;
			public string rss;
            public string pss;
            public string shared_clean;
            public string shared_dirty;
            public string private_clean;
            public string private_dirty;
            public string referenced;
            public string anonymous;
            public string swap;
            public string swapPss;
            public string Locked;
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
            ac  - area is accountable       // VM_ACCOUNT, used by anonymous shared, private mappings and special mappings, 此种内存没有名字， mappedFile 字段为空
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

            /*
            static void show_smap_vma_flags(struct seq_file *m, struct vm_area_struct *vma)
            {
        
        	    const struct {
        		    const char l[2];
        	    }
		           mnemonics[BITS_PER_LONG] = {
        		    [ilog2(VM_READ)]	    = { .l = {'r', 'd'} },
        		    [ilog2(VM_WRITE)]	    = { .l = {'w', 'r'} },
        		    [ilog2(VM_EXEC)]	    = { .l = {'e', 'x'} },
        		    [ilog2(VM_SHARED)]	    = { .l = {'s', 'h'} },
        		    [ilog2(VM_MAYREAD)]	    = { .l = {'m', 'r'} },
        		    [ilog2(VM_MAYWRITE)]	= { .l = {'m', 'w'} },
        		    [ilog2(VM_MAYEXEC)]	    = { .l = {'m', 'e'} },
        		    [ilog2(VM_MAYSHARE)]	= { .l = {'m', 's'} },
        		    [ilog2(VM_GROWSDOWN)]	= { .l = {'g', 'd'} },
        		    [ilog2(VM_PFNMAP)]	    = { .l = {'p', 'f'} },
        		    [ilog2(VM_DENYWRITE)]	= { .l = {'d', 'w'} },
        		    [ilog2(VM_LOCKED)]	    = { .l = {'l', 'o'} },
        		    [ilog2(VM_IO)]		    = { .l = {'i', 'o'} },
        		    [ilog2(VM_SEQ_READ)]	= { .l = {'s', 'r'} },
        		    [ilog2(VM_RAND_READ)]	= { .l = {'r', 'r'} },
        		    [ilog2(VM_DONTCOPY)]	= { .l = {'d', 'c'} },
        		    [ilog2(VM_DONTEXPAND)]	= { .l = {'d', 'e'} },
        		    [ilog2(VM_ACCOUNT)]	    = { .l = {'a', 'c'} },
        		    [ilog2(VM_NORESERVE)]	= { .l = {'n', 'r'} },
        		    [ilog2(VM_HUGETLB)]	    = { .l = {'h', 't'} },
        		    [ilog2(VM_NONLINEAR)]	= { .l = {'n', 'l'} },
        		    [ilog2(VM_ARCH_1)]	    = { .l = {'a', 'r'} },
        		    [ilog2(VM_DONTDUMP)]	= { .l = {'d', 'd'} },
        		    [ilog2(VM_MIXEDMAP)]	= { .l = {'m', 'm'} },
        		    [ilog2(VM_HUGEPAGE)]	= { .l = {'h', 'g'} },
        		    [ilog2(VM_NOHUGEPAGE)]	= { .l = {'n', 'h'} },
        		    [ilog2(VM_MERGEABLE)]	= { .l = {'m', 'g'} },
        	    };
        
        	    size_t i;
        
        	    seq_puts(m, "VmFlags: ");
        	    for (i = 0; i<BITS_PER_LONG; i++) {
        		    if (vma->vm_flags & (1 << i))
        			    seq_printf(m, "%c%c ",
        				       mnemonics[i].l[0],
        				       mnemonics[i].l[1]);
        	    }
        	    seq_putc(m, '\n');
            }

			 */

            private static StringBuilder s_sb = new StringBuilder();
            private static FieldInfo[] s_fis = typeof(SmapsLineInfo).GetFields(BindingFlags.Public | BindingFlags.Instance);

            public static string Title()
			{
                s_sb.Clear();
                foreach(var fi in s_fis)
				{
                    s_sb.Append(fi.Name);
                    s_sb.Append(',');
				}

                string title = s_sb.ToString();
                return title.Substring(0, title.Length - 1);
			}

			public override string ToString()
			{
                s_sb.Clear();
				foreach (var fi in s_fis)
				{
					s_sb.Append(fi.GetValue(this));
					s_sb.Append(',');
				}

				string title = s_sb.ToString();
				return title.Substring(0, title.Length - 1);
			}

		}


        private Regex _regAddrInfo = new Regex(@"([0-9a-f]+)-([0-9a-f]+)\s+([rwxps\-]+)\s+([0-9a-f]+)\s+([0-9a-f\:]+)\s+([0-9a-f]+)\s+(.*)");
        private Regex _regGetNum = new Regex(@"(.+):\s+(\d+).*"); // Size, Rss, Pss, and so on
        private string GetCsvOfSmaps(string smapsStr)
		{
            if (string.IsNullOrEmpty(smapsStr))
                return null;

            StringReader sr = new StringReader(smapsStr);

            List<SmapsLineInfo> lineInfos = new List<SmapsLineInfo>();
            SmapsLineInfo currLineInfo = null;

            string line;
            int lineNo = -1;
            while((line = sr.ReadLine())!= null)
            {
                lineNo++;
                char firstChr = line[0];
                if((firstChr >= '0' && firstChr <= '9') || (firstChr >= 'a' && firstChr <= 'f'))
                {
                    currLineInfo = new SmapsLineInfo();
                    lineInfos.Add(currLineInfo);

                    Match match = _regAddrInfo.Match(line);
                    if (!match.Success)
                    {
                        Console.WriteLine($"unexpected line {lineNo}, content:{line}");
                        return null;
                    }

                    var groups = match.Groups;
                    currLineInfo.addrStart = groups[1].Value;
                    currLineInfo.addrEnd = groups[2].Value;
                    currLineInfo.attr = groups[3].Value;
                    currLineInfo.mapOffset = groups[4].Value;
                    currLineInfo.deviceNo = groups[5].Value;
                    currLineInfo.fileNo = groups[6].Value;
                    currLineInfo.mappedFile = groups[7].Value;

                    if (currLineInfo.mappedFile.Contains("stack"))
                        currLineInfo.isStack = true;

					if (currLineInfo.mappedFile.Contains(".so"))
						currLineInfo.isSo = true;

					if (currLineInfo.mappedFile.Contains("dalvik"))
						currLineInfo.isDalvik = true;

					if (currLineInfo.mappedFile.Contains(".bss"))
						currLineInfo.isBss = true;

					if (currLineInfo.mappedFile.Contains("kgsl-3d0"))
						currLineInfo.isKgsl = true;
				}
                else
				{
                    Match match = _regGetNum.Match(line);
                    if(match.Success) // Size, Rss, Pss.....
					{
                        var groups = match.Groups;
                        string key = groups[1].Value;
                        string number = groups[2].Value;

                        switch(key)
						{
                            case "Size":
                                currLineInfo.size = number;
                                break;
                            case "KernelPageSize":
                                currLineInfo.kernelPageSize = number;
                                break;
                            case "MMUPageSize":
                                currLineInfo.MMUPageSize = number;
                                break;
                            case "Rss":
                                currLineInfo.rss = number;
                                break;
                            case "Pss":
                                currLineInfo.pss = number;
                                break;
                            case "Shared_Clean":
                                currLineInfo.shared_clean = number;
                                break;
                            case "Shared_Dirty":
                                currLineInfo.shared_dirty = number;
                                break;
                            case "Private_Clean":
                                currLineInfo.private_clean = number;
                                break;
                            case "Private_Dirty":
                                currLineInfo.private_dirty = number;
                                break;
                            case "Referenced":
                                currLineInfo.referenced = number;
                                break;
                            case "Anonymous":
                                currLineInfo.anonymous = number;
                                break;
                            case "Swap":
                                currLineInfo.swap = number;
                                break;
                            case "SwapPss":
                                currLineInfo.swapPss = number;
                                break;
                            case "Locked":
                                currLineInfo.Locked = number;
                                break;
                        }
					}
					else
					{
                        if(line.StartsWith("VmFlags:", StringComparison.Ordinal))
						{
                            currLineInfo.VmFlags = line.Substring("VmFlags: ".Length);
						}
					}
				}
			}

            StringBuilder sb = new StringBuilder((int)(smapsStr.Length * 1.5f));
            sb.AppendLine(SmapsLineInfo.Title());
            foreach(var lineInfo in lineInfos)
			{
                sb.AppendLine(lineInfo.ToString());
			}

            return sb.ToString();
		}

        private string GetCsvOfGPUMem(string gpuMemStr)
		{
			if (string.IsNullOrEmpty(gpuMemStr))
				return null;

			StringReader sr = new StringReader(gpuMemStr);

			string line;
            StringBuilder sb = new StringBuilder((int)(gpuMemStr.Length * 1.5f));
            while ((line = sr.ReadLine()) != null)
            {
                string[] cols = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string col in cols)
				{
                    sb.Append(col);
                    sb.Append(',');
                }
                sb.AppendLine();
            }

            string ret = sb.ToString();
		    return ret.Substring(0, ret.Length - 1);
		}
    }
}
