using FMT.FileTools;
using FMT.ServicesManagers;
using FMT.ServicesManagers.Interfaces;

namespace FifaAttribDbAppPlugin.AttribDb
{
    public class FIFAAttribDbVLTWriter
    {
        private IAssetManagementService assetManagementService => SingletonService.GetInstance<IAssetManagementService>();

        public FIFAAttribDbVLTWriter()
        {
        }

        public async Task<byte[]> WriteToBytes(List<FIFAAttribDbAssetEntry> assets)
        {
            var ae_attribdbgameplayvlt = assetManagementService.CustomAssetManagers["legacy"].GetAssetEntry("data/attribdbgameplay/attribdb.vlt");
            assetManagementService.RevertAsset(ae_attribdbgameplayvlt);

            var ms = await Task.Run(() =>
            {
                return (MemoryStream)assetManagementService.CustomAssetManagers["legacy"].GetAsset(ae_attribdbgameplayvlt);
            });

            return await WriteToBytes(assets, ms.ToArray());
        }

        public async Task<byte[]> WriteToBytes(List<FIFAAttribDbAssetEntry> assets, byte[] vanillaData)
        {
            var ms = new MemoryStream(vanillaData);

            using (var nw = new NativeWriter(ms))
            {
                nw.Position = 0;
                foreach (var asset in assets)
                {
                    nw.Position = asset.AttribDbType.DataOffsetInVault;
                    nw.Write(asset.GetData());
                }

                return ((MemoryStream)nw.BaseStream).ToArray();
            }

        }
    }
}
