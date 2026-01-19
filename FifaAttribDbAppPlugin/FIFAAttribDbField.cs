using System;
using System.Collections.Generic;
using System.Text;

namespace FifaAttribDbAppPlugin
{
    public struct FIFAAttribDbField
    {
        public string Name { get; set; }

        public ulong Hash { get; set; }

        public byte[] Value { get; set; }

        public FIFAAttribDbField(string name, byte[] value, ulong hash)
        {
            Name = name;
            Value = value;
            Hash = hash;
        }

        public override string ToString()
        {
            return $"{Name}:{Hash}";
        }
    }
}
