﻿using System;
using System.Collections.Generic;
using System.Globalization;
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
        public int CashierPass;

        public int MoneyType;
        public decimal Total;
        public decimal Money;

        public DocPack()
        {
            // only for serialization
        }

        decimal manualParseDecimal(string line)
        {
            double decimalTemporary = double.Parse(line, CultureInfo.InvariantCulture); 

            return (decimal)decimalTemporary;
        }

        public DocPack(string requestLine)
        {
            XmlDocument request = new XmlDocument();

            request.LoadXml(requestLine);

            foreach(XmlNode node in request.SelectNodes("toCashbox/Services/Service"))
            {
                Service newService = new Service();

                newService.Name = node["Name"].InnerText;
                newService.Quantity = int.Parse(node["Quantity"].InnerText);
                newService.Price = manualParseDecimal(node["Price"].InnerText);
                newService.VAT = int.Parse(node["VAT"].InnerText);

                this.Services.Add(newService);
            }

            XmlNode info = request.SelectSingleNode("toCashbox/Info");

            this.AgrNumber = info["AgrNumber"].InnerText;
            this.Cashier = info["Cashier"].InnerText;
            this.CashierPass = int.Parse(info["CashierPass"].InnerText);
            this.MoneyType = int.Parse(info["MoneyType"].InnerText);
            this.Total = manualParseDecimal(info["Total"].InnerText);
            this.Money = manualParseDecimal(info["Money"].InnerText);

            Log.addDocPack(this);
        }
    }
}