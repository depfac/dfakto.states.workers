namespace dFakto.States.Workers.Abstractions
{
    public interface IFileOutput
    {
        public string OutputFileStoreName { get; set; }

        public string OutputFileName { get; set; }
    }
}
