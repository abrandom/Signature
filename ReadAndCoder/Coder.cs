using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ReadAndCoder
{
    public class Coder: ICoder {
        private readonly int _sizePart;             // размер части
        private readonly string _filePath;          // путь
        private Signature _signature;      // объект с сигнатурой файла
        private BlockingCollection<PartOfFile> _queueParts;    

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

            int countConsumers = 1;//Environment.ProcessorCount;   // количество потоков-обработчиков соответствует количеству доступных процессоров        
            Task[] consumers = new Task[countConsumers];
            for (int i = 0; i < countConsumers; i++)
            {
                consumers[i] = Task.Factory.StartNew(CodeAndRecord);
            }

            try
            {
                producer.Wait();
                Task.WaitAll(consumers);
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                producer.Dispose();
                for (int i = 0; i < countConsumers; i++)
                {
                    consumers[i].Dispose();
                }
                _queueParts.Dispose();
            }
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
                    int readedBytes = codedFile.Read(buffer, 0, buffer.Length);

                    if (readedBytes == 0)
                    {
                        break;
                    }

                    ByteArrayPrinter(buffer);
                    _queueParts.Add(new PartOfFile(countOfParts++, buffer));
                }

                _queueParts.CompleteAdding();
            } 
  
            Console.WriteLine("File is Readed");
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
                        Console.WriteLine("File is coded");
                        return;
                    }

                    throw;
                }

                //ByteArrayPrinter(partOfFile.GetPartOfFile());

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
            SortedDictionary<Int64, string> hashStringSignature = new SortedDictionary<Int64, string>();
            
            foreach (KeyValuePair<long, byte[]> pair in GetByteArraySignature())
            {
                hashStringSignature.Add(pair.Key,
                    BitConverter.ToString(pair.Value).Replace("-", ""));
            }
            
            return hashStringSignature;
        }

        // для отладки
        private void ByteArrayPrinter(byte[] arrBytes)
        {
            for (int i = 0; i < 100; i++)
            {
                Console.Write(arrBytes[i] + " ");
            }
            Console.WriteLine("\n***********");
        }
    }
}
