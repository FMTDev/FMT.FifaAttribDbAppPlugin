using FMT.ServicesManagers.AssetEntryServicing;

namespace FifaAttribDbAppPlugin
{
    public interface IFIFAAttribDbService : IAssetEntryService
    {
        public List<FIFAAttribDbAssetEntry> Assets { get; }

        public void Load(byte[] vaultData, byte[] binaryData);
    }
}