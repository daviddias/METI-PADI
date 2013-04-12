using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class ByteArrayRecord
{
    public string filenameGlobal;
    public long version;
    public byte[] content;

    public ByteArrayRecord(string filenameGlobal, long version, byte[] content)
    {
        this.filenameGlobal = filenameGlobal;
        this.version = version;
        this.content = content;
    }
}
