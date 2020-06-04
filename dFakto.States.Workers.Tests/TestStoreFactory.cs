using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Stores.DirectoryFileStore;
using dFakto.States.Workers.Stores.MysqlDbStore;
using dFakto.States.Workers.Stores.OracleDbStore;
using dFakto.States.Workers.Stores.PostgresqlDatabaseStore;
using dFakto.States.Workers.Stores.SqlserverDbStore;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Tests
{
    public class TestStoreFactory : IStoreFactory
    {
        private Dictionary<string,IStore> _stores = new Dictionary<string, IStore>();
        
        public TestStoreFactory()
        {
            _stores.Add("test", new DirectoryFileStore("test", new DirectoryFileStoreConfig {BasePath = Path.GetTempPath()}));
            _stores.Add("pgsql", new PostgresqlDbStore("pgsql", new NpgsqlConfig{ConnectionString = "server=localhost; user id=postgres; password=depfac$2000; database=test"}));
            _stores.Add("sqlserver", new SqlServerDbStore("sqlserver", new SqlServerConfig{ConnectionString = "server=localhost; user id=sa; password=depfac$2000; database=test"}, null));
            _stores.Add("mariadb", new MysqlDbStore("mariadb",new MysqlConfig{ConnectionString = "server=localhost; user id=root; password=depfac$2000; database=test; AllowLoadLocalInfile=true"}, null ));
            // _stores.Add("oracle",
            //     new OracleDbStore("oracke",
            //         new OracleConfig
            //             {ConnectionString = "User Id=root; Password=depfac$2000; Data Source=localhost:1521/orc1"},
            //         null));
        }
        public IDbStore GetDatabaseStoreFromName(string name)
        {
            if (_stores.ContainsKey(name.ToLower()))
            {
                return _stores[name.ToLower()] as IDbStore;
            }
            return null;
        }

        public IFileStore GetFileStoreFromName(string name)
        {
            if (_stores.ContainsKey(name.ToLower()))
            {
                return _stores[name.ToLower()] as IFileStore;
            }
            return null;
        }

        public IFileStore GetFileStoreFromFileToken(string fileToken)
        {
            var name = FileToken.ParseName(fileToken).ToLower();
            
            if (_stores.ContainsKey(name))
            {
                return _stores[name] as IFileStore;
            }
            return null;
        }

        public IEnumerable<IStore> GetStores()
        {
            return _stores.Values;
        }
    }
}