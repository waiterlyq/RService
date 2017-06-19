using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWCF
{
   public class RQueue:Queue<string>
    {
        public static RQueue _instance;

        private static readonly object lockHelper = new object();

        private RQueue() { }

        public static RQueue getInstance()
        {
            if(_instance == null)
            {
                lock (lockHelper)
                {
                    if (_instance == null)
                        _instance = new RQueue();
                }
            }
            return _instance;
        }
    }
}
