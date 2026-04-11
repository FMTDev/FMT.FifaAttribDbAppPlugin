using FifaAttribDbAppPlugin;
using FMT.Hash;
using FMT.PluginInterfaces.Assets;

namespace FMT.ServicesManagers.AssetEntryServicing
{
    public class FifaAttribDbAssetEntryProviderService : IAssetEntryService
    {
        public Type AssetEntryType { get; } = typeof(FIFAAttribDbAssetEntry);

        public IEnumerable<IAssetEntry> EnumerateAssets(bool modifiedOnly = false)
        {
            return SingletonService
                .GetInstance<IFIFAAttribDbService>().EnumerateAssets(modifiedOnly);
        }

        public FIFAAttribDbAssetEntry GetAssetEntry(string key)
        {
            return SingletonService
                .GetInstance<IFIFAAttribDbService>()
                .Assets
                .First(x => x.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        public T GetAssetEntry<T>(string key) where T : class, IAssetEntry
        {
            return GetAssetEntry(key) as T;
        }

        public byte[] GetAssetEntryData(IAssetEntry entry)
        {
            return GetAssetEntry(entry.Name).GetData();
        }

        public bool ModifyAssetEntry(IAssetEntry entry, byte[] data)
        {
            return SingletonService
                .GetInstance<IFIFAAttribDbService>()
                .ModifyAssetEntry(entry, data, false);
        }

        public IAssetEntry GenerateAssetEntry(string name, string type, Sha1 sha)
        {
            return new FIFAAttribDbAssetEntry(new FIFAAttribDbType() { })
            {
                Name = name,
                Type = type,
                Sha1 = sha
            };
        }

        public void RevertAsset(IAssetEntry entry)
        {
            SingletonService
                .GetInstance<IFIFAAttribDbService>()
                .RevertAsset(entry);
        }

        public bool ModifyAssetEntry(IAssetEntry entry, byte[] data, bool isDataCompressed)
        {
            throw new NotImplementedException();
        }

        public byte[] WriteAssetEntryInfo(IAssetEntry entry)
        {
            throw new NotImplementedException();
        }

        public IAssetEntry ReadAssetEntryInfo(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
