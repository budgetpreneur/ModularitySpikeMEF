using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubnubApi
{
    public class PNResult<T>
    {
        public T Result { get; set; }
        public PNStatus Status { get; set; }
    }
}
