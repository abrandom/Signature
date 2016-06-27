using System;
using System.Collections.Generic;
using System.IO;
using ReadAndCoder;

namespace Signature
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // проверяем наличие обоих аргументов
                if (args.Length != 2)
                {
                    throw new ArgumentException();
                }

                // создаём объект кодировщика и получаем сигнатуру файла
                Coder coder = new Coder(args[0], args[1]);
                SortedDictionary<long, byte[]> signaturs = coder.CreateSignature();

                // распечатываем сигнатуры
                Console.WriteLine(HexSignatureConverter(signaturs));

                Console.Write("Successful. ");
            }
            // ошибки неверно введенных данных
            catch (ArgumentException)
            {
                Console.WriteLine("Illegal argiments!\n" +
                                  "Used: Signeture.exe File_Name Size_of_part_in_Bytes\n");
            }
            // ошибки ввода-вывода
            catch (IOException e)
            {
                Console.WriteLine("I/O exception: " +
                                  e.Message + "\n" +
                                  e.StackTrace);
            }
            // другие ошибки
            catch (Exception e)
            {
                Console.WriteLine("Unknow exception: " +
                                  e.Message + "\n" +
                                  e.StackTrace);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        // преобразуем байтовый массив сигнатур в эквивалентное ему шестнадцатеричное строковое представление.
        private static string HexSignatureConverter(SortedDictionary<long, byte[]> signaturs)
        {
            string resultString = "\n";

            // создаём строку с hex-представлением массива байт
            foreach (long parts in signaturs.Keys) {
                resultString += parts + ": " + BitConverter.ToString(signaturs[parts]) + "\n";
            }

            return resultString.Replace("-", "");
        }
    }
}
