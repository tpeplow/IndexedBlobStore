using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;

namespace IndexedBlobStore.Tests
{
    public class when_tagging_a_blob : IndexedBlobStoreTest
    {
        Establish context = () => _blob = UploadUniqueBlob();
        Because of = () =>
        {
            _blob.AddTag(new IndexedBlobTag("test tag", "file.txt"));
            _searchResult = Client.Find("test tag");
        };
        It should_find_blob_matching_tag = () => _searchResult.SingleOrDefault().ShouldNotBeNull();
        It should_return_the_filename_associated_with_the_tag = () => _searchResult.Single().Tag.FileName.ShouldEqual("file.txt");
        It should_return_the_file = () => _searchResult.Single().Blob.FileKey.ShouldEqual(_blob.FileKey);
        It should_return_the_file_size = () => _searchResult.Single().Blob.Length.ShouldEqual(36);

        static IIndexedBlob _blob;
        static IEnumerable<TaggedIndexedBlob> _searchResult;
    }

    public class when_the_same_blob_has_multiple_tags : IndexedBlobStoreTest
    {
        Establish context = () => _blob = UploadUniqueBlob();
        Because of = () =>
        {
            _blob.AddTag(new IndexedBlobTag("tag 1", "file.txt"));
            _blob.AddTag(new IndexedBlobTag("tag 2", "file.txt"));
            _tag1Search = Client.Find("tag 1");
            _tag2Search = Client.Find("tag 2");
        };

        It should_be_found_using_all_tags = () =>
        {
            _tag1Search.Count().ShouldEqual(1);
            _tag2Search.Count().ShouldEqual(1);
        };

        static IIndexedBlob _blob;
        static IEnumerable<TaggedIndexedBlob> _tag1Search;
        static IEnumerable<TaggedIndexedBlob> _tag2Search;
    }

    public class when_duplicate_tag_is_added_to_blob : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            _blob = UploadUniqueBlob();
            _blob.AddTag(new IndexedBlobTag("tom", "file.txt"));
        };

        Because of = () => _exception = Catch.Exception(() => _blob.AddTag(new IndexedBlobTag("tom", "file.txt")));

        It shold_throw_duplicate_tag_exception = () => _exception.ShouldBeOfExactType<DuplicateTagException>();

        static IIndexedBlob _blob;
        static Exception _exception;
    }

    public class when_multiple_blobs_have_the_same_tag : IndexedBlobStoreTest
    {
        Establish context = () => _blobs = Enumerable.Range(0, 2).Select(x => UploadUniqueBlob()).ToArray();

        Because of = () =>
        {
            foreach (var blob in _blobs)
            {
                blob.AddTag(new IndexedBlobTag("multi blobs", "file.txt"));
            }
            _searchResults = Client.Find("multi blobs");
        };

        It should_match_all_blobs_with_tag = () => _searchResults.Select(x => x.Blob.FileKey).ShouldContainOnly(_blobs.Select(x => x.FileKey));

        static IIndexedBlob[] _blobs;
        static IEnumerable<TaggedIndexedBlob> _searchResults;
    }
}