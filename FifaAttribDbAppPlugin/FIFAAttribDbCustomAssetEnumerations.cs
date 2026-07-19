using FMT.PluginInterfaces;
using FMT.PluginInterfaces.Assets;
using FMT.ProfileSystem;
using FMT.ServicesManagers;
using FMT.ServicesManagers.AssetEntryServicing;
using FMT.ServicesManagers.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FifaAttribDbAppPlugin
{
    public sealed class FIFAAttribDbCustomAssetEnumerations : ICustomAssetEntryEnumerations
    {
        private IAssetManagementService assetManagementService => SingletonService.GetInstance<IAssetManagementService>();

        public Dictionary<string, IEnumerable<IAssetEntry>> GetCustomAssetEntriesEnumerations()
        {
            // This only works in FIFA17, so far
            if (!ProfileManager.IsLoaded(EGame.FIFA17, EGame.FIFA18))
                //if (!ProfileManager.IsLoaded(EGame.FIFA17))
                return new Dictionary<string, IEnumerable<IAssetEntry>>();

            var service = new FIFAAttribDbService();

            byte[] GetAssetBytes(IAssetEntry entry)
            {
                using (var stream = assetManagementService.CustomAssetManagers["legacy"].GetAsset(entry))
                {
                    if (stream is MemoryStream ms)
                    {
                        return ms.ToArray();
                    }
                    using (var tempMs = new MemoryStream())
                    {
                        stream.CopyTo(tempMs);
                        return tempMs.ToArray();
                    }
                }
            }

            var ae_attribdbgameplay = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.bin");
            var ae_attribdbgameplayvlt = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.vlt");

            if (ae_attribdbgameplay != null && ae_attribdbgameplayvlt != null)
            {
                assetManagementService.RevertAsset(ae_attribdbgameplay);
                assetManagementService.RevertAsset(ae_attribdbgameplayvlt);
                service.Load(
                    "data/attribdbgameplay/attribdb.vlt",
                    "data/attribdbgameplay/attribdb.bin",
                    GetAssetBytes(ae_attribdbgameplayvlt),
                    GetAssetBytes(ae_attribdbgameplay)
                );
            }

            //var ae_attribdb = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdb/attribdb.bin");
            //var ae_attribdbvlt = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdb/attribdb.vlt");

            //if (ae_attribdb != null && ae_attribdbvlt != null)
            //{
            //    assetManagementService.RevertAsset(ae_attribdb);
            //    assetManagementService.RevertAsset(ae_attribdbvlt);
            //    service.Load(
            //        "data/attribdb/attribdb.vlt",
            //        "data/attribdb/attribdb.bin",
            //        GetAssetBytes(ae_attribdbvlt),
            //        GetAssetBytes(ae_attribdb)
            //    );
            //}

            //var ae_attribdb_inbig = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdb/attribdb.bin");
            //var ae_attribdbvlt_inbig = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdb/attribdb.vlt");

            //if (ae_attribdb_inbig != null && ae_attribdbvlt_inbig != null)
            //{
            //    assetManagementService.RevertAsset(ae_attribdb);
            //    assetManagementService.RevertAsset(ae_attribdbvlt);
            //    service.Load(
            //        "data/attribdb/attribdb.vlt",
            //        "data/attribdb/attribdb.bin",
            //        GetAssetBytes(ae_attribdbvlt),
            //        GetAssetBytes(ae_attribdb)
            //    );
            //}

            if (SingletonService.Instantiated<FIFAAttribDbService>())
            {
                var existingService = SingletonService.GetInstance<IFIFAAttribDbService>();
                SingletonService.UnRegister<IFIFAAttribDbService>();
            }
            SingletonService.RegisterInstance<IFIFAAttribDbService, FIFAAttribDbService>(service);
            SingletonService.GetInstance<IAssetEntryServiceCollectionProvider>().RegisterAssetEntryService<FIFAAttribDbService>(service);

            // Assign the types to the Gameplay section
            Dictionary<string, IEnumerable<IAssetEntry>> assetCollections = new();
            assetCollections.Add("Gameplay", service.EnumerateAssets().OrderBy(x => x.Name));

            return assetCollections;
        }
    }
}

