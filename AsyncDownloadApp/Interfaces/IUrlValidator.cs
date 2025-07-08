using System.Collections.Generic;

namespace AsyncDownload.Interfaces
{
    public interface IUrlValidator
    {
        (List<string> Valid, List<string> Invalid) Validate(IEnumerable<string> urls);
    }
}