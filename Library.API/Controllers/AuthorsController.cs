using AutoMapper;
using Library.API.Entity;
using Library.API.Helper;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        ILibraryRepository libraryRepository;
        ILogger<AuthorsController> logger;
        IUrlHelper urlHelper;
        public AuthorsController(ILibraryRepository LibraryRepository, ILogger<AuthorsController> Logger,IUrlHelper UrlHelper)
        {
            libraryRepository = LibraryRepository;
            logger = Logger;
            urlHelper = UrlHelper;
        }
        [HttpGet(Name ="GetAuthors")]
        public IActionResult GetAuthors(AuthorResourceParams authorResourceParams)
        {
            var authors = libraryRepository.GetAuthors(authorResourceParams);
            var previousPageLink = authors.HasPrevious ?
                CreateAuthorResourceUri(authorResourceParams,
                ResourceUriType.PreviousPage) : null;

            var nextPageLink = authors.HasNext ?
                CreateAuthorResourceUri(authorResourceParams,
                ResourceUriType.NextPage) : null;

            var paginationMetadata = new
            {
                totalCount = authors.TotalCount,
                pageSize = authors.PageSize,
                currentPage = authors.CurrentPage,
                totalPages = authors.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));
            var authorsVM = Mapper.Map<IEnumerable<AuthorVM>>(authors);
            return Ok(authorsVM);
        }
        private string CreateAuthorResourceUri(AuthorResourceParams authorResourceParams,ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return urlHelper.Link("GetAuthors",
                      new
                      {
                          searchQuery = authorResourceParams.SearchQuery,
                          genre = authorResourceParams.Genre,
                          pageNumber = authorResourceParams.PageNumber - 1,
                          pageSize = authorResourceParams.PageSize
                      });
                case ResourceUriType.NextPage:
                    return urlHelper.Link("GetAuthors",
                      new
                      {
                          searchQuery = authorResourceParams.SearchQuery,
                          genre = authorResourceParams.Genre,
                          pageNumber = authorResourceParams.PageNumber + 1,
                          pageSize = authorResourceParams.PageSize
                      });

                default:
                    return urlHelper.Link("GetAuthors",
                    new
                    {
                        searchQuery=authorResourceParams.SearchQuery,
                        genre = authorResourceParams.Genre,
                        pageNumber = authorResourceParams.PageNumber,
                        pageSize = authorResourceParams.PageSize
                    });
            }
        }
        [HttpGet("{id}", Name = "GetAuthor")]
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
            if (!ModelState.IsValid)
                return new UnprocessableEntityObject(ModelState);
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
            return CreatedAtRoute("GetAuthorCollection", new { ids = id }, created);
        }
        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection([ModelBinder(BinderType = typeof(IdsModelBinder))]IEnumerable<Guid> ids)
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
            logger.LogInformation(100, $"Author {id} was deleted");
            return NoContent();
        }
        [HttpPut("{id}")]//HttpPut full update 
        public IActionResult FullyUpdateAuthor(Guid id, [FromBody] AuthorUpdateVM authorVM)
        {
            if (authorVM == null)
                return BadRequest();
            if (!ModelState.IsValid)
                return new UnprocessableEntityObject(ModelState);
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
                authorPatch.ApplyTo(newAuthorVM, ModelState);
                TryValidateModel(newAuthorVM);
                if (!ModelState.IsValid)
                    return new UnprocessableEntityObject(ModelState);
                var authorAdd = Mapper.Map<Author>(newAuthorVM);
                authorAdd.Id = id;
                libraryRepository.AddAuthor(authorAdd);
                if (!libraryRepository.Save())
                    throw new Exception($"Createing author for {id} failed to save");
                var createdauthor = Mapper.Map<AuthorVM>(authorAdd);
                return CreatedAtRoute("GetAuthor", new { id = createdauthor.Id }, createdauthor);
            }
            var authorVM = Mapper.Map<AuthorUpdateVM>(author);
            authorPatch.ApplyTo(authorVM, ModelState);
            TryValidateModel(authorVM);
            if (!ModelState.IsValid)
                return new UnprocessableEntityObject(ModelState);
            Mapper.Map(authorVM, author);
            libraryRepository.UpdateAuthor(author);
            if (!libraryRepository.Save())
                throw new Exception($"Update author {id} failed on server");
            return NoContent();
        }
    }
}