

namespace ReadAndCoder
{
    internal class PartOfFile
    {
        private long _numberPart;

        private byte[] _partOfFile;

        internal PartOfFile(long numberPart, byte[] partOfFile)
        {
            _numberPart = numberPart;
            _partOfFile = partOfFile;
        }

        internal long GetNumberPart()
        {
            return _numberPart;
        }

        internal byte[] GetPartOfFile()
        {
            return _partOfFile;
        }
    }
}
