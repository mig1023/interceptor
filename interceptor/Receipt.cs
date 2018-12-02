﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text.RegularExpressions;

namespace interceptor
{
    class Receipt
    {
        const int MARGIN_TOP = 15;
        const int MARGIN_LEFT = 30;
        const int LINE_HEIGHT = 13;

        static int[] COLUMN_WIDTH = { 0, 20, 250, 50, 60, 75 };
        static int CURRENT_LINE = 0;
        static int CURRENT_ROW = 0;

        static Document document;
        static PdfContentByte cb;

        public static string appNumber(string appNum)
        {
            Match ReqMatch = Regex.Match(appNum, @"^(\d{3})(\d{4})(\d{2})(\d{2})(\d{4})$");

            return ReqMatch.Groups[1].Value + "/" + ReqMatch.Groups[2].Value + "/" +
                ReqMatch.Groups[3].Value + "/" + ReqMatch.Groups[4].Value + "/" +
                ReqMatch.Groups[5].Value;
        }

        public static void printReceipt(string appDataString, DocPack doc)
        {
            string[] appData = appDataString.Split('|');

            if (appData[0] != "OK")
            {
                Log.addWeb("Ошибка вернувшихся данных записи");
                return;
            }

            int receiptIndex = int.Parse(appData[5]) + 1;

            string fileName = appData[2] + "_D_" + receiptIndex.ToString() + ".pdf";

            document = new Document();
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            PdfWriter writer = PdfWriter.GetInstance(document, fs);
            document.Open();

            cb = writer.DirectContent;

            BaseFont bf = BaseFont.CreateFont("FONT.TTF", Encoding.GetEncoding(1251).BodyName, BaseFont.NOT_EMBEDDED);

            cb.SetColorFill(BaseColor.BLACK);
            cb.SetFontAndSize(bf, 10);

            for (int i = 0; i < 2; i++)
            {
                if (i > 0)
                {
                    AddText();
                    AddText();
                    AddText(
                        "- - - - - - - - - - - - - - - - - - - - - - -" +
                        " линия разрыва " +
                        "- - - - - - - - - - - - - - - - - - - - - - -" +
                        " линия разрыва " +
                        "- - - - - - - - - - - - - - - - - - - - - - -"
                    );
                    AddText();
                    AddText();

                    CURRENT_ROW = 0;
                }

                AddTitle("Акт N " + appNumber(appData[2]) + "/ДОП" + receiptIndex.ToString());
                AddTitle("выполненных работ на дополнительные услуги");

                AddText("г.Москва");
                AddText(DateTime.Now.ToString("dd MMMM yyyy"), x: 500, y: CurrentY(), noNewLine: true);

                AddText();

                AddText("Заявитель: " + appData[1] + " (номер записи " + appNumber(appData[2]) + ")");
                AddText("Исполнитель: ООО \"Виза Менеджмент Сервиc\"");

                AddText("Оказаны следующие виды работ:");

                AddRow("", "", "", "", "Цена", "Сумма", header: true, aligment: 1);
                AddRow("    " + "п/п", "Наименование работы(услуги)", "Ед.изм.", "Кол-во", "(с учетом", "(с учетом", withBox: false, header: true, aligment: 1);
                AddRow("", "", "", "", "НДС 18%), руб", "НДС 18%), руб", withBox: false, header: true, aligment: 1);

                foreach (Service service in doc.Services)
                {
                    decimal total = service.Price * service.Quantity;

                    AddRow("N", service.Name, "шт", service.Quantity.ToString(),
                        service.Price.ToString(), total.ToString());
                }

                AddText();
                AddText("ИТОГО : " + Cashbox.manDocPackSumm + " руб", x: 450, y: CurrentY(), noNewLine: true);

                AddText("Услуги оказаны в полном объеме и в срок.");
                AddText("Услуги оплачены Заказчиком в сумме: " + appData[3].ToLower());
                AddText("в т.ч. НДС 18% " + appData[4].ToLower());
                AddText("Стороны друг к другу претензий не имеют.");

                AddText();
                AddText();
                AddText("Подписи сторон:");
                AddText();
                AddText("М.П.");
                AddText();
                AddText(
                    "Сдал: ___________________ / ______________________ /"
                    + "       " +
                    "Принял: ____________________ / _____________________ /"
                );

                AddText();
                AddText("(ФИО)", x: 175, y: CurrentY(), noNewLine: true);
                AddText("(ФИО)", x: 460, y: CurrentY(), noNewLine: true);
            }


            document.Close();
            fs.Close();
            writer.Close();

            System.Diagnostics.Process.Start(fileName);

            CURRENT_LINE = 0;
            CURRENT_ROW = 0;

            Log.add("Сформирован акт " + fileName);

            CRM.sendFile(fileName, appData[6]);
        }

        static public void AddTextBox(bool header = false)
        {
            int columnHeight = (header ? LINE_HEIGHT*3 : LINE_HEIGHT);

            float yPos = CurrentY() + 3;
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
