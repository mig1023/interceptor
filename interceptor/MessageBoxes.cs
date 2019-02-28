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
        static public MessageBoxResult NullSummCash()
        {
            return MessageBox.Show(
                "Введена нулевая сумма оплаты чека. Продолжить?", "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );
        }

        static public MessageBoxResult NullReturnDate()
        {
            return MessageBox.Show(
                "Дата договора не введена, будет использована текущая. Продолжить?", "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );
        }

        static public MessageBoxResult NullInServices(string services)
        {
            return MessageBox.Show(
                "Услуги не имеют цену по прайслисту выбранного центра: " +
                services + ". " +
                "Такие услуги не будут отображены в чеке. Продолжить?",
                "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );
        }

        static public MessageBoxResult ServSummEmpty(string services)
        {
            bool many = (services.IndexOf(',') >= 0 ? true : false);

            return MessageBox.Show(
                (
                    many ?
                    "Выбраны услуги " + services + ". Но суммы оставлены нулевыми" :
                    "Выбрана услуга " + services + ". Но сумма оставлена нулевой"
                ) + ". Продолжить?", "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );
        }

        static public MessageBoxResult ServNoClick(string services)
        {
            bool many = (services.IndexOf(',') >= 0 ? true : false);

            return MessageBox.Show(
                (
                    many ?
                    "Указаны суммы для " + services + ". Но эти услуги не выбраны" :
                    "Указана сумма для " + services + ". Но эта услуга не выбрана"
                ) + ". Продолжить?", "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );
        }

        static public MessageBoxResult ServFieldFail(string services)
        {
            bool many = (services.IndexOf(',') >= 0 ? true : false);

            return MessageBox.Show(
                (
                    many ? 
                    "В полях " + services + " указаны нечитаемые значения" :
                    "В поле " + services + " указано нечитаемое значение"
                ) +                
                ". Продолжить?", "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );
        }
    }
}
