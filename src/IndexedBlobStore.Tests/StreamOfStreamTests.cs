using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Machine.Specifications;

namespace IndexedBlobStore.Tests
{
    public class StreamOfStreamTests : IndexedBlobStoreTest
    {
        Because of = () =>
        {
            var streams = new StreamOfStreams(_sourceStreams);
            using (var streamReader = new StreamReader(streams))
            {
                _combinedStreams = streamReader.ReadToEnd();
            }
        };

        public class when_all_streams_are_small
        {
            Establish context = () => _sourceStreams = new[]
            {
                CreateStream("hello "),
                CreateStream("world")
            };

            It should_combine = () => _combinedStreams.ShouldEqual("hello world");
        }

        public class when_source_streams_cannot_be_read_in_single_read
        {
            Establish context = () =>
            {
                _contents = Enumerable.Range(0, 1025).Select(x => x.ToString()).Aggregate((x, y) => x += y);
                _sourceStreams = new[]
                {
                    CreateStream(_contents),
                    CreateStream(" hello "),
                    CreateStream(_contents)
                };
            };

            It should_combine = () => _combinedStreams.ShouldEqual(_contents + " hello " + _contents);

            static string _contents;
        }

        static Stream[] _sourceStreams;
        static string _combinedStreams;
    }
}