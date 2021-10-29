namespace MongoDownloader
{
    public interface IVersion
    {
        public string Number { get; }

        public bool Production { get; }
    }
}