using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IndexedBlobStore
{
    public class SHA1FileKeyGenerator : IFileKeyGenerator
    {
        public string GenerateKey(Stream stream)
        {
            stream.EnsureAtStart();
            using (var sha1Managed = new SHA1Managed())
            {
                var hashBytes = sha1Managed.ComputeHash(stream);
                var hashString = new StringBuilder(2 * hashBytes.Length);
                foreach (var b in hashBytes)
                {
                    hashString.AppendFormat("{0:X2}", b);
                }
                return hashString.ToString();
            }
        }
    }
}