using FifaAttribDbAppPlugin.AttribDb;
using FMT.Core.Readers.FIFA;
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
    }
}
