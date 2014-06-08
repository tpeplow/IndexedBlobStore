namespace IndexedBlobStore
{
    public class TaggedIndexedBlob
    {
        public TaggedIndexedBlob(IReadonlyIndexedBlob blob, IndexedBlobTag tag)
        {
            Blob = blob;
            Tag = tag;
        }

        public IReadonlyIndexedBlob Blob { get; private set; }

        public IndexedBlobTag Tag { get; private set; }
    }
}