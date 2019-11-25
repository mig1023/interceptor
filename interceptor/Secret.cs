using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class Secret
    {
        public static string PROTOCOL_PASS = "";

        public static string PROTOCOL_IP_CLIENT = (MainWindow.TEST_VERSION ? "127.0.0.1" : "127.0.0.1");
        public static int PROTOCOL_PORT_SEND = 80;

        public static string PROTOCOL_IP_SERVER = "127.0.0.1";
        public static int PROTOCOL_PORT_RECEIVE = 80;
    }
}
