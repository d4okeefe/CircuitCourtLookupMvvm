using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircuitCourtLookupMvvm.Utilities
{
    public static class FileNavigationStaticUtilities
    {
        public static string NormalizeFilePath(string path)
        {
            return System.IO.Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }
    }
}
