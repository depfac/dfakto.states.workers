using System;
using System.IO;
using System.Threading.Tasks;

namespace dFakto.States.Workers.Abstractions
{
    public interface IFileStore : IDisposable
    {
        Task<string> CreateFileToken(string fileName);
        Task<string> GetFileName(string fileToken);
        Task<Stream> OpenRead(string taskToken);
        Task<Stream> OpenWrite(string taskToken);
        Task Delete(string fileToken);
        Task<bool> Exists(string fileToken);
    }
}