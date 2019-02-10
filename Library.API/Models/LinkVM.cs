using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class LinkVM
    {
        public string Href { get; set; }
        public string Rel { get; set; }
        public string Method { get; set; }

        public LinkVM(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }

    }
}
