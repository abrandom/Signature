using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MyProgressBar;


namespace ReadAndCoder
{
    public class Coder: ICoder {
        private readonly int _sizePart;             //размер части
        private readonly string _filePath;          //путь
        private Signature _signature;               //объект с сигнатурой файла
        private BlockingCollection<PartOfFile> _queueParts;
        private bool _isSignatureCreated;
        private long _steps;                         

        //указываем корректные путь к файлу и размер части в байтах,
        //инициализируем все необходимые параметры
        public Coder(string path, int part)
        {
            _filePath = path;
            _sizePart = part;

            _steps = 0;

            _queueParts = new BlockingCollection<PartOfFile>();
            _signature = new Signature();
        }

        public void CreateSignature()
        {
            //реализован шаблон поставщик-потребители
            //распараллеливание потребления поручим системе

            try
            {
                Parallel.Invoke(ReadFile, CodedProgressBar, CodeAndRecord);
            }
            finally
            {
                if (_queueParts != null)
                {
                    _queueParts.Dispose();
                }
            }

            _isSignatureCreated = true;
        }

        //читаем файл блоками и добавляем в очередь
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
        }

        //берем блок из очереди, кодируем и добавляем сигнатуру части в общий список
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
                        return;
                    }

                    throw;
                }

                SHA256 mySha256 = SHA256.Create();

                _signature.AddPartSignature(partOfFile.GetNumberPart(),
                    mySha256.ComputeHash(partOfFile.GetPartOfFile()));

                //увеличиваем счетчик шагов для прогрессбара
                Interlocked.Increment(ref _steps);
            }
        }

        private void CodedProgressBar()
        {
            //находим количество шагов (размер_файла / размер_блока)
            long countOfSteps = new FileInfo(_filePath).Length / _sizePart;

            //циклично прорисовываем наш прогрессбар
            do
            {
                Thread.Sleep(100);
                MyProgressBars.DravTextProgressBar(Interlocked.Read(ref _steps), Interlocked.Read(ref countOfSteps));
            } while (Interlocked.Read(ref _steps) < Interlocked.Read(ref countOfSteps));
            Console.WriteLine();
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

        //проверяет наличие сигнатуры
        private void IsSignatureNotCreated()
        {
            if (!_isSignatureCreated)
            {
                throw new MissingMethodException("The signature has not been created");
            }
        }
    }
}
