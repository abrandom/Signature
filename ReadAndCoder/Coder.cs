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
        private Signature _signature;               // объект с сигнатурой файла
        private BlockingCollection<PartOfFile> _queueParts;
        private bool _isSignatureCreated;

        // указываем корректные путь к файлу и размер части в байтах,
        // инициализируем все необходимые параметры
        public Coder(string path, int part)
        {
            _filePath = path;
            _sizePart = part;

            _queueParts = new BlockingCollection<PartOfFile>();
            _signature = new Signature();
        }

        public void CreateSignature()
        {
            Task producer = null;
            Task[] consumers = null;
            int countConsumers = Environment.ProcessorCount;   // количество потоков-обработчиков соответствует количеству доступных процессоров        

            try
            {
                producer = Task.Factory.StartNew(ReadFile);
                
                consumers = new Task[countConsumers];
                for (int i = 0; i < countConsumers; i++)
                {
                    consumers[i] = Task.Factory.StartNew(CodeAndRecord);
                }

                producer.Wait();
                Task.WaitAll(consumers);
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                if (producer != null)
                {
                    producer.Dispose();
                }
                
                for (int i = 0; i < countConsumers; i++)
                {
                    if (consumers != null && consumers[i] != null)
                    {
                        consumers[i].Dispose();
                    }
                    
                }

                if (_queueParts != null)
                {
                    _queueParts.Dispose();
                }
            }

            _isSignatureCreated = true;
        }

        // читаем файл блоками
        private void ReadFile()
        {
            long countOfParts = 0;
            byte[] buffer = new byte[_sizePart];
            
            using (FileStream codedFile = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            {
                int readedBytes;

                while ((readedBytes = codedFile.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] readedByteArray = new byte[readedBytes];
                    Array.Copy(buffer, readedByteArray, readedBytes);

                    _queueParts.Add(new PartOfFile(countOfParts++, readedByteArray));
                }

                _queueParts.CompleteAdding();
            } 
  
            Console.WriteLine("File is Readed");
        }

        // добавляем сигнатуру части в общий список
        private void CodeAndRecord()
        {
            PartOfFile partOfFile;
           
            while (!_queueParts.IsCompleted)
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
            IsSignatureNotCreated();

            return _signature.GetSignature(); 
        }

        public SortedDictionary<Int64, string> GetHashStringSignature()
        {
            IsSignatureNotCreated();
            
            SortedDictionary<Int64, string> hashStringSignature = new SortedDictionary<Int64, string>();
            
            foreach (KeyValuePair<long, byte[]> pair in GetByteArraySignature())
            {
                hashStringSignature.Add(pair.Key,
                    BitConverter.ToString(pair.Value).Replace("-", ""));
            }
            
            return hashStringSignature;
        }

        private void IsSignatureNotCreated()
        {
            if (!_isSignatureCreated)
            {
                throw new MissingMethodException("The signature has not been created");
            }
        }
    }
}
