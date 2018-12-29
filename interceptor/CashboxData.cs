using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class CashboxData
    {
        public int tableNumber;
        public int fieldNumber;
        public int rowNumber;
        public string fieldValue;
        public string description;

        public static List<CashboxData> data = new List<CashboxData>() {

                new CashboxData() {
                    tableNumber = 6,
                    fieldNumber = 1,
                    rowNumber = 1,
                    fieldValue = "2000",
                    description = "НДС/1/значение",
                },
                new CashboxData() {
                    tableNumber = 6,
                    fieldNumber = 2,
                    rowNumber = 1,
                    fieldValue = "НДС 20%",
                    description = "НДС/1/наименование",
                },
                new CashboxData() {
                    tableNumber = 6,
                    fieldNumber = 1,
                    rowNumber = 2,
                    fieldValue = "0",
                    description = "НДС/2/значение",
                },
                new CashboxData() {
                    tableNumber = 6,
                    fieldNumber = 2,
                    rowNumber = 2,
                    fieldValue = "без НДС",
                    description = "НДС/2/наименование",
                },

                new CashboxData() {
                    tableNumber = 5,
                    fieldNumber = 1,
                    rowNumber = 1,
                    fieldValue = "НАЛИЧНЫМИ",
                    description = "оплата/1",
                },
                new CashboxData() {
                    tableNumber = 5,
                    fieldNumber = 1,
                    rowNumber = 2,
                    fieldValue = "БЕЗНАЛИЧНЫМИ",
                    description = "оплата/2",
                },

                new CashboxData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 1,
                    fieldValue = "(услуги сервисные)",
                    description = "отделы/1",
                },
                new CashboxData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 2,
                    fieldValue = "(консульский сбор)",
                    description = "отделы/2",
                },
                new CashboxData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 3,
                    fieldValue = "(ресепшен)",
                    description = "отделы/3",
                },
                new CashboxData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 4,
                    fieldValue = "(страхование)",
                    description = "отделы/4",
                },
        };
    }
}
