using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace socketserver_autorestart
{
    class Program
    {
        static void Main(string[] args)
        {
            string serverName = args[0];

            Log(String.Format("самовосстановление: отслеживаем {0}", serverName));
            Console.WriteLine("\n\n\n\tсамовосстановление сокет-сервера");

            while (true)
            {
                try
                {
                    Process[] proc = Process.GetProcessesByName(serverName);

                    if (proc == null || proc.Length == 0)
                    {
                        Log("процесс не найден -> перезапуск");

                        Process.Start(serverName + ".exe");
                    }
                    else if (!proc[0].Responding)
                    {
                        Log("пропала связь -> перезапуск");

                        try
                        {
                            proc[0].Kill();
                        }
                        catch
                        {
                            Log("ошибка остановки текущего процесса (совсем повис?)");
                        }

                        Process.Start(proc[0].ProcessName + ".exe");
                    }
                }
                catch (Exception ex)
                {
                    Log("неизвестная ОШИБКА: " + ex.Message);
                    Log("стек ошибки: " + ex.ToString());
                }

                Thread.Sleep(3000);
            }
        }

        public static void Log(string errorLine)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("socketserver.log", true))
                    sw.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"), errorLine));
            }
            catch (Exception)
            {
                // nothing to do here
            }
        }
    }
}
