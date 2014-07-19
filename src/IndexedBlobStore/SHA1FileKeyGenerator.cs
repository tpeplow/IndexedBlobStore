using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IndexedBlobStore
{
    public class SHA1FileKeyGenerator : IFileKeyGenerator
    {
        public string GenerateKey(string fileName, Stream fileStream)
        {
            fileStream.EnsureAtStart();
            using (var sha1Managed = new SHA1Managed())
            {
                using (var fileNameStream = new MemoryStream())
                using (var streamWriter = new StreamWriter(fileNameStream))
                {
                    streamWriter.Write(fileName);
                    streamWriter.Flush();
                    fileNameStream.Position = 0;

                    var hashBytes = sha1Managed.ComputeHash(new StreamOfStreams(fileNameStream, fileStream));
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
}