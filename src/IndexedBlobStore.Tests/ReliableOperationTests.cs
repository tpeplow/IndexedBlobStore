using System;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage;

namespace IndexedBlobStore.Tests
{
    public class when_blob_read_fails_with_bad_request
    {
        Establish context = () => _uploadAttempts = 0;
        Because of = () => _exception = Catch.Exception(() => ReliableCloudOperations.RetryRead(() =>
        {
            _uploadAttempts++;
            throw new StorageException(new RequestResult { HttpStatusCode = 400 }, "request bad", new Exception("inside you"));
        }));

        It should_retry_5_times = () => _uploadAttempts.ShouldEqual(6);
        It should_throw_the_exception = () => _exception.ShouldNotBeNull();
        It should_throw_read_exception = () =>
        {
            _exception.ShouldBeOfExactType<RetryReadException>();
            _exception.InnerException.ShouldBeOfExactType<StorageException>();
        };

        static int _uploadAttempts;
        static Exception _exception;
    }

    public class when_blob_read_fails_with_precondition_failed
    {
        Establish context = () => _uploadAttempts = 0;
        Because of = () => _exception = Catch.Exception(() => ReliableCloudOperations.RetryRead(() =>
        {
            _uploadAttempts++;
            throw new StorageException(new RequestResult { HttpStatusCode = 412 }, "preconditions not met", new Exception("inside you"));
        }));

        It should_NOT_retry = () => _uploadAttempts.ShouldEqual(1);
        It should_throw_the_exception = () => _exception.ShouldNotBeNull();
        It should_throw_write_exception = () =>
        {
            _exception.ShouldBeOfExactType<RetryReadException>();
            _exception.InnerException.ShouldBeOfExactType<StorageException>();
        };

        static int _uploadAttempts;
        static Exception _exception;
    }

    public class when_read_operation_has_some_other_storage_exception
    {
        Because of = () => _exception = Catch.Exception(() => ReliableCloudOperations.RetryRead(() =>
        {
            throw new StorageException(new RequestResult { HttpStatusCode = 500 }, "internal", new Exception("inside you"));
        }));

        It should_throw = () => _exception.ShouldNotBeNull();
        It should_throw_write_exception = () =>
        {
            _exception.ShouldBeOfExactType<RetryReadException>();
            _exception.InnerException.ShouldBeOfExactType<StorageException>();
        };

        static Exception _exception;
    }

    public class when_read_operation_is_ok
    {
        Establish context = () => _uploadAttempts = 0;

        Because of = () => ReliableCloudOperations.RetryRead(() => _uploadAttempts++);

        It should_not_retry = () => _uploadAttempts.ShouldEqual(1);

        static int _uploadAttempts;
    }



    public class when_blob_write_fails_with_bad_request
    {
        Establish context = () => _uploadAttempts = 0;
        Because of = () => _exception = Catch.Exception(() =>  ReliableCloudOperations.RetryWrite(() =>
        {
            _uploadAttempts++;
            throw new StorageException(new RequestResult { HttpStatusCode = 400 }, "request bad", new Exception("inside you"));
        }));
        
        It should_retry_5_times = () => _uploadAttempts.ShouldEqual(6);
        It should_throw_the_exception = () => _exception.ShouldNotBeNull();
        It should_throw_write_exception = () =>
        {
            _exception.ShouldBeOfExactType<RetryWriteException>();
            _exception.InnerException.ShouldBeOfExactType<StorageException>();
        };
        
        static int _uploadAttempts;
        static Exception _exception;
    }

    public class when_blob_write_fails_with_precondition_failed
    {
        Establish context = () => _uploadAttempts = 0;
        Because of = () => _exception = Catch.Exception(() => ReliableCloudOperations.RetryWrite(() =>
        {
            _uploadAttempts++;
            throw new StorageException(new RequestResult { HttpStatusCode = 412 }, "preconditions not met", new Exception("inside you"));
        }));

        It should_NOT_retry = () => _uploadAttempts.ShouldEqual(1);
        It should_throw_the_exception = () => _exception.ShouldNotBeNull();
        It should_throw_write_exception = () =>
        {
            _exception.ShouldBeOfExactType<RetryWriteException>();
            _exception.InnerException.ShouldBeOfExactType<StorageException>();
        };

        static int _uploadAttempts;
        static Exception _exception;
    }

    public class when_write_operation_has_some_other_storage_exception
    {
        Because of = () => _exception = Catch.Exception(() => ReliableCloudOperations.RetryWrite(() =>
        {
            throw new StorageException(new RequestResult { HttpStatusCode = 500 }, "internal", new Exception("inside you"));
        }));

        It should_throw = () => _exception.ShouldNotBeNull();
        It should_throw_write_exception = () =>
        {
            _exception.ShouldBeOfExactType<RetryWriteException>();
            _exception.InnerException.ShouldBeOfExactType<StorageException>();
        };

        static Exception _exception;
    }

    public class when_write_operation_is_ok
    {
        Establish context = () => _uploadAttempts = 0;

        Because of = () => ReliableCloudOperations.RetryWrite(() => _uploadAttempts++);

        It should_not_retry = () => _uploadAttempts.ShouldEqual(1);

        static int _uploadAttempts;
    }
}