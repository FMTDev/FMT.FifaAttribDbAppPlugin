using FMT.Core.Readers.Ebx;
using FMT.InitfsSupport;
using FMT.Logging;
using FMT.Models.Assets;
using FMT.Models.Assets.AssetEntry.Entries;
using FMT.PluginInterfaces;
using FMT.PluginInterfaces.Assets;
using FMT.ServicesManagers;
using FMT.ServicesManagers.AssetEntryServicing;
using FMT.ServicesManagers.Interfaces;
using System.Collections.Concurrent;

namespace FifaAttribDbAppPluginTests
{
    internal class AssetManagementMockForTests : IAssetManagementService
    {
        public List<SuperBundleEntry> SuperBundles { get; } = new List<SuperBundleEntry>();

        public ConcurrentDictionary<Guid, ChunkAssetEntry> SuperBundleChunks { get; } = new ConcurrentDictionary<Guid, ChunkAssetEntry>();

        public List<BundleEntry> Bundles { get; } = new List<BundleEntry>();

        public ILogger Logger { get; set; }
        public Dictionary<string, ICustomAssetManager<IAssetEntry>> CustomAssetManagers { get; set; } = new Dictionary<string, ICustomAssetManager<IAssetEntry>>();

        public List<EmbeddedFileEntry> EmbeddedFileEntries { get; } = new List<EmbeddedFileEntry>();

        public LocaleIniService LocaleINIMod => throw new NotImplementedException();

        public ConcurrentDictionary<string, EbxAssetEntry> EBX { get; private set; } = new ConcurrentDictionary<string, EbxAssetEntry>();

        public ConcurrentDictionary<string, ResAssetEntry> RES { get; private set; } = new ConcurrentDictionary<string, ResAssetEntry>();

