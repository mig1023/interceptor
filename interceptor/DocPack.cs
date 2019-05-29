using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;


namespace interceptor
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

        public int RequestOnly;

        public string Email = String.Empty;
        public string Mobile = String.Empty;

        public DocPack()
        {
            // only for serialization
        }

        public static decimal manualParseDecimal(string line)
        {
            double decimalTemporary = 0;

            if (line == String.Empty)
                decimalTemporary = 0;
            else
                Double.TryParse(line, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalTemporary);

            return (decimal)decimalTemporary;
        }

        public void DocPackFromXML(string requestLine)
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
                newService.Department = int.Parse(node["Department"].InnerText);

                if (node.SelectSingleNode("ReceptionID") != null)
                    newService.ReceptionID = int.Parse(node["ReceptionID"].InnerText);
                else
                    newService.ReceptionID = 0;

                if (node.SelectSingleNode("ReturnShipping") != null)
                    newService.ReturnShipping = int.Parse(node["ReturnShipping"].InnerText);
                else
                    newService.ReturnShipping = 0;

                this.Services.Add(newService);
            }

            XmlNode info = request.SelectSingleNode("toCashbox/Info");

            this.AgrNumber = info["AgrNumber"].InnerText;
            this.Cashier = info["Cashier"].InnerText;
            this.CashierPass = int.Parse(info["CashierPass"].InnerText);
            this.MoneyType = int.Parse(info["MoneyType"].InnerText);
            this.Total = manualParseDecimal(info["Total"].InnerText);
            this.Money = manualParseDecimal(info["Money"].InnerText);

            if ( (this.MoneyType == 2) && (this.Total != this.Money))
            {
                Log.Add("Ошибка расхождения сумм при оплате картой " + 
                    "(Total: " + this.Total + " <=> Money: " + this.Money  + ")");
                Log.Add("Выполнено приведение к сумме Total");

                this.Money = this.Total;
            }

            this.RequestOnly = int.Parse(info["RequestOnly"].InnerText);

            if (info["EMail"] != null)
                this.Email = info["EMail"].InnerText;

            if (info["Mobile"] != null)
                this.Mobile = info["Mobile"].InnerText;

            Log.AddDocPack(this);
        }

        public void AddInfo(string Cashier, int CashierPass, int MoneyType)
        {
            this.Cashier = Cashier;
            this.CashierPass = CashierPass;
            this.MoneyType = MoneyType;
        }

        public void AddService(string ServiceName, int Quantity, decimal Price, int VAT)
        {
            Service newService = new Service();

            newService.Name = ServiceName;
            newService.Quantity = Quantity;
            newService.Price = Price;
            newService.VAT = VAT;

            this.Services.Add(newService);
        }

        public void AddMoney(string Summ)
        {
            this.Money = manualParseDecimal(Summ);
        }
    }
}
