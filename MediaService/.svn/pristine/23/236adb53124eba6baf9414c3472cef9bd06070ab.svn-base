using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class UpRadioMessage
    {
        public ulong File { get; set; }
        public byte Count { get; set; }

        public List<byte> Indexs { get; private set; }

        public UpRadioMessage(ulong file, byte count, byte index)
        {
            File = file;
            Count = count;
            Indexs = new List<byte>();
            Indexs.Add(index);
        }

        public void AddIndexs(byte index)
        {
            if (!Indexs.Contains(index))
                Indexs.Add(index);
        }

        public bool IsCompleted()
        {
            if (Indexs.Count == Count)
                return true;
            else
                return false;
        }
    }
}
