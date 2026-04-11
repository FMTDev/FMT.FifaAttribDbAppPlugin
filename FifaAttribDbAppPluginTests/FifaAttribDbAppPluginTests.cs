using FifaAttribDbAppPlugin;
using FifaAttribDbAppPlugin.AttribDb;
using FifaAttribDbAppPlugin.AttribDbGameplay;
using FMT.FileTools;
using FMT.ServicesManagers;
using FMT.ServicesManagers.Interfaces;
using System.Diagnostics;

namespace FifaAttribDbAppPluginTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            SingletonService.RegisterInstance<IAssetManagementService, AssetManagementMockForTests>(new AssetManagementMockForTests());
        }

        [Test]
        public void ReadAttribDbAttribDbBinary()
        {
            var msAttribDb_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDb.attribdb.BIN").CopyTo(msAttribDb_Data);
            new FIFAAttribDbBinaryReader(msAttribDb_Data.ToArray());
        }

        [Test]
        public void ReadAttribDbAttribDbGameplayBinary()
        {
            var msAttribDb_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msAttribDb_Data);
            new FIFAAttribDbBinaryReader(msAttribDb_Data.ToArray());
        }

        [Test]
        public void ReadAttribDbAttribDbGameplayVault()
        {
            var msAttribDb_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msAttribDb_Data);
            new FIFAAttribDbGameplayVLTReader(msAttribDb_Data.ToArray());
        }

        [Test]
        public void ReadAttribDbAttribDbGameplay()
        {
            var msAttribDb_Vlt_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msAttribDb_Vlt_Data);
            var msAttribDb_Bin_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msAttribDb_Bin_Data);

            var service = new FIFAAttribDbService();
            service.Load(msAttribDb_Vlt_Data.ToArray(), msAttribDb_Bin_Data.ToArray());
            _ = service.Assets;
        }

        [Test]
        public void ReadWriteAttribDbAttribDbGameplayVault()
        {
            var msAttribDb_Vlt_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msAttribDb_Vlt_Data);
            var msAttribDb_Bin_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.BIN").CopyTo(msAttribDb_Bin_Data);

            var attribDbVltData = msAttribDb_Vlt_Data.ToArray();
            var attribDbBinData = msAttribDb_Bin_Data.ToArray();

            FIFAAttribDbService service = new FIFAAttribDbService();
            service.Load(attribDbVltData, attribDbBinData);

            var movement = service.GetAssetEntry("actor/movement");
            //movement.AttribDbType.Fields.First(x => x.Name == "ATTR_DribbleJogSpeed").Value = 999.9;

            var writtenVltData = new FIFAAttribDbVLTWriter().WriteToBytes(service.Assets, attribDbVltData).Result;

            attribDbVltData = msAttribDb_Vlt_Data.ToArray();
//#if DEBUG
//            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "writtenAttribDbGameplay.VLT");
//            File.WriteAllBytes(path, writtenVltData);
//#endif

            for(var i = 0; i < attribDbVltData.Length; i++)
            {
                var vanillaByte = attribDbVltData[i];
                var writtenByte = writtenVltData[i];
                if (writtenByte != vanillaByte)
                {
                    Debug.WriteLine($"Byte at position {i} is different. Vanilla: {vanillaByte}, Written: {writtenByte}");
                    throw new Exception($"Byte at position {i} is different. Vanilla: {vanillaByte}, Written: {writtenByte}");
                }
            }

        }

    }
}
