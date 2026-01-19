using FMT.PluginInterfaces;
using FMT.PluginInterfaces.Assets;
using FMT.ProfileSystem;
using FMT.ServicesManagers;
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
            if (!ProfileManager.IsLoaded(EGame.FIFA17))
                return new Dictionary<string, IEnumerable<IAssetEntry>>();

            var ae_attribdbgameplay = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.bin");
            assetManagementService.CustomAssetManagers["legacy"].GetAsset(ae_attribdbgameplay);
            var ae_attribdbgameplayvlt = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.vlt");

            var service = new FIFAAttribDbService();
            service.Load(((MemoryStream)assetManagementService.CustomAssetManagers["legacy"].GetAsset(ae_attribdbgameplayvlt)).ToArray());

            Dictionary<string, IEnumerable<IAssetEntry>> assetCollections = new();
            assetCollections.Add("Gameplay", service.Types);

            return assetCollections;
        }
    }
}

