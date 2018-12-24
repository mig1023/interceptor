using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace autoupdate
{
    class Program
    {
        static void Main(string[] args)
        {
            string interceptor = args[0].Replace(".exe", "");

            while (Process.GetProcessesByName(interceptor).Length > 0)
                Thread.Sleep(300);

            string[] files = Directory.GetFiles(args[1]);

            foreach(string file in files)
            {
                string fileNewName = Path.GetFileName(file);

                if (File.Exists(fileNewName))
                    File.Delete(fileNewName);

                File.Move(file, fileNewName);
            }

            DirectoryInfo oldDirectory = new DirectoryInfo(args[1]);
            oldDirectory.Delete();

            Process.Start(args[0]);
        }
    }
}
