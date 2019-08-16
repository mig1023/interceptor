using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class ManualDocPack
    {
        public static List<Service> Services = new List<Service>();

        public static void AddService(string name)
        {
            foreach (Service service in Services)
                if (service.Name == name)
                {
                    service.Quantity += 1;
                    return;
                }

            Service newService = new Service();
            newService.Name = name;
            newService.Quantity = 1;
            Services.Add(newService);
        }

        public static void SubService(string name)
        {
            foreach (Service service in Services)
                if (service.Name == name)
                    if (service.Quantity > 0)
                        service.Quantity -= 1;
                    else
                        service.Quantity = 0;
        }

        public static void CleanServices()
        {
            Services.Clear();
        }

        public static string AllServices()
        {
            List<string> manDocPack = new List<string>();

            foreach (Service service in Services)
                for(int i = 0; i < service.Quantity; i++)
                    manDocPack.Add(service.Name);

            return String.Join("|", manDocPack.ToArray());
        }

        public static int GetService(string name)
        {
            foreach (Service service in Services)
                if (service.Name == name)
                    return service.Quantity;

            return 0;
        }
    }
}
