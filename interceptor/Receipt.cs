using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace interceptor
{
    class Receipt
    {
        const int MARGIN_TOP = 50;
        const int MARGIN_LEFT = 30;
        const int LINE_HEIGHT = 15;

        static int[] COLUMN_WIDTH = { 0, 20, 250, 50, 60, 75 };
        static int CURRENT_LINE = 0;
        static int CURRENT_ROW = 0;

        static Document document;
        static PdfContentByte cb;

        public static void printReceipt()
        {
            document = new Document();
            FileStream fs = new FileStream("newFile.pdf", FileMode.Create, FileAccess.Write);
            PdfWriter writer = PdfWriter.GetInstance(document, fs);
            document.Open();

            cb = writer.DirectContent;

            BaseFont bf = BaseFont.CreateFont("FONT.TTF", Encoding.GetEncoding(1251).BodyName, BaseFont.NOT_EMBEDDED);
            Font font = new Font(bf, Font.DEFAULTSIZE, Font.NORMAL);

            cb.SetColorFill(BaseColor.BLACK);
            cb.SetFontAndSize(bf, 10);

            AddTitle("Акт N 001/2018/11/09/0001/ДОП");
            AddTitle("выполненных работ на дополнительные услуги");

            AddText("г.Москва");
            AddText("09/11/2018", x: 520, y: CurrentY(), noNewLine: true);

            AddText();

            AddText("Заявитель: Петров Иван Иванович (номер записи 001/2018/11/09/0001)");
            AddText("Исполнитель: ООО Виза Менеджмент Сервич");

            AddText("Оказаны следующие виды работ:");

            AddRow("п/п", "Наименование работы(услуги)", "Ед.изм.", "Кол-во", "Цена", "Сумма", header: true, aligment: 1);
            AddRow("", "", "", "", "(с учетом", "(с учетом", withBox: false, header: true, aligment: 1);
            AddRow("", "", "", "", "НДС 18%), руб", "НДС 18%), руб", withBox: false, header: true, aligment: 1);

            AddRow("N", "Услуга фотографирования", "шт", "1", "100", "100");
            AddRow("N", "Услуга ксерокопирования", "шт", "2", "200", "400");
            AddRow("N", "Услуга заполнения анкеты", "шт", "2", "300", "600");
            AddRow("N", "Услуга распечатки документов", "шт", "2", "400", "800");

            AddText();

            AddText("ИТОГО: Тысяча семьсот рублей, в т.ч. НДС 18% сто рублей");

            AddText();
            AddText();
            AddText("М.П.");
            AddText("Сдал:", x: 200, y: CurrentY(), noNewLine: true);
            AddText("Принял:", x: 350, y: CurrentY(), noNewLine: true);

            document.Close();
            fs.Close();
            writer.Close();
        }

        static public void AddTextBox(bool header = false)
        {
            int columnHeight = (header ? 45 : 15);

            float yPos = CurrentY() + 5;
            float xPosLeft = MARGIN_LEFT + 3;
            float xPosRight = document.PageSize.Width - MARGIN_LEFT;

            AddLine(xPosLeft, yPos, xPosRight, yPos);
            AddLine(xPosLeft, yPos, xPosLeft, (yPos + columnHeight));
            AddLine(xPosRight, yPos, xPosRight, (yPos + columnHeight));

            float xPosColumn = xPosLeft;

            foreach (int size in COLUMN_WIDTH)
            {
                xPosColumn += size;
                AddLine(xPosColumn, yPos, xPosColumn, (yPos + columnHeight));
            }

            yPos += columnHeight;
            AddLine(xPosLeft, yPos, xPosRight, yPos);
        }

        static public float CurrentY()
        {
            return MARGIN_TOP + CURRENT_LINE * LINE_HEIGHT;
        }

        static public void AddLine(float x1, float y1, float x2, float y2)
        {
            cb.SetLineWidth(0.4);
            cb.MoveTo(x1, document.PageSize.Height - y1);
            cb.LineTo(x2, document.PageSize.Height - y2);
            cb.Stroke();
        }

        static public void AddTitle(string text, float y = 0)
        {
            AddText(text, (document.PageSize.Width / 2), y, 1);
        }

        static public void AddRow(string num, string service, string unit, string quantity,
            string price, string total, bool header = false, bool withBox = true, int aligment = 0)
        {
            if (num == "N")
            {
                CURRENT_ROW += 1;
                AddText(CURRENT_ROW.ToString(), withBox: withBox, header: header);
            }
            else
                AddText(num, withBox: withBox, header: header);

            AddText(service, column: 1, aligment: aligment, header: header);
            AddText(unit, column: 2, aligment: aligment, header: header);
            AddText(quantity, column: 3, aligment: aligment, header: header);
            AddText(price, column: 4, aligment: aligment, header: header);
            AddText(total, column: 5, aligment: aligment, header: header);
        }

        static public void AddText(string text = "", float x = 0, float y = 0, int aligment = 0,
            bool noNewLine = false, bool withBox = false, int column = -1, bool header = false)
        {
            if (withBox) AddTextBox(header);

            if (!noNewLine && column < 0) CURRENT_LINE += 1;

            float yPos = (y == 0 ? CurrentY() : y);
            float xPos = (x == 0 ? MARGIN_LEFT : x);

            if (column == -1 && header) xPos -= 4;

            if (column >= 0)
            {
                xPos += (header ? 15 : 10);

                for (int a = 0; a <= column; a++)
                    xPos += COLUMN_WIDTH[a];

                if (header && column >= 0 && (column + 1 < COLUMN_WIDTH.Length))
                    xPos += (COLUMN_WIDTH[column + 1] / 2) - 10;
                else if (header && column + 1 == COLUMN_WIDTH.Length)
                    xPos = (document.PageSize.Width - (COLUMN_WIDTH[column] / 2) - MARGIN_LEFT);
            }

            if (withBox) xPos += 10;

            cb.BeginText();
            cb.ShowTextAligned(aligment, text, xPos, (document.PageSize.Height - yPos), 0);
            cb.EndText();
        }
    }
}
