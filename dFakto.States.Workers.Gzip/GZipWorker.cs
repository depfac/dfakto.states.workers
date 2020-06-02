using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers.Gzip
{
    public class GZipWorker : BaseWorker<GZipInput,string>
    {
        private readonly ILogger<GZipWorker> _logger;
        private readonly IFileStoreFactory _fileStoreFactory;
        private static readonly string GzipExtension = "gz";
        public GZipWorker(ILogger<GZipWorker> logger,IFileStoreFactory fileStoreFactory)
        :base("GZip")
        {
            _logger = logger;
            _fileStoreFactory = fileStoreFactory;
        }

        public override async Task<string> DoWorkAsync(GZipInput input, CancellationToken token)
        {
            string outputFileName = input.OutputFileName;

            using var inputFileStore = _fileStoreFactory.GetFileStoreFromFileToken(input.FileToken);
            using var outputFileStore = string.IsNullOrEmpty(input.OutputFileStoreName) ? inputFileStore :  _fileStoreFactory.GetFileStoreFromName(input.OutputFileStoreName);

            if (string.IsNullOrEmpty(outputFileName))
            {
                string fileName = await outputFileStore.GetFileName(input.FileToken);
                outputFileName = input.Compress ? GetCompressedFileName(fileName) : GetDecompressedFileName(fileName);
            }
            
            _logger.LogDebug($"GZip output file name '{outputFileName}'");

            string outputFileToken = await outputFileStore.CreateFileToken(outputFileName);
            using (var inputStream = await inputFileStore.OpenRead(input.FileToken))
            using (var outputStream = await outputFileStore.OpenWrite(outputFileToken))
            using (var gzipStream = input.Compress ? 
                new GZipStream(outputStream, CompressionMode.Compress) :
                new GZipStream(inputStream, CompressionMode.Decompress))
            {
                if (input.Compress)
                {
                    await inputStream.CopyToAsync(gzipStream, input.BufferSize, token);
                }
                else
                {
                    await gzipStream.CopyToAsync(outputStream,input.BufferSize, token);
                }
            }

            if (input.DeleteSource)
            {
                _logger.LogDebug("Deleting source filetoken");
                await inputFileStore.Delete(input.FileToken);
            }

            return outputFileToken;
        }

        private string GetDecompressedFileName(string outputFileName)
        {
            if (Path.HasExtension(outputFileName)) // Try to remove the .gz at the end
            {
                return Path.GetFileNameWithoutExtension(outputFileName); 
            }
            return outputFileName + "_" + DateTime.Now.Ticks;
            
        }

        private string GetCompressedFileName(string outputFileName)
        {
            return $"{outputFileName}.{GzipExtension}";
        }
    }
}