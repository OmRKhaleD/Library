using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class AuthorUpdateVM : AuthorManipulate
    {
        [Required(ErrorMessage = "You should fill out a date of birth.")]
        public override DateTimeOffset DateOfBirth { get => base.DateOfBirth; set => base.DateOfBirth = value; }
        [Required(ErrorMessage = "You should fill out a genre.")]
        public override string Genre { get => base.Genre; set => base.Genre = value; }
    }
}
