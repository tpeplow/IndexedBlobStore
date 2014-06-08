namespace IndexedBlobStore
{
    public class IndexedBlobTag
    {
        public IndexedBlobTag(string tag, string fileName)
        {
            Tag = tag;
            FileName = fileName;
        }

        public string Tag { get; private set; }

        public string FileName { get; private set; }
    }
}