using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public abstract class LinkedResourceBaseVM
    {
        public List<LinkVM> Links { get; set; } = new List<LinkVM>();
    }
}
