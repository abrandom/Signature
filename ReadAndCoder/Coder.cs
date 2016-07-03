using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ReadAndCoder
{
    public class Coder: ICoder {
        private readonly int _sizePart;             // размер части
        private readonly string _filePath;          // путь
        private readonly Signature _signature;      // объект с сигнатурой файла
        private readonly BlockingCollection<PartOfFile> _queueParts;    

        // в конструкторе указываем корректные путь к файлу и размер части в байтах,
        // проверяем данные на корректность,
        // а так же инициализируем все необходимые параметры
        public Coder(string path, int part)
        {
            _filePath = path;
            _sizePart = part;

            _queueParts = new BlockingCollection<PartOfFile>();
            _signature = new Signature();

            CreateSignature();
        }

        private void CreateSignature()
        {
            Task producer = Task.Factory.StartNew(ReadFile);

            int countConsumers = Environment.ProcessorCount;   // количество потоков-обработчиков соответствует количеству доступных процессоров        
            Task[] consumers = new Task[countConsumers];
            for (int i = 0; i < countConsumers; i++)
            {
                consumers[i] = Task.Factory.StartNew(CodeAndRecord);
            }

            Task.WaitAll(consumers);
        }

        // читаем файл блоками
        private void ReadFile()
        {
            long countOfParts = 0;
            byte[] buffer = new byte[_sizePart];
            
            using (FileStream codedFile = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            {
                while (true)
                {
                    int readedBytes = codedFile.Read(buffer, 0, _sizePart);

                    if (readedBytes == 0)
                    {
                        break;
                    }

                    _queueParts.Add(new PartOfFile(countOfParts++, buffer));
                }

                _queueParts.CompleteAdding();
            }   
        }

        // добавляем сигнатуру части в общий список
        private void CodeAndRecord()
        {
            PartOfFile partOfFile;

            while (true)
            {
                try
                {
                    partOfFile = _queueParts.Take();
                }
                catch (InvalidOperationException)
                {
                    if (_queueParts.IsAddingCompleted)
                    {
                        return;
                    }

                    throw;
                }

                SHA256 mySha256 = SHA256.Create();

                _signature.AddPartSignature(partOfFile.GetNumberPart(),
                    mySha256.ComputeHash(partOfFile.GetPartOfFile()));
            }
        }

        public SortedDictionary<Int64, byte[]> GetByteArraySignature()
        {
            return _signature.GetSignature(); 
        }

        public SortedDictionary<Int64, string> GetHashStringSignature()
        {
            ConcurrentDictionary<Int64, string> hashStringConcurrentDictionary = new ConcurrentDictionary<long, string>();

            // т.к. при малых размерах "блоков" и большом размере файла количество хеш-блоков может оказаться достаточно большим,
            // распараллеливаем задачу преобразования в строку
            Parallel.ForEach(GetByteArraySignature().AsParallel(), pair =>
            {
                hashStringConcurrentDictionary.TryAdd(pair.Key,
                    BitConverter.ToString(pair.Value).Replace("-", ""));
            });
            
            // создаём отсортированный словарь на основе конкурентного
            SortedDictionary<Int64, string> hashStringSignature = new SortedDictionary<Int64, string>(hashStringConcurrentDictionary);
            
            return hashStringSignature;
        }
    }
}
