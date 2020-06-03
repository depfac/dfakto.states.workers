using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using dFakto.States.Workers.SqlToCsvWorker;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dFakto.States.Workers.Tests
{
    public class SqlExportToCsvTests: BaseTests
    {
        private readonly string _tableName = StringUtils.Random(10);
        
        private readonly IStoreFactory _storeFactory;
        public SqlExportToCsvTests()
        {
            _storeFactory = Host.Services.GetService<IStoreFactory>();
            CreateTable(_tableName);
        }
        
        [Fact]
        public async Task TestExport()
        {
            Insert(_tableName, (1, "hello"), (2,"world"));
            SqlExportToCsvWorker worker = Host.Services.GetService<SqlExportToCsvWorker>();

            foreach (var database in Host.Services.GetService<IStoreFactory>().GetFileStores().Where( x => x is IDbStore))
            {
                var input = new SqlExportToCsvInput
                {
                    ConnectionName = database.Name,
                    Query = $"SELECT * FROM {_tableName} ORDER BY COL1",
                    OutputFileName = "tmp",
                    OutputFileStoreName = "test"
                };
                
                var csvFileToken = await worker.DoJsonWork<SqlExportToCsvInput,string>(input);
                var outputFileStore = _storeFactory.GetFileStoreFromName("test");
                string csvFileContent = await ReadTextFileInStore(outputFileStore, csvFileToken);
                var lines = csvFileContent.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(3, lines.Length);
                Assert.Equal("\"col1\";\"col2\"", lines[0]);
                Assert.Equal("\"1\";\"hello\"", lines[1]);
                Assert.Equal("\"2\";\"world\"", lines[2]);
            }
        }
        
        [Fact]
        public async Task TestExport_CommaAsSeparator()
        {
            Insert(_tableName, (1, "hello"), (2,"world"));
            
            SqlExportToCsvWorker worker = Host.Services.GetService<SqlExportToCsvWorker>();

            foreach (var database in Host.Services.GetService<IStoreFactory>().GetFileStores().Where( x => x is IDbStore))
            {
                var input = new SqlExportToCsvInput
                {
                    ConnectionName = database.Name,
                    Query = $"SELECT * FROM {_tableName} ORDER BY COL1",
                    OutputFileName = "tmp",
                    OutputFileStoreName = "test",
                    Separator = ','
                };
                
                var csvFileToken = await worker.DoJsonWork<SqlExportToCsvInput,string>(input);
                var outputFileStore = _storeFactory.GetFileStoreFromName("test");
                string csvFileContent = await ReadTextFileInStore(outputFileStore, csvFileToken);
                var lines = csvFileContent.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(3, lines.Length);
                Assert.Equal("\"col1\",\"col2\"", lines[0]);
                Assert.Equal("\"1\",\"hello\"", lines[1]);
                Assert.Equal("\"2\",\"world\"", lines[2]);
            }
        }
    }
}