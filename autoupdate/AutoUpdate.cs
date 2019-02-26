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
        static string PROTOCOL_PASS = "";

        static void Main(string[] args)
        {
            if ((args.Length > 1) && (args[0].ToLower() == "md5show"))
            {
                string[] lines = System.IO.File.ReadAllLines(args[1]);

                string crcCheck = CreateMD5(string.Join(String.Empty, lines));

                Console.WriteLine(args[1].ToString() + " ----> " + crcCheck.ToString());
                Console.ReadLine();

                Environment.Exit(0);
            }

            string interceptor = args[0].Replace(".exe", String.Empty);

            Log("ожидание остановки процесса " + interceptor);

            while (Process.GetProcessesByName(interceptor).Length > 0)
                Thread.Sleep(300);

            string[] files = Directory.GetFiles(args[1]);

            foreach (string file in files)
            {
                string fileNewName = Path.GetFileName(file);

                if (File.Exists(fileNewName))
                    File.Delete(fileNewName);

                Log("перемещение нового файла " + fileNewName);
                File.Move(file, fileNewName);
            }

            string[] dirs = Directory.GetDirectories(Directory.GetCurrentDirectory(), "update_*");

            foreach (string dir in dirs)
                Directory.Delete(dir, true);

            Log("перезапуск " + interceptor);
            Process.Start(args[0]);
        }

        public static void Log(string line)
        {
            string logFileName = "logs\\interceptor-update.log";

            using (StreamWriter sw = new StreamWriter(logFileName, true))
            {
                string dateLine = DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss");

                sw.WriteLine(dateLine + " " + line);
            }
        }

        public static string CreateMD5(string input, bool notOrd = false)
        {
            if (notOrd)
                input += PROTOCOL_PASS;
            else
            {
                byte[] bytes = Encoding.GetEncoding(1251).GetBytes(PROTOCOL_PASS);

                foreach (byte b in bytes)
                    input += b.ToString() + " ";
            }

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString();
            }
        }
    }
}
