using AutoMapper;
using Library.API.Entity;
using Library.API.Helper;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        ILibraryRepository libraryRepository;
        ILogger<BooksController> logger;
        public BooksController(ILibraryRepository LibraryRepository,ILogger<BooksController> Logger)
        {
            libraryRepository = LibraryRepository;
            logger = Logger;
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
        public IActionResult CreateBook(Guid authorId,[FromBody] BookCreateVM bookVM)
        {
            if (bookVM == null)
                return BadRequest();
            if (bookVM.Title == bookVM.Description)
                ModelState.AddModelError(nameof(BookCreateVM), "title and description can not be the same.");
            if (!ModelState.IsValid)
                return new UnprocessableEntityObject(ModelState);
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
            logger.LogInformation(100, $"Book {id} for Author {authorId} was deleted");
            return NoContent();
        }
        [HttpPut("{id}")]//HttpPut full update 
        public IActionResult FullyUpdateAuthorBook(Guid authorId, Guid id,[FromBody] BookUpdateVM bookVM)
        {
            if (bookVM == null)
                return BadRequest();
            if (bookVM.Title == bookVM.Description)
                ModelState.AddModelError(nameof(BookUpdateVM), "title and description can not be the same.");
            if (!ModelState.IsValid)
                return new UnprocessableEntityObject(ModelState);
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var book = libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
            {
                //Upserting: using HttpPut to insert
                var bookAdd = Mapper.Map<Book>(bookVM);
                bookAdd.Id = id;
                libraryRepository.AddBookForAuthor(authorId, bookAdd);
                if (!libraryRepository.Save())
                    throw new Exception($"Createing book for {authorId} failed to save");
                var createdbook = Mapper.Map<BookVM>(bookAdd);
                return CreatedAtRoute("GetBook", new { authorId = authorId, id = createdbook.Id }, createdbook);
            }
            Mapper.Map(bookVM, book);
            libraryRepository.UpdateBookForAuthor(book);
            if (!libraryRepository.Save())
                throw new Exception($"Update book {id} for author {authorId} failed on server");
            return NoContent();
        }
        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateAuthorBook(Guid authorId,Guid id,[FromBody]JsonPatchDocument<BookUpdateVM> bookPatch)
        {
            if (bookPatch == null)
                return BadRequest();
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var book = libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
            {
                //Upserting: using HttpPatch to insert
                var newBookVM = new BookUpdateVM();
                bookPatch.ApplyTo(newBookVM, ModelState);
                if (newBookVM.Title == newBookVM.Description)
                    ModelState.AddModelError(nameof(BookUpdateVM), "title and description can not be the same.");
                TryValidateModel(newBookVM);
                if (!ModelState.IsValid)
                    return new UnprocessableEntityObject(ModelState);
                var bookAdd = Mapper.Map<Book>(newBookVM);
                bookAdd.Id = id;
                libraryRepository.AddBookForAuthor(authorId, bookAdd);
                if (!libraryRepository.Save())
                    throw new Exception($"Createing book for {authorId} failed to save");
                var createdbook = Mapper.Map<BookVM>(bookAdd);
                return CreatedAtRoute("GetBook", new { authorId = authorId, id = createdbook.Id }, createdbook);
            }
            var bookVM = Mapper.Map<BookUpdateVM>(book);
            bookPatch.ApplyTo(bookVM, ModelState);
            if (bookVM.Title == bookVM.Description)
                ModelState.AddModelError(nameof(BookUpdateVM), "title and description can not be the same.");
            TryValidateModel(bookVM);
            if (!ModelState.IsValid)
                return new UnprocessableEntityObject(ModelState);
            Mapper.Map(bookVM, book);
            libraryRepository.UpdateBookForAuthor(book);
            if (!libraryRepository.Save())
                throw new Exception($"Update book {id} for author {authorId} failed on server");
            return NoContent();
        }
    }
}
