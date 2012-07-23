using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RwLib
{
    class Program
    {
        static void Main(string[] args)
        {
            Rwlib.PcscRw rw = new Rwlib.PcscRw();
            byte[] atr = rw.Connect( rw.GetRwNames()[0] );
        }
    }
}
