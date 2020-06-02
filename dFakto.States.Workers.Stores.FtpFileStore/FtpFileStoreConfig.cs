using System.Security.Authentication;

namespace dFakto.States.Workers.Stores.FtpFileStore
{
    public class FtpFileStoreConfig
    {
        public string HostName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Encryption { get; set; }
        public SslProtocols SslProtocols { get; set; }
    }
}