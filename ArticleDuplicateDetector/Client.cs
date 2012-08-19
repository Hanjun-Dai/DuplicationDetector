using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Palas.Common.Data;
using System.Threading;
using HooLab.Log;

namespace Analyzer.Core.Algorithm.FingerPrint
{
    class Client
    {
        static void Main(string[] args)
        {
            DateTime? t = null;
            Console.WriteLine((DateTime.Now - t) == DetectorFacade.DetectPeriod ? 0 : 1);
            Console.ReadLine();
        }
    }
}
