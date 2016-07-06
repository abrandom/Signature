using System;

namespace MyProgressBar
{
    public class MyProgressBars
    {
        public static void DravTextProgressBar(long progress, long total) {
            Console.CursorLeft = 0;
            Console.Write("[");
            Console.CursorLeft = 41;
            Console.Write("]");
            Console.CursorLeft = 1;
            double onePies = 40.0 / total;

            //рисуем заполненные поля
            int position = 1;
            for (int i = 0; i < onePies * progress; i++) {
                Console.CursorLeft = position++;
                Console.Write("=");
            }

            //рисуем незаполненные поля
            for (int i = position; i < 41; i++) {
                Console.CursorLeft = position++;
                Console.Write(".");
            }

            //рисуем проценты
            Console.CursorLeft = 43;
            Console.Write(100 * progress / total + "%");
        }
    }
}
