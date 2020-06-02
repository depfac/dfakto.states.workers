using System;
using System.IO;
using dFakto.States.Workers.Stores.DirectoryFileStore;
using Xunit;

namespace dFakto.States.Workers.Tests
{
    public class DirectoryFileStoreTests
    {
        [Theory]
        [InlineData("test.doc")]
        [InlineData("sub/sd.doc")]
        public void CheckFileToken(string fileName)
        {
            DirectoryFileStore store = new DirectoryFileStore("test",new DirectoryFileStoreConfig
            {
                BasePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            });

            Assert.Equal("file://test/"+fileName,store.CreateFileToken(fileName).Result);
        }
        
        [Theory]
        [InlineData("/test.doc")]
        [InlineData("/sub/test.doc")]
        public void CheckFileTokenStartingWithSlash(string fileName)
        {
            DirectoryFileStore store = new DirectoryFileStore("test",new DirectoryFileStoreConfig
            {
                BasePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            });

            Assert.Equal("file://test"+fileName,store.CreateFileToken(fileName).Result);
        }
        
        
        [Theory]
        [InlineData("test.doc")]
        public void CheckCreateAndDeleteFile(string fileName)
        {
            DirectoryFileStore store = new DirectoryFileStore("test",new DirectoryFileStoreConfig
            {
                BasePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            });

            var token = store.CreateFileToken(fileName).Result;
            using (var stream = store.OpenWrite(token).Result)
                using(var writer = new StreamWriter(stream))
            {
                writer.Write("hello");
            }

            Assert.True(store.Exists(token).Result);
            Assert.Equal("hello",
                File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    fileName)));

            store.Delete(token).Wait();
            Assert.False(store.Exists(token).Result);
        }
    }
}