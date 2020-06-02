using System;
using System.Collections.Generic;
using dFakto.States.Workers.Abstractions;
using Microsoft.Extensions.Configuration;

namespace dFakto.States.Workers.FileStores
{
    public class StoreFactoryConfig
    {
        public StoreConfig[] Stores { get; set; }
    }
}