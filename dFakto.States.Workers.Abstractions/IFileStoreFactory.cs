using System.Collections.Generic;

namespace dFakto.States.Workers.Abstractions
{
    public interface IFileStoreFactory
    {
        IFileStore GetFileStoreFromName(string name);
        IFileStore GetFileStoreFromFileToken(string fileToken);
        IEnumerable<IFileStore> GetFileStores();
    }
}