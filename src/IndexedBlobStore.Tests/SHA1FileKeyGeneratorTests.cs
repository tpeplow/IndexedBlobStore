using System.Security.Cryptography;
using System.Text;
using Machine.Specifications;

namespace IndexedBlobStore.Tests
{
    public class when_computing_sha1_of_file : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            var hashBytes = new SHA1Managed().ComputeHash(CreateStream("filenamecontents"));
            var hashString = new StringBuilder(2 * hashBytes.Length);
            foreach (var b in hashBytes)
            {
                hashString.AppendFormat("{0:X2}", b);
            }
            _expectedSha = hashString.ToString();
        };

        Because of = () => _result = new SHA1FileKeyGenerator().GenerateKey("filename", CreateStream("contents"));

        It should_compute_a_hash_using_the_filename_and_contexnts = () => _result.ShouldEqual(_expectedSha);

        static string _expectedSha;
        static string _result;
    }
}