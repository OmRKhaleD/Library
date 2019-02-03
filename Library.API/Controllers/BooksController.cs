using AutoMapper;
using Library.API.Entity;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        ILibraryRepository libraryRepository;
        public BooksController(ILibraryRepository LibraryRepository)
        {
            libraryRepository = LibraryRepository;
        }
        [HttpGet()]
        public IActionResult GetAuthorBooks(Guid authorId)
        {
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var books = libraryRepository.GetBooksForAuthor(authorId);
            var booksVM = Mapper.Map<IEnumerable<BookVM>>(books);
            return Ok(booksVM);
        }
        [HttpGet("{id}", Name = "GetBook")]
        public IActionResult GetAuthorBook(Guid authorId,Guid id)
        {
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var book = libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
                return NotFound();
            var bookVM = Mapper.Map<BookVM>(book);
            return Ok(bookVM);
        }
        [HttpPost]
        public IActionResult CreateBook(Guid authorId,[FromBody] CreateBookVM bookVM)
        {
            if (bookVM == null)
                return BadRequest();
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var book = Mapper.Map<Book>(bookVM);
            libraryRepository.AddBookForAuthor(authorId, book);
            if (!libraryRepository.Save())
                throw new Exception($"Createing book for {authorId} failed to save");
            var createdbook = Mapper.Map<BookVM>(book);
            return CreatedAtRoute("GetBook", new { authorId = authorId, id = createdbook.Id }, createdbook);
        }
        [HttpDelete("{id}")]
        public IActionResult DeleteAuthorBook(Guid authorId,Guid id)
        {
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var book = libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
                return NotFound();
            libraryRepository.DeleteBook(book);
            if (!libraryRepository.Save())
                throw new Exception($"Delete book {id} for author {authorId} failed on server");
            return NoContent();
        }
    }
}
