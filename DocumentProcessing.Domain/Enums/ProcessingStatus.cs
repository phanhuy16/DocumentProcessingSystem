using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentProcessing.Domain.Enums
{
    public enum ProcessingStatus
    {
        Uploaded = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3
    }
}
