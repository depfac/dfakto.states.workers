using System;
using System.Collections.Generic;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;

namespace dFakto.States.Workers.FileStores
{
    public class FileStoreFactoryConfig
    {
        public FileStoreConfig[] Stores { get; set; }
    }
}