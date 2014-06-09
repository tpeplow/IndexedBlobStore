namespace IndexedBlobStore
{
    public class TaggedIndexedBlob
    {
        public TaggedIndexedBlob(IReadonlyIndexedBlob blob, string tag)
        {
            Blob = blob;
            Tag = tag;
        }

        public IReadonlyIndexedBlob Blob { get; private set; }

        public string Tag { get; private set; }
    }
}