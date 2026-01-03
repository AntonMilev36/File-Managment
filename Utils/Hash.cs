namespace FileManagment.Utils
{
    public static class Hash
    {
        public static long CalculateCheckSum(byte[] data)
        {
            long checksum = 0;
            foreach (byte b in data)
                checksum = (checksum + b) % long.MaxValue;
            return checksum;
        }

        public static long CalculateChecksumIncremental(long current, byte b)
        {
            return (current + b) % long.MaxValue;
        }
    }
}
