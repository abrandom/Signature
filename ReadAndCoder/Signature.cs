using System;
using System.Collections.Generic;

namespace ReadAndCoder {
    internal class Signature
    {
        //для хранения сигнатуры используем словарь с ключами-номерами частей
        //и значениями-байтовыми хеш-массивами
        private SortedDictionary<Int64, byte[]> _signature;
        private object _locker = new object();  

        internal Signature()
        {
            _signature = new SortedDictionary<long, byte[]>();
        }

        //возвращаем наш словарь c сигнатурами
        internal SortedDictionary<Int64, byte[]> GetSignature()
        {
            return _signature;
        }

        //для указанной части добавляем байтовую сигнатуру
        internal void AddPartSignature(long part, byte[] signBytes)
        {
            //в один момент времени писать может только один поток
            lock (_locker)
            {
                _signature.Add(part, signBytes);
            }
        }

    }
}
