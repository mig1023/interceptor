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
    class AutoUpdate
    {
        static void Main(string[] args)
        {
            string interceptor = args[0].Replace(".exe", String.Empty);

            Log("ожидание остановки процесса " + interceptor);

            while (Process.GetProcessesByName(interceptor).Length > 0)
                Thread.Sleep(300);

            string[] files = Directory.GetFiles(args[1]);

            foreach(string file in files)
            {
                string fileNewName = Path.GetFileName(file);

                if (File.Exists(fileNewName))
                {
                    Log("удаление старого файла " + fileNewName);
                    File.Delete(fileNewName);
                }

                Log("перемещение нового файла " + fileNewName);
                File.Move(file, fileNewName);
            }

            Log("удаление папки обновления " + args[1]);
            DirectoryInfo oldDirectory = new DirectoryInfo(args[1]);
            oldDirectory.Delete();

            Log("перезапуск процесса " + args[0]);
            Process.Start(args[0]);
        }

        public static void Log(string line)
        {
            string logFileName = "interceptor-update.log";

            using (StreamWriter sw = new StreamWriter(logFileName, true))
            {
                string dateLine = DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss");

                sw.WriteLine(dateLine + " " + line);
            }
        }
    }
}
