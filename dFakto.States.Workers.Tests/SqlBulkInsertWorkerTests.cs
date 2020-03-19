using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.Interfaces;
using dFakto.States.Workers.Sql;
using dFakto.States.Workers.Sql.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace dFakto.States.Workers.Tests
{
    public class SqlBulkInsertWorkerTests : BaseTests
    {
        [Theory]
        [InlineData("pgsql","sqlserver")]
        [InlineData("sqlserver","sqlserver")]
        [InlineData("mariadb","sqlserver")]
        
        [InlineData("sqlserver","pgsql")]
        [InlineData("pgsql","pgsql")]
        [InlineData("mariadb", "pgsql")]
        
        [InlineData("sqlserver","mariadb")]
        [InlineData("pgsql","mariadb")]
        [InlineData("mariadb", "mariadb")]
        public async Task TestBulkInsertFromQuery(string source, string destination)
        {
            var sql = Host.Services.GetService<SqlBulkInsertWorker>();
            var src = Host.Services.GetServices<BaseDatabase>().First(x => x.Name == source);
            var dst = Host.Services.GetServices<BaseDatabase>().First(x => x.Name == destination);

            string tableSrc = CreateTable(src);
            string tableDst = CreateTable(dst);

            Insert(src, tableSrc, Enumerable.Range(0, 1000).Select(x => (x, "hello")));

            try
            {
                var input = new BulkInsertInput();
                input.Source.ConnectionName = src.Name;
                input.Source.Query = new SqlQuery
                {
                    Query = $"SELECT * FROM {tableSrc}",
                    Type = SqlQueryType.Reader
                };
                input.Destination.ConnectionName = dst.Name;
                input.Destination.TableName = tableDst;
                
                var result = await sql.DoJsonWork<BulkInsertInput,bool>(input);
                Assert.True(result);
                Assert.Equal(1000,Count(dst,tableDst));
            }
            finally
            {
                DropTable(src, tableSrc);
                DropTable(dst, tableDst);
            }
        }

        [Theory]
        [InlineData("pgsql")]
        [InlineData("sqlserver")]
        [InlineData("mariadb")]
        public async Task TestBulkInsertFromCsvFileWithoutHeader(string destination)
        {
            var fileStore = Host.Services.GetService<FileStoreFactory>().GetFileStoreFromName("test");
            var dst = Host.Services.GetServices<BaseDatabase>().First(x => x.Name == destination);

            string tableName = CreateTable(dst);
            try
            {
                var token = await fileStore.CreateFileToken("test.csv");
                await using (var stream = await fileStore.OpenWrite(token))
                await using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.WriteLine("1,Frank");
                    writer.WriteLine("2,Bob");
                    writer.WriteLine("3,Julia");
                    writer.WriteLine("4,Marie");
                }

                BulkInsertInput input = new BulkInsertInput();
                input.Source.FileToken = token;
                input.Source.Separator = ',';
                input.Source.Headers = false;
                input.Destination.ConnectionName = destination;
                input.Destination.TableName = tableName;

                var sql = Host.Services.GetService<SqlBulkInsertWorker>();

                var result = await sql.DoJsonWork<BulkInsertInput,bool>(input);

                Assert.True(result);
                Assert.Equal(4, Count(dst, tableName));
            }
            finally
            {
                DropTable(dst,tableName);
            }
        }
        
        [Theory]
        [InlineData("pgsql")]
        [InlineData("sqlserver")]
        [InlineData("mariadb")]
        public async Task TestBulkInsertFromCsvFileWithHeader(string destination)
        {
            var fileStore = Host.Services.GetService<FileStoreFactory>().GetFileStoreFromName("test");
            var dst = Host.Services.GetServices<BaseDatabase>().First(x => x.Name == destination);

            string tableName = CreateTable(dst);
            try
            {
                var token = await fileStore.CreateFileToken("test.csv");
                await using (var stream = await fileStore.OpenWrite(token))
                await using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.WriteLine("id,name");
                    writer.WriteLine("1,Frank");
                    writer.WriteLine("2,Bob");
                    writer.WriteLine("3,Julia");
                    writer.WriteLine("4,Marie");
                }

                BulkInsertInput input = new BulkInsertInput();
                input.Source.FileToken = token;
                input.Source.Separator = ',';
                input.Source.Headers = true;
                input.Destination.ConnectionName = destination;
                input.Destination.TableName = tableName;

                var sql = Host.Services.GetService<SqlBulkInsertWorker>();

                var result = await sql.DoJsonWork<BulkInsertInput,bool>(input);

                Assert.True(result);
                Assert.Equal(4, Count(dst, tableName));
            }
            finally
            {
                DropTable(dst,tableName);
            }
        }
        private string CreateTable(BaseDatabase database)
        {
            var tableName = StringUtils.Random(10);
            using var conn = database.CreateConnection();
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"CREATE TABLE {tableName} (col1 INT , col2 VARCHAR(100))";
                cmd.ExecuteNonQuery();
            }

            return tableName;
        }

        private void DropTable(BaseDatabase database, string tableName)
        {
            using var conn = database.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP TABLE {tableName}";
            cmd.ExecuteNonQuery();
        }

        private void Insert(BaseDatabase database, string tableName, IEnumerable<(int, string )> values)
        {
            using var conn = database.CreateConnection();
            conn.Open();
            
            foreach (var value in values)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"INSERT INTO {tableName} VALUES(@p1, @p2)";
                var p = cmd.CreateParameter();
                p.ParameterName = "p1";
                p.Value = value.Item1;
                cmd.Parameters.Add(p);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "p2";
                p2.Value = value.Item2;
                cmd.Parameters.Add(p2);

                cmd.ExecuteNonQuery();
            }
        }

        private int Count(BaseDatabase database, string tableName)
        {
            using var conn = database.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}