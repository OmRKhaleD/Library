using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public abstract class AuthorManipulate
    {
        [Required(ErrorMessage = "You should fill out a first name.")]
        [MaxLength(100, ErrorMessage = "The first name shouldn't have more than 100 characters.")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "You should fill out a second name.")]
        [MaxLength(100, ErrorMessage = "The second name shouldn't have more than 100 characters.")]
        public string LastName { get; set; }
        public virtual DateTimeOffset DateOfBirth { get; set; }
        [MaxLength(100, ErrorMessage = "The genre shouldn't have more than 100 characters.")]
        public virtual string Genre { get; set; }

        public ICollection<BookCreateVM> Books { get; set; }
       = new List<BookCreateVM>();
    }
}
