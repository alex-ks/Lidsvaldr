using Lidsvaldr.WorkflowComponents.Arguments;
using Lidsvaldr.WorkflowComponents.Executer;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidsvaldr.ModelTask
{
    class Program
    {
        static void Main(string[] args)
        {
            var converter = new ImageConverter();
            converter.PhotoPostprocessing(2);
            converter.PhotoSlicing(2, 5);
        }
    }
}
