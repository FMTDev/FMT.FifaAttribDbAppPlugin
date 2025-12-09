using FifaAttribDbAppPlugin.AttribDb;
using FifaAttribDbAppPlugin.AttribDbGameplay;
using System;
using System.Collections.Generic;
using System.Text;

namespace FifaAttribDbAppPlugin
{
    public class FIFAAttribDbService
    {
        public FIFAAttribDbService() { }

        public List<string> Types { get; } = new List<string>();

        public void ReadAttribDbBinary(byte[] data)
        {
            var reader = new FIFAAttribDbBinaryReader(data);
        }

        public void ReadAttribDbVlt(byte[] data)
        {

        }


        public void ReadAttribDbGameplayBinary(byte[] data)
        {
            var reader = new FIFAAttribDbGameplayBinaryReader(data);
        }

        public void ReadAttribDbGameplayVlt(byte[] data)
        {

        }
    }
}
