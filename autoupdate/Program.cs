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
            Console.WriteLine("START");

            string interceptor = args[0].Replace(".exe", "");

            while (Process.GetProcessesByName(interceptor).Length > 0)
                Thread.Sleep(300);

            string[] files = Directory.GetFiles(args[1]);

            foreach(string x in files)
                Console.WriteLine(x);

            string s = Console.ReadLine();
        }
    }
}
