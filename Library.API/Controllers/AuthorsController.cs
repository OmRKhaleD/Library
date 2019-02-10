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
        IPropertyMappingService propertyMappingService;
        ITypeHelperService typeHelperService;
        public AuthorsController(ILibraryRepository LibraryRepository, ILogger<AuthorsController> Logger,IUrlHelper UrlHelper,
            IPropertyMappingService PropertyMappingService, ITypeHelperService TypeHelperService)
        {
            libraryRepository = LibraryRepository;
            logger = Logger;
            urlHelper = UrlHelper;
            propertyMappingService = PropertyMappingService;
            typeHelperService = TypeHelperService;
        }
        [HttpGet(Name ="GetAuthors")]
        public IActionResult GetAuthors(AuthorResourceParams authorResourceParams)
        {
            if (!propertyMappingService.ValidMappingExistsFor<AuthorVM, Author>(authorResourceParams.OrderBy))
                return BadRequest();
            if (!typeHelperService.TypeHasProperties<AuthorVM>(authorResourceParams.Fields))
                return BadRequest();
            var authors = libraryRepository.GetAuthors(authorResourceParams);

            var paginationMetadata = new
            {
                totalCount = authors.TotalCount,
                pageSize = authors.PageSize,
                currentPage = authors.CurrentPage,
                totalPages = authors.TotalPages
            };

            Response.Headers.Add("X-Pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));
            var authorsVM = Mapper.Map<IEnumerable<AuthorVM>>(authors);

            var links = CreateAuthorsLinks(authorResourceParams, authors.HasNext, authors.HasPrevious);
            var shapedAuthors = authorsVM.ShapeData(authorResourceParams.Fields);
            var shapedAuthorsWithLinks = shapedAuthors.Select(a => {
                var authorAsDictionary = a as IDictionary<string, object>;
                var authorLinks = CreateAuthorLinks((Guid)authorAsDictionary["Id"], authorResourceParams.Fields);
                authorAsDictionary.Add("links", authorLinks);
                return authorAsDictionary;
            });

            var linkedCollectionResource = new
            {
                value = shapedAuthorsWithLinks,
                links = links
            };
            return Ok(linkedCollectionResource);
        }
        private string CreateAuthorResourceUri(AuthorResourceParams authorResourceParams,ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return urlHelper.Link("GetAuthors",
                      new
                      {
                          fields = authorResourceParams.Fields,
                          orderBy=authorResourceParams.OrderBy,
                          searchQuery = authorResourceParams.SearchQuery,
                          genre = authorResourceParams.Genre,
                          pageNumber = authorResourceParams.PageNumber - 1,
                          pageSize = authorResourceParams.PageSize
                      });
                case ResourceUriType.NextPage:
                    return urlHelper.Link("GetAuthors",
                      new
                      {
                          fields = authorResourceParams.Fields,
                          orderBy = authorResourceParams.OrderBy,
                          searchQuery = authorResourceParams.SearchQuery,
                          genre = authorResourceParams.Genre,
                          pageNumber = authorResourceParams.PageNumber + 1,
                          pageSize = authorResourceParams.PageSize
                      });
                case ResourceUriType.Current:
                default:
                    return urlHelper.Link("GetAuthors",
                    new
                    {
                        fields = authorResourceParams.Fields,
                        orderBy = authorResourceParams.OrderBy,
                        searchQuery = authorResourceParams.SearchQuery,
                        genre = authorResourceParams.Genre,
                        pageNumber = authorResourceParams.PageNumber,
                        pageSize = authorResourceParams.PageSize
                    });
            }
        }
        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id,[FromQuery] string fields)
        {
            if (!typeHelperService.TypeHasProperties<AuthorVM>(fields))
                return BadRequest();
            var author = libraryRepository.GetAuthor(id);
            if (author == null)
                return NotFound();
            var authorVM = Mapper.Map<AuthorVM>(author);
            var links = CreateAuthorLinks(id, fields);

            var linkedResourceToReturn = authorVM.ShapeData(fields)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
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
            var links = CreateAuthorLinks(createdAuthor.Id, null);
            var linkedResourceToReturn = createdAuthor.ShapeData(null)
                as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);
            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
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
        [HttpDelete("{id}",Name ="DeleteAuthor")]
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
        [HttpPut("{id}",Name = "FullyUpdateAuthor")]//HttpPut full update 
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
        [HttpPatch("{id}",Name = "PartiallyUpdateAuthor")]
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

        private IEnumerable<LinkVM> CreateAuthorLinks(Guid id, string fields)
        {
            var links = new List<LinkVM>();
            if (string.IsNullOrWhiteSpace(fields))
                links.Add(new LinkVM(urlHelper.Link("GetAuthor", new { id = id }),"self","GET"));
            else
                links.Add(new LinkVM(urlHelper.Link("GetAuthor", new { id = id, fields = fields }),"self","GET"));
            links.Add(new LinkVM(urlHelper.Link("DeleteAuthor", new { id = id }),"delete_author","DELETE"));
            links.Add(new LinkVM(urlHelper.Link("FullyUpdateAuthor", new { id = id }), "update_author", "PUT"));
            links.Add(new LinkVM(urlHelper.Link("PartiallyUpdateAuthor", new { id = id }), "partially_update_author", "PATCH"));
            links.Add(new LinkVM(urlHelper.Link("CreateBook", new { authorId = id }),"create_book_for_author","POST"));
            links.Add(new LinkVM(urlHelper.Link("GetBooksForAuthor", new { authorId = id }),"books","GET"));
            return links;
        }

        private IEnumerable<LinkVM> CreateAuthorsLinks(AuthorResourceParams authorResourceParams,bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkVM>();
            links.Add(new LinkVM(CreateAuthorResourceUri(authorResourceParams,ResourceUriType.Current), "self", "GET"));
            if (hasNext)
                links.Add(new LinkVM(CreateAuthorResourceUri(authorResourceParams, ResourceUriType.NextPage),"nextPage", "GET"));
            if (hasPrevious)
                links.Add(new LinkVM(CreateAuthorResourceUri(authorResourceParams, ResourceUriType.PreviousPage),"previousPage", "GET"));
            return links;
        }
    }
}