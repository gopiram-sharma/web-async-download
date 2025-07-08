using System;
using System.Collections.Generic;
using System.Linq;
using AsyncDownload.Interfaces;

namespace AsyncDownload.Services
{
    public class UrlValidator : IUrlValidator
    {
        public (List<string> Valid, List<string> Invalid) Validate(IEnumerable<string> urls)
        {
            var valid = new List<string>();
            var invalid = new List<string>();

            foreach (var u in urls.Select(u => u.Trim()))
            {
                if (Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    if (!valid.Contains(u, StringComparer.OrdinalIgnoreCase))
                        valid.Add(u);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(u) && !invalid.Contains(u, StringComparer.OrdinalIgnoreCase))
                        invalid.Add(u);
                }
            }
            return (valid, invalid);
        }
    }
}