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
        static private MessageBoxResult MessageBoxWithLog(string message)
        {
            Log.Add("предупреждение: " + message);

            MessageBoxResult result = MessageBox.Show(
               message, "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question
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

        static public MessageBoxResult ServSummEmpty(string services)
        {
            bool many = (services.IndexOf(',') >= 0 ? true : false);

            return MessageBoxWithLog(
                (
                    many ?
                    "Выбраны услуги " + services + ". Но суммы оставлены нулевыми" :
                    "Выбрана услуга " + services + ". Но сумма оставлена нулевой"
                ) + ". Продолжить?"
            );
        }

        static public MessageBoxResult ServNoClick(string services)
        {
            bool many = (services.IndexOf(',') >= 0 ? true : false);

            return MessageBoxWithLog(
                (
                    many ?
                    "Указаны суммы для " + services + ". Но эти услуги не выбраны" :
                    "Указана сумма для " + services + ". Но эта услуга не выбрана"
                ) + ". Продолжить?"
            );
        }

        static public MessageBoxResult ServFieldFail(string services)
        {
            bool many = (services.IndexOf(',') >= 0 ? true : false);

            return MessageBoxWithLog(
                (
                    many ? 
                    "В полях " + services + " указаны нечитаемые значения" :
                    "В поле " + services + " указано нечитаемое значение"
                ) + ". Продолжить?"
            );
        }

        static public void WaitingForResetting()
        {
            MessageBoxResult msg = MessageBox.Show(
                "Перенастройка кассы может несколько минут!\nОперация начнётся после нажатия на кнопку OK", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning
            );
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
    }
}
