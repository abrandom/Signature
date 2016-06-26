using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ReadAndCoder
{
    public class Coder {
        private readonly long _sizePart;   // размер части
        private readonly String _filePath;    // путь

        // в конструкторе указываем корректные путь к файлу и размер части в байтах
        public Coder(String path, long part) {
            _filePath = path;
            _sizePart = part;
        }

        public void GetSignature() {
            int b = 0;  // переменная для считывания байт
            byte[] buffer = new byte[_sizePart];    // массив для хранения байт в части

            using (FileStream codedFile = new FileStream(_filePath, FileMode.Open, FileAccess.Read)) {
                for (long i = 0; i < GetCountOfParts(); i++) {
                    long position = codedFile.Seek(i * _sizePart, SeekOrigin.Begin);

                    // для каждой части заполняем буфер
                    for (long j = position; j < position + _sizePart; j++) {
                        // читаем байт из пачки
                        b = codedFile.ReadByte();
                        // если он прочитан, то зибрасываем в буфер
                        if (b != -1) {
                            buffer[j - position] = (byte)b;
                        }
                        // иначе (в последней части байт может оказаться меньше, чем размер части)
                        // достигли конец части и добиваем буфер нулями
                        else {
                            for (; j < position + _sizePart; j++) {
                                buffer[j - position] = 0;
                            }
                        }
                    }

                    // тут работаем с заполненным буфером
                    SHA256 mySha256 = SHA256.Create();
                    Console.Write("Part N" + i + "\tSHA256: ");
                    foreach (byte bt in mySha256.ComputeHash(buffer)) {
                        Console.Write(bt);
                    }
                    Console.WriteLine();
                }
            }
        }

        // получаем количество частей
        private long GetCountOfParts() {
            FileInfo codedFile = new FileInfo(_filePath);
            long sizeFile = codedFile.Length;
            long countOfParts = sizeFile / _sizePart;

            // если размер файла не кратен размеру части, то
            // добавляем часть на отброшенный при делении остаток
            if (sizeFile % _sizePart != 0) {
                countOfParts++;
            }

            return countOfParts;
        }
    }
}
