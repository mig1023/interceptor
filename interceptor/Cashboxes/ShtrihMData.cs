using System.Collections.Generic;

namespace interceptor
{
    class ShtrihMData
    {
        public int tableNumber;
        public int fieldNumber;
        public int rowNumber;
        public string fieldValue;
        public string description;

        public static List<ShtrihMData> data = new List<ShtrihMData>() {

                new ShtrihMData() {
                    tableNumber = 6,
                    fieldNumber = 1,
                    rowNumber = 1,
                    fieldValue = "2000",
                    description = "неверное значение НДС (ожидалось 20%)",
                },
                new ShtrihMData() {
                    tableNumber = 6,
                    fieldNumber = 2,
                    rowNumber = 1,
                    fieldValue = "НДС 20%",
                    description = "неверное наименование поля НДС",
                },
                new ShtrihMData() {
                    tableNumber = 6,
                    fieldNumber = 1,
                    rowNumber = 2,
                    fieldValue = "0",
                    description = "неверное значение 'без НДС' (ожидалось 0%)",
                },
                new ShtrihMData() {
                    tableNumber = 6,
                    fieldNumber = 2,
                    rowNumber = 2,
                    fieldValue = "без НДС",
                    description = "неверное наименование поля 'без НДС'",
                },

                new ShtrihMData() {
                    tableNumber = 5,
                    fieldNumber = 1,
                    rowNumber = 1,
                    fieldValue = "НАЛИЧНЫМИ",
                    description = "неверное наименование поля типов оплаты (наличные)",
                },
                new ShtrihMData() {
                    tableNumber = 5,
                    fieldNumber = 1,
                    rowNumber = 2,
                    fieldValue = "БЕЗНАЛИЧНЫМИ",
                    description = "неверное наименование поля типов оплаты (безналичные)",
                },

                new ShtrihMData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 1,
                    fieldValue = "(услуги сервисные)",
                    description = "неверное наименование поля услуг (сервисные)",
                },
                new ShtrihMData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 2,
                    fieldValue = "(консульский сбор)",
                    description = "неверное наименование поля услуг (консульский)",
                },
                new ShtrihMData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 3,
                    fieldValue = "(ресепшен)",
                    description = "неверное наименование поля услуг (ресепшен)",
                },
                new ShtrihMData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 4,
                    fieldValue = "(РГС)",
                    description = "неверное наименование поля услуг (страхование-РГС)",
                },
                new ShtrihMData() {
                    tableNumber = 7,
                    fieldNumber = 1,
                    rowNumber = 5,
                    fieldValue = "(Капитал Лайф)",
                    description = "неверное наименование поля услуг (страхование-КЛ)",
                },
        };
    }
}
