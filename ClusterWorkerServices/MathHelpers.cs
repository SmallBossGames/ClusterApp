using System;
using System.Security.Cryptography;
using System.Text;

namespace ClusterWorkerServices
{
    public static class MathHelpers
    {
        public static bool IsPrime(long n)
        {
            for (var i = 2L; i <= Math.Sqrt(n); i++)
                if (n % i == 0)
                    return false;
            return true;
        }

        public static (int nonce, string hash) FindExpectedHash(byte[] sourceData, int hardLevel, int checkFrom, int checkTo)
        {
            using var mySHA256 = SHA256.Create();

            var buffer = new byte[sizeof(int) + sourceData.Length];

            Array.Copy(sourceData, 0, buffer, sizeof(int), sourceData.Length);

            for (int i = checkFrom; i < checkTo; i++)
            {
                BitConverter.TryWriteBytes(buffer, i);

                var hash = mySHA256.ComputeHash(buffer);

                if (CheckHash(hash, hardLevel))
                {
                    var hashString = PrintByteArray(hash);
                    return (i, hashString);
                }
            }

            return (0, string.Empty);
        }

        // Display the byte array in a readable format.
        public static string PrintByteArray(byte[] array)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append($"{array[i]:X2}");
                if ((i % 4) == 3) Console.Write(" ");
            }
            return sb.ToString();
        }

        private static bool CheckHash(byte[] hash, int maskBits)
        {
            for (int j = 0; j < hash.Length && maskBits != 0; j++)
            {
                var delta = maskBits > 8 ? 8 : maskBits;

                var result = hash[j] & (byte)((1 << delta) - 1);

                if (result != 0)
                {
                    return false;
                }

                maskBits -= delta;
            }

            return true;
        }
    }
}
