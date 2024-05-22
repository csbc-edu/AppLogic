using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppLogic
{
    internal class Key
    {
        private Tuple<int, int> _impl;
        public int Id { get; set; }
        public int Bit { get; set; }

        public Key(int id, int bit)
        {
            Id = id;
            Bit = bit;
            _impl = Tuple.Create(id, bit);
        }

        public override int GetHashCode()
        {
            return _impl.GetHashCode();
        }
    }
}
