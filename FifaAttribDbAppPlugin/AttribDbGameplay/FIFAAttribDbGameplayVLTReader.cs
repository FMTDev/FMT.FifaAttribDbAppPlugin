using FifaAttribDbAppPlugin.AttribDb;
using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FifaAttribDbAppPlugin.AttribDbGameplay
{
    public class FIFAAttribDbGameplayVLTReader : FIFAAttribDbVLTReader
    {
        public FIFAAttribDbGameplayVLTReader(string filePath) : base(filePath)
        {
        }

        public FIFAAttribDbGameplayVLTReader(byte[] vltData) : base(vltData)
        {
          
        }
    }
}
