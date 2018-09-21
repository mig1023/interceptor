using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace shtrih_interceptor
{
    public class DocPack
    {
        public List<Service> Services = new List<Service>();

        public string AgrNumber;
        public string Cashier;

        public int MoneyType;
        public string Total;
        public string Money;
        public string Change;

        public DocPack()
        {

        }

        public DocPack(string requestLine)
        {
            XmlDocument request = new XmlDocument();

            request.LoadXml(requestLine);

            foreach(XmlNode node in request.SelectNodes("toCashbox/Services/Service"))
            {
                Service newService = new Service();

                newService.Name = node["Name"].InnerText;
                newService.Number = int.Parse(node["Number"].InnerText);
                newService.Price = node["Price"].InnerText;
                newService.Total = node["Total"].InnerText;
                newService.VAT = node["VAT"].InnerText;

                this.Services.Add(newService);
            }

            XmlNode info = request.SelectSingleNode("toCashbox/Info");

            this.AgrNumber = info["AgrNumber"].InnerText;
            this.Cashier = info["Cashier"].InnerText;
            this.MoneyType = int.Parse(info["MoneyType"].InnerText);
            this.Total = info["Total"].InnerText;
            this.Money = info["Money"].InnerText;
            this.Change = info["Change"].InnerText;

            Log.addDocPack(this);
        }
    }
}
