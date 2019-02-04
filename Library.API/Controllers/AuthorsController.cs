using AutoMapper;
using Library.API.Entity;
using Library.API.Helper;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Controllers
{    
    [Route("api/authors")]
    public class AuthorsController : Controller
    { 
        ILibraryRepository libraryRepository;
        public AuthorsController(ILibraryRepository LibraryRepository)
        {
            libraryRepository = LibraryRepository;
        }
        [HttpGet()]
        public IActionResult GetAuthors()
        {
            var authors = libraryRepository.GetAuthors();
            var authorsVM = Mapper.Map<IEnumerable<AuthorVM>>(authors);
            return Ok(authorsVM);
        }
        [HttpGet("{id}",Name ="GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            var author = libraryRepository.GetAuthor(id);
            if (author == null)
                return NotFound();
            var authorVM = Mapper.Map<AuthorVM>(author);
            return Ok(authorVM);
        }
        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorCreateVM authorVM)
        {
            if (authorVM == null)
                return BadRequest();
            var author = Mapper.Map<Author>(authorVM);
            libraryRepository.AddAuthor(author);
            if (!libraryRepository.Save())
                throw new Exception("Creating author failed to save");
            var createdAuthor = Mapper.Map<AuthorVM>(author);
            return CreatedAtRoute("GetAuthor", new { id = createdAuthor.Id }, createdAuthor);
        }
        [HttpPost("authorcollections")]
        public IActionResult CreateAuthors([FromBody]IEnumerable<AuthorCreateVM> authorVM)
        {
            if (authorVM == null)
                return BadRequest();
            var authors = Mapper.Map<IEnumerable<Author>>(authorVM);
            foreach (var item in authors)
            {
                libraryRepository.AddAuthor(item);
            }
            if (!libraryRepository.Save())
                throw new Exception("Creating collection of authors failed to save");
            var created = Mapper.Map<IEnumerable<AuthorVM>>(authors);
            var id = string.Join(",", created.Select(a => a.Id));
            return CreatedAtRoute("GetAuthors",new { ids=id},created);
        }
        [HttpGet("({ids})",Name ="GetAuthors")]
        public IActionResult GetAuthorCollection([ModelBinder(BinderType =typeof(IdsModelBinder))]IEnumerable<Guid> ids)
        {
            if (ids == null)
                return BadRequest();
            var authors = libraryRepository.GetAuthors(ids);
            if (ids.Count() != authors.Count())
                return NotFound();
            var authorsVM = Mapper.Map<IEnumerable<AuthorVM>>(authors);
            return Ok(authorsVM);
        }
        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (libraryRepository.AuthorExists(id))
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            return NotFound();
        }
        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var author = libraryRepository.GetAuthor(id);
            if (author == null)
                return NotFound();
            libraryRepository.DeleteAuthor(author);
            if (!libraryRepository.Save())
                throw new Exception($"Delete author {id} failed on server");
            return NoContent();
        }
        [HttpPut("{id}")]//HttpPut full update 
        public IActionResult fullyUpdateAuthor(Guid id, [FromBody] AuthorUpdateVM authorVM)
        {
            if (authorVM == null)
                return BadRequest();
            var author = libraryRepository.GetAuthor(id);
            if (author == null)
            {
                //Upserting: using HttpPut to insert
                var authorAdd = Mapper.Map<Author>(authorVM);
                authorAdd.Id = id;
                libraryRepository.AddAuthor(authorAdd);
                if (!libraryRepository.Save())
                    throw new Exception($"Createing author for {id} failed to save");
                var createdauthor = Mapper.Map<AuthorVM>(authorAdd);
                return CreatedAtRoute("GetAuthor", new { id = createdauthor.Id }, createdauthor);
            }
            Mapper.Map(authorVM, author);
            libraryRepository.UpdateAuthor(author);
            if (!libraryRepository.Save())
                throw new Exception($"Update author {id} failed on server");
            return NoContent();
        }
        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateAuthor(Guid id, [FromBody]JsonPatchDocument<AuthorUpdateVM> authorPatch)
        {
            if (authorPatch == null)
                return BadRequest();
            var author = libraryRepository.GetAuthor(id);
            if (author == null)
            {
                //Upserting: using HttpPatch to insert
                var newAuthorVM = new AuthorUpdateVM();
                authorPatch.ApplyTo(newAuthorVM);
                var authorAdd = Mapper.Map<Author>(newAuthorVM);
                authorAdd.Id = id;
                libraryRepository.AddAuthor(authorAdd);
                if (!libraryRepository.Save())
                    throw new Exception($"Createing author for {id} failed to save");
                var createdauthor = Mapper.Map<AuthorVM>(authorAdd);
                return CreatedAtRoute("GetAuthor", new { id = createdauthor.Id }, createdauthor);
            }
            var authorVM = Mapper.Map<AuthorUpdateVM>(author);
            authorPatch.ApplyTo(authorVM);
            Mapper.Map(authorVM, author);
            libraryRepository.UpdateAuthor(author);
            if (!libraryRepository.Save())
                throw new Exception($"Update author {id} failed on server");
            return NoContent();
        }
    }
}
