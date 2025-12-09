using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FifaAttribDbAppPlugin.AttribDb
{
    public class FIFAAttribDbGameplayVLTReader : NativeReader
    {
        public FIFAAttribDbGameplayVLTReader(string filePath) : base(filePath)
        {
        }

        public FIFAAttribDbGameplayVLTReader(byte[] vltData) : base(vltData)
        {
            this.Position = 0;
            this.ReadBytes(32); // Skip header
            this.Pad(16);
        }
    }
}
