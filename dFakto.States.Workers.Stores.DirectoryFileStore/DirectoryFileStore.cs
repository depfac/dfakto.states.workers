using System;
using System.IO;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;

namespace dFakto.States.Workers.Stores.DirectoryFileStore
{
    public class DirectoryFileStore : IFileStore
    {
        public const string TYPE = "file";
        
        private readonly string _basePath;

        public string Name { get; }
        
        public DirectoryFileStore(string fileStoreName,  DirectoryFileStoreConfig config)
        {
            Name = fileStoreName;
            _basePath = config.BasePath;
        }
        
        public Task<string> CreateFileToken(string fileName)
        {
            FileToken token = new FileToken(TYPE,Name);
            token.SetPath(fileName);
            return Task.FromResult(token.ToString());
        }

        public Task<string> GetFileName(string fileToken)
        {
            var token = FileToken.Parse(fileToken,Name);
            return Task.FromResult(Path.GetFileName(token.Path));
        }

        public async Task<Stream> OpenRead(string fileToken)
        {
            var token = FileToken.Parse(fileToken,Name);
            
            if(!await Exists(token))
                throw new FileNotFoundException();

            return new FileStream(GetAbsolutePath(FileToken.Parse(fileToken, Name)), FileMode.Open);
        }

        public Task<Stream> OpenWrite(string token)
        {
            string localPath = GetAbsolutePath(FileToken.Parse(token, Name));
            string dir = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            return Task.FromResult((Stream) new FileStream(localPath, FileMode.Create));
        }
        
        public Task Delete(string fileToken)
        {
            var absolutePath = GetAbsolutePath(FileToken.Parse(fileToken, Name));
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
            return Task.CompletedTask;
        }

        public async Task<bool> Exists(string fileToken)
        {
            return await Exists(FileToken.Parse(fileToken,Name));
        }
        
        private Task<bool> Exists(FileToken fileToken)
        {
            return Task.FromResult(File.Exists(GetAbsolutePath(fileToken)));
        }

        private string GetAbsolutePath(FileToken token)
        {
            return Path.Combine(_basePath, token.Path);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}