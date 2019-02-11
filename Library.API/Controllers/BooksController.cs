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
using System.Linq;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        ILibraryRepository libraryRepository;
        ILogger<BooksController> logger;
        IUrlHelper urlHelper;
        public BooksController(ILibraryRepository LibraryRepository, ILogger<BooksController> Logger,IUrlHelper UrlHelper)
        {
            libraryRepository = LibraryRepository;
            logger = Logger;
            urlHelper = UrlHelper;
        }
        [HttpGet(Name ="GetBooks")]
        public IActionResult GetAuthorBooks(Guid authorId,[FromHeader(Name ="Accept")] string mediaType)
        {
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var books = libraryRepository.GetBooksForAuthor(authorId);
            var booksVM = Mapper.Map<IEnumerable<BookVM>>(books);
            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                booksVM = booksVM.Select(book => { book = CreatebookLinks(book); return book; });
                var wrapper = new LinkedCollectionResourceWrapperVM<BookVM>(booksVM);
                return Ok(CreateBooksLinks(wrapper));
            }
            else
            {
                return Ok(booksVM);
            }
        }
        [HttpGet("{id}", Name = "GetBook")]
        public IActionResult GetAuthorBook(Guid authorId, Guid id, [FromHeader(Name = "Accept")] string mediaType)
        {
            if (!libraryRepository.AuthorExists(authorId))
                return NotFound();
            var book = libraryRepository.GetBookForAuthor(authorId, id);
            if (book == null)
                return NotFound();
            var bookVM = Mapper.Map<BookVM>(book);
            if (mediaType == "application/vnd.marvin.hateoas+json")
                return Ok(CreatebookLinks(bookVM));
            else
                return Ok(bookVM);
        }
        [HttpPost(Name ="CreateBook")]
        public IActionResult CreateBook(Guid authorId, [FromBody] BookCreateVM bookVM, [FromHeader(Name = "Accept")] string mediaType)
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
            if (mediaType == "application/vnd.marvin.hateoas+json")
                return CreatedAtRoute("GetBook", new { authorId = authorId, id = createdbook.Id }, CreatebookLinks(createdbook));
            else
                return CreatedAtRoute("GetBook", new { authorId = authorId, id = createdbook.Id }, createdbook);
        }
        [HttpDelete("{id}",Name ="DeleteBook")]
        public IActionResult DeleteAuthorBook(Guid authorId, Guid id)
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
        [HttpPut("{id}",Name ="FullyUpdateBook")]//HttpPut full update 
        public IActionResult FullyUpdateAuthorBook(Guid authorId, Guid id, [FromBody] BookUpdateVM bookVM)
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
        [HttpPatch("{id}",Name ="PartiallyUpdateBook")]
        public IActionResult PartiallyUpdateAuthorBook(Guid authorId, Guid id, [FromBody]JsonPatchDocument<BookUpdateVM> bookPatch)
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

        private BookVM CreatebookLinks(BookVM book)
        {
            book.Links.Add(new LinkVM(urlHelper.Link("GetBook",new { id=book.Id}),"self","Get"));
            book.Links.Add(new LinkVM(urlHelper.Link("DeleteBook",new { id = book.Id }),"delete_book","DELETE"));
            book.Links.Add(new LinkVM(urlHelper.Link("FullyUpdateBook",new { id = book.Id }),"update_book","PUT"));
            book.Links.Add(new LinkVM(urlHelper.Link("PartiallyUpdateBook",new { id = book.Id }),"partially_update_book","PATCH"));
            return book;
        }
        private LinkedCollectionResourceWrapperVM<BookVM> CreateBooksLinks(LinkedCollectionResourceWrapperVM<BookVM> booksWrapper)
        {
            // link to self
            booksWrapper.Links.Add(new LinkVM(urlHelper.Link("GetBooks", new { }),"self","GET"));
            return booksWrapper;
        }
    }
}