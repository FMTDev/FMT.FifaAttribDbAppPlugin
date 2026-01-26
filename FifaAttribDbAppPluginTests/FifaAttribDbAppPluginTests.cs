using FifaAttribDbAppPlugin;
using FifaAttribDbAppPlugin.AttribDb;
using FifaAttribDbAppPlugin.AttribDbGameplay;
using FMT.FileTools;

namespace FifaAttribDbAppPluginTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
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
            var msAttribDb_Data = new MemoryStream();
            EmbeddedResourceHelper.GetEmbeddedResourceByName("Legacy.FIFA17.AttribDbGameplay.attribdb.VLT").CopyTo(msAttribDb_Data);
            //new FIFAAttribDbGameplayVLTReader(msAttribDb_Data.ToArray());

            var service = new FIFAAttribDbService();
            service.Load(msAttribDb_Data.ToArray());
            _ = service.Types;
        }
    }
}
