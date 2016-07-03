using System;
using System.Collections.Generic;

namespace ReadAndCoder
{
    interface ICoder
    {
        // получаем словарь с сигнатурой "как есть"
        SortedDictionary<Int64, byte[]> GetByteArraySignature();

        // получаем словарь с шестнадцатиричным строковым представлением сигнатуры
        SortedDictionary<Int64, string> GetHashStringSignature();
    }
}
