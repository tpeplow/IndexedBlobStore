using System;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage;

namespace IndexedBlobStore.Tests
{
    public class when_blob_upload_fails_with_bad_request
    {
        Establish context = () => _uploadAttempts = 0;
        Because of = () => _exception = Catch.Exception(() =>  ReliableCloudOperations.UploadBlob(() =>
        {
            _uploadAttempts++;
            throw new StorageException(new RequestResult { HttpStatusCode = 400 }, "request bad", new Exception("inside you"));
        }));
        
        It should_retry_5_times = () => _uploadAttempts.ShouldEqual(6);
        It should_throw_the_exception = () => _exception.ShouldNotBeNull();

        static int _uploadAttempts;
        static Exception _exception;
    }

    public class when_blob_upload_fails_with_precondition_failed
    {
        Establish context = () => _uploadAttempts = 0;
        Because of = () => _exception = Catch.Exception(() => ReliableCloudOperations.UploadBlob(() =>
        {
            _uploadAttempts++;
            throw new StorageException(new RequestResult { HttpStatusCode = 412 }, "preconditions not met", new Exception("inside you"));
        }));

        It should_NOT_retry = () => _uploadAttempts.ShouldEqual(1);
        It should_throw_the_exception = () => _exception.ShouldNotBeNull();

        static int _uploadAttempts;
        static Exception _exception;
    }

    public class when_operation_has_some_other_storage_exception
    {
        Because of = () => _exception = Catch.Exception(() => ReliableCloudOperations.UploadBlob(() =>
        {
            throw new StorageException(new RequestResult { HttpStatusCode = 500 }, "internal", new Exception("inside you"));
        }));

        It should_throw = () => _exception.ShouldNotBeNull();
        static Exception _exception;
    }

    public class when_operation_is_ok
    {
        Establish context = () => _uploadAttempts = 0;

        Because of = () => ReliableCloudOperations.UploadBlob(() => _uploadAttempts++);

        It should_not_retry = () => _uploadAttempts.ShouldEqual(1);

        static int _uploadAttempts;
    }
}