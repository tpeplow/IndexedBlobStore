using System;
using System.Collections.Generic;
using System.IO;
using Machine.Specifications;

namespace IndexedBlobStore.Tests
{
    public class when_uploading_blob_that_does_not_already_exist : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            _contents = Guid.NewGuid().ToString();
            _contentsStream = CreateStream(_contents);
            _uploadedBlob = Client.CreateIndexedBlob("file", _contentsStream);
        };
        Because of = () =>
        {
            _uploadedBlob.Upload();
            _downloadedBlob = Client.GetIndexedBlob(_uploadedBlob.FileKey);
        };
        It can_be_looked_up_by_key = () => _downloadedBlob.ShouldNotBeNull();
        It can_download_contents = () => ReadStream(_downloadedBlob.OpenRead()).ShouldEqual(_contents);
        It should_include_sha1_as_the_key = () =>
        {
            using (var stream = _downloadedBlob.OpenRead())
                _uploadedBlob.FileKey.ShouldEndWith(new SHA1FileKeyGenerator().GenerateKey("file", stream));
        };
        It should_store_the_length = () => _uploadedBlob.Length.ShouldEqual(36);
        It should_store_the_file_name = () => _downloadedBlob.FileName.ShouldEqual("file");
        It should_have_an_empty_properties_dictionary = () => _downloadedBlob.Properties.Count.ShouldEqual(0);

        Cleanup clean = () => _uploadedBlob.Dispose();

        static IIndexedBlob _uploadedBlob;
        static IReadonlyIndexedBlob _downloadedBlob;
        static Stream _contentsStream;
        static string _contents;
    }

    public class when_uploading_blob_with_properties : IndexedBlobStoreTest
    {
        Because of = () =>
        {
            using (var blob = Client.CreateIndexedBlob("a file.txt", "blob with properties", CreateStream("contents with properties"), properties: new Dictionary<string, string> { { "Hello", "World" } }))
            {
                blob.Upload();
            }
            _downloadedBlob = Client.GetIndexedBlob("blob with properties");
        };

        It should_contain_the_properties = () => _downloadedBlob.Properties["Hello"].ShouldEqual("World");
        It should_fail_to_upload_if_you_only_change_properties = () =>
        {
            var exception = Catch.Exception(() =>
            {
                using (
                    var blob = Client.CreateIndexedBlob("a file.txt", "blob with properties",
                        CreateStream("contents with properties"),
                        properties: new Dictionary<string, string> { { "Goodbye", "Otherworld" } }))
                {
                    blob.Upload();
                }
            });
            exception.ShouldBeOfExactType<BlobAlreadyExistsException>();
        };

        static IReadonlyIndexedBlob _downloadedBlob;
    }

    public class when_uploading_blob_that_already_exists : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            using (var blob = Client.CreateIndexedBlob("file", CreateStream(_content)))
                blob.Upload();
        };
        Because of = () => _exception = Catch.Exception(() =>
        {
            _reference = Client.CreateIndexedBlob("file", CreateStream(_content));
            _reference.Upload();
        });
        It should_indicate_it_already_exists = () => _reference.Exists.ShouldBeTrue();
        It should_throw_blob_already_exists_exception_if_uploaded = () => _exception.ShouldBeOfExactType<BlobAlreadyExistsException>();

        static Exception _exception;
        static IIndexedBlob _reference;
        static string _content = "when uploading blob that already exists";
    }

    public class when_uploading_blob_that_already_exists_interleaved : IndexedBlobStoreTest
    {
        Because of = () => _exception = Catch.Exception(() =>
        {
            _reference = Client.CreateIndexedBlob("file", CreateStream(_content));

            using (var blob = Client.CreateIndexedBlob("file", CreateStream(_content)))
            {
                blob.Upload();
            }

            _reference.Upload();
        });
        It should_indicate_it_already_exists = () => _reference.Exists.ShouldBeTrue();
        It should_throw_blob_already_exists_exception_if_uploaded = () => _exception.ShouldBeOfExactType<BlobAlreadyExistsException>();

        static Exception _exception;
        static IIndexedBlob _reference;
        static string _content = "when uploading blob that already exists interleaved";
    }

    public class when_uploading_blob_that_already_exists_interleaved_with_duplicates : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            _options = new IndexedBlobStorageOptions()
            {
                AdditionalBlobsForLoadBalancing = 50
            };
        };

        Because of = () => _exception = Catch.Exception(() =>
        {
            _reference = Client.CreateIndexedBlob("file", CreateStream(_content), _options);

            using (var blob = Client.CreateIndexedBlob("file", CreateStream(_content), _options))
            {
                blob.Upload();
            }

            _reference.Upload();
        });
        It should_indicate_it_already_exists = () => _reference.Exists.ShouldBeTrue();
        It should_throw_blob_already_exists_exception_if_uploaded = () => _exception.ShouldBeOfExactType<BlobAlreadyExistsException>();

        static Exception _exception;
        static IIndexedBlob _reference;
        static string _content = "when uploading blob that already exists interleaved";
        static IndexedBlobStorageOptions _options;
    }

    public class when_uploading_blob_that_already_exists_interleaved_with_duplicates_async : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            _options = new IndexedBlobStorageOptions()
            {
                AdditionalBlobsForLoadBalancing = 50,
                UseBlobCopyAccrossStorageAccounts = true
            };
        };

        Because of = () => _exception = Catch.Exception(() =>
        {
            _reference = Client.CreateIndexedBlob("file", CreateStream(_content), _options);

            using (var blob = Client.CreateIndexedBlob("file", CreateStream(_content), _options))
            {
                blob.Upload();
            }

            _reference.Upload();
        });
        It should_indicate_it_already_exists = () => _reference.Exists.ShouldBeTrue();
        It should_throw_blob_already_exists_exception_if_uploaded = () => _exception.ShouldBeOfExactType<BlobAlreadyExistsException>();

        static Exception _exception;
        static IIndexedBlob _reference;
        static string _content = "when uploading blob that already exists interleaved";
        static IndexedBlobStorageOptions _options;
    }

    public class when_uploading_blob_using_custom_key : IndexedBlobStoreTest
    {
        Because of = () =>
        {
            using (var blob = Client.CreateIndexedBlob("file", "imported key", CreateStream("contents")))
                blob.Upload();
            _reference = Client.GetIndexedBlob("imported key");
        };
        It can_be_looked_up_by_key = () => _reference.ShouldNotBeNull();
        It can_download_contents = () => ReadStream(_reference.OpenRead()).ShouldEqual("contents");
        It should_use_provided_key = () => _reference.FileKey.ShouldEqual("imported key");

        static IReadonlyIndexedBlob _reference;
    }

    public class when_uploading_blob_requesting_copies_for_load_balancing : IndexedBlobStoreTest
    {
        Because of = () =>
        {
            TestContext.Current.CacheSettings.Enabled = false;
            using (
                var blob = Client.CreateIndexedBlob("file", CreateStream(_expectedContent),
                    new IndexedBlobStorageOptions { AdditionalBlobsForLoadBalancing = 2 }))
            {
                blob.Upload();
                _nonSpecificVersion = Client.GetIndexedBlob(blob.FileKey).OpenRead();
                _copy1 = Client.GetIndexedBlob(blob.FileKey).OpenRead(new IndexedBlobReadOptions { UseSpecificLoadBalancedBlob = 1 });
                _copyThatDoesNotExist = Client.GetIndexedBlob(blob.FileKey).OpenRead(new IndexedBlobReadOptions { UseSpecificLoadBalancedBlob = 3 });
                TestContext.Current.CacheSettings.Enabled = true;
            }
        };

        It should_download_from_a_random_copy_when_you_dont_sepcify_one = () => ReadStream(_nonSpecificVersion).ShouldEqual(_expectedContent);
        It should_not_complain_if_the_copy_you_request_does_not_exist = () => ReadStream(_copyThatDoesNotExist).ShouldEqual(_expectedContent);
        It should_let_user_download_specific_copy = () => ReadStream(_copy1).ShouldEqual(_expectedContent);

        static Stream _copy1;
        static Stream _copyThatDoesNotExist;
        static string _expectedContent = "load balanced";
        static Stream _nonSpecificVersion;
    }

    public class when_uploading_blob_with_subdirectories_in_file_name : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            _contents = Guid.NewGuid().ToString();
            _contentsStream = CreateStream(_contents);
            _uploadedBlob = Client.CreateIndexedBlob("folder\\file", _contentsStream);
        };
        Because of = () =>
        {
            _uploadedBlob.Upload();
            _downloadedBlob = Client.GetIndexedBlob(_uploadedBlob.FileKey);
        };
        It can_be_looked_up_by_key = () => _downloadedBlob.ShouldNotBeNull();
        It can_download_contents = () => ReadStream(_downloadedBlob.OpenRead()).ShouldEqual(_contents);
        It should_include_sha1_as_the_key = () =>
        {
            using (var stream = _downloadedBlob.OpenRead())
                _uploadedBlob.FileKey.ShouldEndWith(new SHA1FileKeyGenerator().GenerateKey("folder\\file", stream));
        };
        It should_store_the_length = () => _uploadedBlob.Length.ShouldEqual(36);
        It should_store_the_file_name = () => _downloadedBlob.FileName.ShouldEqual("folder\\file");
        It should_have_an_empty_properties_dictionary = () => _downloadedBlob.Properties.Count.ShouldEqual(0);

        Cleanup clean = () => _uploadedBlob.Dispose();

        static IIndexedBlob _uploadedBlob;
        static IReadonlyIndexedBlob _downloadedBlob;
        static Stream _contentsStream;
        static string _contents;
    }
}