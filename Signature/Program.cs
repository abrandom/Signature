using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ReadAndCoder;

namespace Signaturer
{
    class Program
    {

        private static long _sizePart;   // размер части
        private static String _filePath;    // путь

        static void Main(string[] args)
        {
            // вводим путь к файлу
            Console.Write("Press enter the path of file: ");
            _filePath = Console.ReadLine();

            // выход, если файла не существует
            if (!File.Exists(_filePath)) {
                Console.WriteLine("File not found. Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // вводим размер части
            Console.Write("Press enter size of part in Byte: ");
            
            // выход, если некорректно ввели размер части
            if (!Int64.TryParse(Console.ReadLine(), out _sizePart))
            {
                Console.WriteLine("Invalid size of part. Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // создаём объект кодировщика и получаем сигнатуру файла
            Coder coder = new Coder(_filePath, _sizePart);
            SortedDictionary<Int64, byte[]> signaturs = coder.CreateSignature();

            // вывод на экран
            foreach (long parts in signaturs.Keys)
            {
                Console.Write(parts + ": ");
                foreach (byte b in signaturs[parts])
                {
                    Console.Write(b);
                }
                Console.WriteLine();
            }

            



            Console.WriteLine("Successful. Press any key to exit.");
            Console.ReadKey();

            
        }
    }
}
