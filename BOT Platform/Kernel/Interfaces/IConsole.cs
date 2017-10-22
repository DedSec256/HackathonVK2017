using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOT_Platform.Kernel.Interfaces
{
    interface IConsole
    {
        void Write(string text);
        string Read();
    }
}
