namespace dFakto.States.Workers.Gzip
{
    public class GZipInput
    {
        public string FileToken { get; set; }
        public bool DeleteSource { get; set; }
        public bool Compress { get; set; }
        public string OutputFileName { get; set; }
        public string OutputFileStoreName { get; set; }
        public int BufferSize { get; set; } = 2048;
    }
}