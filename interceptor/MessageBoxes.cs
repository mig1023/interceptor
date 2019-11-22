using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace interceptor
{
    class MessageBoxes
    {
        static private MessageBoxResult MessageBoxWithLog(string message, bool warning = false)
        {
            Log.Add("предупреждение: " + message);

            MessageBoxResult result = MessageBox.Show(
               message, "Внимание!", MessageBoxButton.YesNo,
               (warning ? MessageBoxImage.Warning : MessageBoxImage.Question)
            );

            Log.Add("ответ: " + (result == MessageBoxResult.Yes ? "ДА" : "нет"));

            return result;
        }

        static public MessageBoxResult NullSummCash()
        {
            return MessageBoxWithLog("Введена нулевая сумма оплаты чека. Продолжить?");
        }

        static public MessageBoxResult NullReturnDate()
        {
            return MessageBoxWithLog("Дата договора не введена, будет использована текущая. Продолжить?");
        }

        static public MessageBoxResult NullInServices(string services)
        {
            return MessageBoxWithLog(
                "Услуги не имеют цены по прайслисту выбранного центра: " + services.TrimEnd() +
                ". Такие услуги не будут отображены в чеке. Продолжить?"
            );
        }

        static public MessageBoxResult isReportCleaningNeed()
        {
            return MessageBoxWithLog("Распечатка отчёта с гашением приведёт к закрытию смены. Продолжить?", warning: true);
        }

        static public void ChangeMessage(string change)
        {
            MessageBoxResult msg = MessageBox.Show(
              "Сдача: " + change, "Оплата", MessageBoxButton.OK, MessageBoxImage.Information
            );
        }

        static public void CanceledDocument()
        {
            MessageBoxResult msg = MessageBox.Show(
                "Открытый чек успешно анулирован", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning
            );
        }

        static public void ShowReceiptContent(DocPack doc)
        {
            string receipt = String.Empty;

            foreach (Service service in doc.Services)
            {
                decimal sum = service.Price * service.Quantity;
                receipt += service.Name + "\n" + service.Price + " x " + service.Quantity + " = " + sum + "\n\n";
            }

            receipt += "\nИТОГО: " + doc.Total.ToString() + "\n";

            MessageBoxResult msg = MessageBox.Show(
                receipt, "Чек", MessageBoxButton.OK, MessageBoxImage.Information
            );
        }

        static public void ShowCashboxSearchCancel()
        {
            MessageBox.Show("Программа в процессе поиска кассы.\nК сожалению, она не может закрыться мгновенно.\n" +
                "Она будет оставаться ещё некоторое время в памяти.\n\nЭто может иметь эффект при перезапуске программы.\n" +
                "Если возникнут затруднения - попробуйте перезапустить программу чуть позже, через несколько минут",
                "interceptor", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
