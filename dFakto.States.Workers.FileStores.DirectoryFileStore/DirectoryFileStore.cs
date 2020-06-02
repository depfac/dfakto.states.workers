using System.IO;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;

namespace dFakto.States.Workers.FileStores.DirectoryFileStore
{
    public class DirectoryFileStore : IFileStore
    {
        private readonly string _fileStoreName;
        public const string TYPE = "file";
        
        private readonly string _basePath;

        public DirectoryFileStore(string fileStoreName,  DirectoryFileStoreConfig config)
        {
            _fileStoreName = fileStoreName;
            _basePath = config.BasePath;
        }
        
        public Task<string> CreateFileToken(string fileName)
        {
            FileToken token = new FileToken(TYPE,_fileStoreName);
            token.Path = fileName;
            
            return Task.FromResult(token.ToString());
        }

        public Task<string> GetFileName(string fileToken)
        {
            var token = FileToken.Parse(fileToken,_fileStoreName);
            return Task.FromResult(Path.GetFileName(token.Path));
        }

        public async Task<Stream> OpenRead(string fileToken)
        {
            var token = FileToken.Parse(fileToken,_fileStoreName);
            
            if(!await Exists(token))
                throw new FileNotFoundException();

            return new FileStream(GetAbsolutePath(FileToken.Parse(fileToken, _fileStoreName)), FileMode.Open);
        }

        public Task<Stream> OpenWrite(string token)
        {
            string localPath = GetAbsolutePath(FileToken.Parse(token, _fileStoreName));
            string dir = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            return Task.FromResult((Stream) new FileStream(localPath, FileMode.Create));
        }
        
        public Task Delete(string fileToken)
        {
            var absolutePath = GetAbsolutePath(FileToken.Parse(fileToken, _fileStoreName));
            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
            return Task.CompletedTask;
        }

        public async Task<bool> Exists(string fileToken)
        {
            return await Exists(FileToken.Parse(fileToken,_fileStoreName));
        }
        
        private Task<bool> Exists(FileToken fileToken)
        {
            return Task.FromResult(System.IO.File.Exists(GetAbsolutePath(fileToken)));
        }

        private string GetAbsolutePath(FileToken token)
        {
            return Path.Combine(_basePath, token.Path);
        }

        public void Dispose()
        {
        }
    }
}