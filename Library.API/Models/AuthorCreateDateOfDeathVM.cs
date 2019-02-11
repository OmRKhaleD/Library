using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class AuthorCreateDateOfDeathVM
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public virtual DateTimeOffset DateOfBirth { get; set; }
        public virtual DateTimeOffset? DateOfDeath { get; set; }
        public virtual string Genre { get; set; }
    }
}