        public ConcurrentDictionary<Guid, ChunkAssetEntry> Chunks { get; private set; } = new ConcurrentDictionary<Guid, ChunkAssetEntry>();
        public InitFSManager InitFSManager { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event IAssetManagementService.AssetManagerModifiedHandler AssetManagerInitialised;
        public event IAssetManagementService.AssetManagerModifiedHandler AssetManagerModified;

        public void AddChunk(ChunkAssetEntry entry)
        {
            Chunks.TryAdd(entry.Id, entry);
        }

        public bool AddEbx(EbxAssetEntry entry)
        {
            return EBX.TryAdd(entry.Name, entry);
        }

        public void AddRes(ResAssetEntry entry)
        {
            RES.TryAdd(entry.Name, entry);
        }

        public IEnumerable<ChunkAssetEntry> EnumerateChunks(bool modifiedOnly = false)
        {
            return Chunks.Values;
        }

        public IEnumerable<IAssetEntry> EnumerateCustomAssets(string type, bool modifiedOnly = false)
        {
            return Chunks.Values;
        }

        public IEnumerable<EbxAssetEntry> EnumerateEbx(string type = "", bool modifiedOnly = false, bool includeLinked = false, bool includeHidden = true, string bundleSubPath = "")
        {
            return EBX.Values;
        }

        public IEnumerable<ResAssetEntry> EnumerateRes(uint resType = 0, bool modifiedOnly = false, string bundleSubPath = "")
        {
            return RES.Values;
        }

        private MemoryStream GetAsset(AssetEntry entry, bool getModified = true)
        {
            return new MemoryStream(entry.ModifiedEntry.Data);
        }

        public byte[] GetAssetData(IAssetEntry entry, bool getModified = true)
        {
            return entry.ModifiedEntry.Data;
        }

        public MemoryStream GetChunk(IChunkAssetEntry entry, bool getModified = true)
        {
            return new MemoryStream(GetAssetData(entry, getModified));
        }

        public ChunkAssetEntry GetChunkEntry(Guid id, bool sbChunkOnly = false)
        {
            return Chunks.ContainsKey(id) ? Chunks[id] : null;
        }

        public Stream GetCustomAsset(string type, AssetEntry entry)
        {
            throw new NotImplementedException();
        }

        public EbxAsset GetEbx(EbxAssetEntry entry, bool getModified = true)
        {
            return EbxReader.GetEbxReader(new MemoryStream(EBX[entry.Name].ModifiedEntry.Data)).ReadAsset();
        }

        public EbxAsset GetEbx(IAssetEntry entry, bool getModified = true)
        {
            return EBX[entry.Name].ModifiedEntry.DataObject as EbxAsset;
        }

        public IEbxAsset GetEbxAssetFromStream(Stream stream)
        {
            throw new NotImplementedException();
        }

        public EbxAssetEntry GetEbxEntry(ReadOnlySpan<char> name)
        {
            return EBX[name.ToString()];
        }

        public Stream GetEbxStream(EbxAssetEntry entry, bool getModified = false)
        {
            return new MemoryStream(GetAssetData(entry, getModified));
        }

        public MemoryStream GetRes(ResAssetEntry entry, bool getModified = true)
        {
            throw new NotImplementedException();
        }

        public ResAssetEntry GetResEntry(string name)
        {
            throw new NotImplementedException();
        }

        public MemoryStream GetResourceData(string superBundleName, long offset, long size, IAssetEntry entry = null)
        {
            throw new NotImplementedException();
        }

        public int GetSuperBundleId(string superBundle)
        {
            throw new NotImplementedException();
        }

        public void Initialize(bool additionalStartup = true)
        {
            throw new NotImplementedException();
        }

        public void InitializeEbxEntriesAndCustomAssets(bool writeToCache)
        {
            throw new NotImplementedException();
        }

        public object LoadTypeFromPluginByInterface(string interfaceName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> LoadTypesFromPluginByInterface<T>(params object[] args)
        {
            throw new NotImplementedException();
        }

        public bool ModifyChunk(ChunkAssetEntry chunkAssetEntry, byte[] buffer, CompressionType compressionOverride = CompressionType.Default, bool addToChunkBundle = false)
        {
            throw new NotImplementedException();
        }

        public void ModifyCustomAsset(string type, string name, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void ModifyEbx(string name, EbxAsset asset)
        {
            throw new NotImplementedException();
        }

        public void ModifyRes(ResAssetEntry assetEntry, byte[] buffer, byte[] meta = null)
        {
            throw new NotImplementedException();
        }

        public void RemoveChunk(Guid id)
        {
            throw new NotImplementedException();
        }

        public void RevertAsset(IAssetEntry entry, bool dataOnly = false, bool suppressOnModify = true, bool forceResetOfLinkedAssets = false)
        {
            throw new NotImplementedException();
        }

        public void WriteToLog(string text, params object[] vars)
        {
            throw new NotImplementedException();
        }

        public void AssignEbxEntryTypes(bool writeToCache = true)
        {
        }

        public bool ModifyEntry(IAssetEntry entry, byte[] d2)
        {
            var assetType = entry.GetType();
            if (SingletonService.Instantiated<IAssetEntryServiceCollectionProvider>())
            {
                var service = SingletonService.GetInstance<IAssetEntryServiceCollectionProvider>().GetAssetEntryServiceForAssetEntry(assetType);
                if (service != null)
                {
                    // data here is not compressed AFAIK, but the service may expect compressed data, so we can add a parameter to the ModifyEntry method in the future if needed to specify this
                    return service.ModifyAssetEntry(entry, d2, false);
                }
            }
            return false;
        }

        public BundleEntry GetBundleEntry(int bundleId)
        {
            throw new NotImplementedException();
        }

        public bool GetLegacyFileNameIfExists(string name, out string legacyFileName)
        {
            throw new NotImplementedException();
        }

        public SuperBundleEntry GetSuperBundle(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BundleEntry> EnumerateBundles(BundleType type = BundleType.None, bool modifiedOnly = false)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
