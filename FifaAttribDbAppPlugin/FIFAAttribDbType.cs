using FMT.Hash;
using System;
using System.Collections.Generic;
using System.Text;

namespace FifaAttribDbAppPlugin
{
    public struct FIFAAttribDbType
    {
        public string Name { get; set; }

        public int HashInt { get; set; }

        public ulong HashLong { get; set; }

        public FIFAAttribDbType(string name)
        {
            Name = name;
            HashInt = Fnv1.HashString(name);
            HashLong = Fnv64.FNV64_String8_Lower(name);
        }

        public override string ToString()
        {
            return $"{Name}:{HashInt}:{HashLong}";
        }
    }
}
