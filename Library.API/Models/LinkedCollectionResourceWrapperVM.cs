using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class LinkedCollectionResourceWrapperVM<T> : LinkedResourceBaseVM
        where T : LinkedResourceBaseVM
    {
        public IEnumerable<T> Value { get; set; }

        public LinkedCollectionResourceWrapperVM(IEnumerable<T> value)
        {
            Value = value;
        }
    }
}
