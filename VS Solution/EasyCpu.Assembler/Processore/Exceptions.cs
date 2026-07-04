using EasyCpu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCpu.Assembler.Processore
{
    public class CpuTrapException : ApplicationException
    {
        public CpuTrapException() { }
    }

    public class CpuLoopException : ApplicationException
    {
        public CpuLoopException() { }
    }

    public class CpuException : ApplicationException
    {
        public CodiceErrore err;
        public CpuException(CodiceErrore err)
        {
            this.err = err;
        }
        public CpuException() { }
    }

    public class CodiceException : CpuException
    {
        public CodiceException(CodiceErrore err) : base(err) { }
    }
}
