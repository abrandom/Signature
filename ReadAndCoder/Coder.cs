using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadAndCoder
{
    public class Coder {
        private readonly long _sizePart;            // размер части
        private readonly string _filePath;          // путь
        private Queue<Int64> _queueParts;           // очередь частей
        private Signature _signature;               // объект с сигнатурой файла
        private long _countParts;                   // количество частей
        private object _locker = new object();      // локер

        // в конструкторе указываем корректные путь к файлу и размер части в байтах
        // а так же инициализируем все необходимые параметры
        public Coder(string path, long part) {
            _filePath = path;
            _sizePart = part;

            _countParts = GetCountOfParts();
            _signature = new Signature(_countParts);
            _queueParts = QueuePartsCreator(_countParts);
        }

        public SortedDictionary<Int64, byte[]> CreateSignature()
        {
            int countThreads = 4;   // количество потоков-обработчиков
            
            // создаём потоки-обработчики на основе ф-ии Task и запускаем их
            Thread[] packetThreads = new Thread[countThreads];
            for (int i = 0; i < countThreads; i++)
            {
                packetThreads[i] = new Thread(Task);
                packetThreads[i].Start();
            }

            // ждем, пока отработают
            for (int i = 0; i < countThreads; i++)
            {
                packetThreads[i].Join();
            }

            return _signature.GetSignature();
        }

        // функция потока, которая берет из очереди номер части, 
        // читает из файла нужный кусок байт
        // и получает его SHA256-хэш
        private void Task()
        {
            long numberPart;                        // номер полученной части
           
            // бесконечный цикл, пока из очереди не вытащат все элементы
            while (true)
            {
                // изменять очередь частей может только один поток,
                // блокируем доступ остальным
                lock (_locker)
                {
                    // если в очереди больше нет элементов, то завершаем работу потоков
                    if (_queueParts.Count == 0)
                    {
                        return; // точка выхода
                    }

                    // забираем из очереди номер текущей части
                    numberPart = _queueParts.Dequeue();
                }
                // дальнейшие действия не требуют блокировки общего ресурса

                // читаем данные и отправляем их кодировщику
                CodeAndRecord(numberPart, ReadPartFile(numberPart));     
            }
        }

        // читаем часть файла
        private byte[] ReadPartFile(long numberPart)
        {
            int readedByte;                         // переменная, в которую считывается 1 байт
            byte[] buffer = new byte[_sizePart];    // массив для хранения байт в части

            using (FileStream codedFile = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            {
                // переходим на позицию для начала побайтового считывания
                codedFile.Seek(numberPart*_sizePart, SeekOrigin.Begin);

                // для каждой части заполняем буфер
                for (long i = 0; i < _sizePart; i++)
                {
                    // читаем байт
                    readedByte = codedFile.ReadByte();
                    // если он прочитан, то забрасываем в буфер
                    if (readedByte != -1)
                    {
                        buffer[i] = (byte) readedByte;
                    }
                    // иначе (в последней части количество байт может оказаться меньше, чем размер буфера)
                    // достигли конец части и добиваем буфер нулями
                    else
                    {
                        for (; i < _sizePart; i++)
                        {
                            buffer[i] = 0;
                        }
                    }
                }
            }

        return buffer;
        }

        // добавляем сигнатуру части в общий список
        private void CodeAndRecord(long numberPart, byte[] filePart)
        {
            SHA256 mySha256 = SHA256.Create();
            _signature.AddPartSignature(numberPart, mySha256.ComputeHash(filePart));
        }

        // создание очереди из частей
        private Queue<Int64> QueuePartsCreator(long count)
        {
            Queue<Int64> queueParts = new Queue<long>();

            for (long i = 0; i < count; i++)
            {
                queueParts.Enqueue(i);
            }

            return queueParts;
        } 

        // количество частей, на которые будем разбивать файл
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
