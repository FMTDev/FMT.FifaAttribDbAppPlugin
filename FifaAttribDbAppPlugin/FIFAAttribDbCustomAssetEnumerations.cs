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

            var ae_attribdbgameplay = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.bin");
            var ae_attribdbgameplayvlt = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.vlt");

            // Ensure the assets are not modified
            assetManagementService.RevertAsset(ae_attribdbgameplay);
            assetManagementService.RevertAsset(ae_attribdbgameplayvlt);

            // TODO: This should not be registered here, but rather in the AppPlugin initialization
            var service = new FIFAAttribDbService();
            service.Load(((MemoryStream)assetManagementService.CustomAssetManagers["legacy"].GetAsset(ae_attribdbgameplayvlt)).ToArray(), ((MemoryStream)assetManagementService.CustomAssetManagers["legacy"].GetAsset(ae_attribdbgameplay)).ToArray());
            if (SingletonService.Instantiated<FIFAAttribDbService>())
            {
                var existingService = SingletonService.GetInstance<IFIFAAttribDbService>();
                SingletonService.UnRegister<IFIFAAttribDbService>();
            }
            SingletonService.RegisterInstance<IFIFAAttribDbService, FIFAAttribDbService>(service);
            SingletonService.GetInstance<IAssetEntryServiceCollectionProvider>().RegisterAssetEntryService<FIFAAttribDbService>(service);

            // Assign the types to the Gameplay section
            Dictionary<string, IEnumerable<IAssetEntry>> assetCollections = new();
            assetCollections.Add("Gameplay", service.EnumerateAssets().OrderBy(x=> x.Name));

            return assetCollections;
        }
    }
}

