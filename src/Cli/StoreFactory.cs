using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

public static class StoreFactory
{
    public static ICatalogStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

        ICatalogStore store = useFile
            ? new FileCatalogStore(dataPath)
            : new InMemoryCatalogStore(SampleData.Products());

        if (useCache)
        {
            store = new CachingCatalogStore(store);
        }

        return store;
    }
}