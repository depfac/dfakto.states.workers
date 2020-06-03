using System.Collections.Generic;

namespace dFakto.States.Workers.Abstractions
{
    public interface IStoreFactory
    {
        IDbStore GetDatabaseStoreFromName(string name);
        IFileStore GetFileStoreFromName(string name);
        IFileStore GetFileStoreFromFileToken(string fileToken);
        IEnumerable<IStore> GetStores();
    }
}