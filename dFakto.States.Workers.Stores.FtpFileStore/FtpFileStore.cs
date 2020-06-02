using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CoreFtp;
using dFakto.States.Workers.Abstractions;
using FluentFTP;
using FtpClient = FluentFTP.FtpClient;

namespace dFakto.States.Workers.Stores.FtpFileStore
{
    public class FtpFileStore: IFileStore
    {
        private readonly string _fileStoreName;
        private readonly FtpFileStoreConfig _config;
        public const string TYPE = "ftp";
        private readonly CoreFtp.FtpClient _client;

        public FtpFileStore(string fileStoreName, FtpFileStoreConfig config)
        {
            _fileStoreName = fileStoreName;
            _config = config;
            _client = new CoreFtp.FtpClient(new FtpClientConfiguration
            {
                Host = config.HostName,
                Username = config.Username,
                Password = config.Password
            });
            _client.LoginAsync().Wait();
        }

        public Task<string> CreateFileToken(string fileName)
        {
            var now = DateTime.Now;
            
            FileToken token = new FileToken(TYPE, _fileStoreName);
            token.Path = Path.Combine(now.Year.ToString(), now.Month.ToString("00"), now.Day.ToString("00"),
                fileName).GetFtpPath();
            return Task.FromResult(token.ToString());
        }

        public Task<string> GetFileName(string fileToken)
        {
            FileToken token = FileToken.Parse(fileToken,_fileStoreName);
            return Task.FromResult(token.Path.GetFtpFileName());
        }

        public async Task<Stream> OpenRead(string token)
        {
            var fileToken = FileToken.Parse(token,_fileStoreName);
            
            var client = GetNewClient();
            string dir = fileToken.Path.GetFtpDirectoryName();
            if (!await client.DirectoryExistsAsync(dir))
            {
                await client.CreateDirectoryAsync(dir);
            }

            return await _client.OpenFileReadStreamAsync(fileToken.Path);
        }

        public async Task<Stream> OpenWrite(string token)
        {

            var fileToken = FileToken.Parse(token,_fileStoreName);
            
            string dir = fileToken.Path.GetFtpDirectoryName();
            var client = GetNewClient();
            
            if (!await client.DirectoryExistsAsync(dir))
            {
                await client.CreateDirectoryAsync(dir);
            }
            
            return await _client.OpenFileWriteStreamAsync(fileToken.Path);
        }

        public async Task Delete(string fileToken)
        {
            FileToken token = FileToken.Parse(fileToken,_fileStoreName);
            using (var client = GetNewClient())
            {
                if (await client.FileExistsAsync(token.Path))
                {
                    await client.DeleteFileAsync(token.Path);
                }
            }
        }

        public async Task<bool> Exists(string fileToken)
        {
            FileToken token = FileToken.Parse(fileToken,_fileStoreName);

            using (var c = GetNewClient())
            {
                return await c.FileExistsAsync(token.Path);
            }
        }

        private FtpClient GetNewClient()
        {
            var _ftpClient = new FtpClient(_config.HostName);
            _ftpClient.Credentials = _config.Username == null ? 
                new NetworkCredential() : 
                new NetworkCredential(_config.Username, _config.Password);
            _ftpClient.SslProtocols = _config.SslProtocols;
            _ftpClient.DataConnectionEncryption = _ftpClient.EncryptionMode != FtpEncryptionMode.None;
            _ftpClient.Connect();

            return _ftpClient;
        }

        public void Dispose()
        {
        }
    }
}