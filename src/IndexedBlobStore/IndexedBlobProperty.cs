using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    internal class IndexedBlobProperty : TableEntity
    {
        public const string Prefix = "prop::";

        public IndexedBlobProperty()
        {
        }

        public IndexedBlobProperty(string fileKey, string key, string value)
        {
            PartitionKey = fileKey;
            RowKey = Prefix + key;
            Value = value;
            Key = key;
        }

        public string Value { get; set; }
        public string Key { get; set; }
    }
}