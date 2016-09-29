using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class SingleCallModel
    {
    }

    public class InputParamer
    {
        public string sn { get; set; }
        public int state { get; set; }
        public int tid { get; set; }
    }

    public class ReturnParamer
    {
        public bool status { get; set; }
        public int state { get; set; }
        public int sn { get; set; }
        public int tid { get; set; }
    }

    public class ReturnState
    {
        public bool status { get; set; }
        public int state { get; set; }
        public int tid { get; set; }
    }
}
