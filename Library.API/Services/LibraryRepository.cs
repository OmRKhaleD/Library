using System;
using System.Collections.Generic;
using System.Linq;
using Library.API.Entity;
using Library.API.Helper;
using Library.API.Models;
using Microsoft.EntityFrameworkCore;


namespace Library.API.Services
{
    public class LibraryRepository : ILibraryRepository
    {
        LibraryDbContext context;
        IPropertyMappingService propertyMappingService;
        public LibraryRepository(LibraryDbContext _context, IPropertyMappingService _propertyMappingService)
        {
            context = _context;
            propertyMappingService = _propertyMappingService;
        }
        public void AddAuthor(Author author)
        {
            if (author.Id == Guid.Empty)
            {
                author.Id = Guid.NewGuid();
            }
            context.Authors.Add(author);

            // the repository fills the id (instead of using identity columns)
            if (author.Books.Any())
            {
                foreach (var book in author.Books)
                {
                    book.Id = Guid.NewGuid();
                }
            }
        }

        public void AddBookForAuthor(Guid authorId, Book book)
        {
            var author = GetAuthor(authorId);
            if (author != null)
            {
                if (book.Id == Guid.Empty)
                {
                    book.Id = Guid.NewGuid();
                }
                author.Books.Add(book);
            }
        }

        public bool AuthorExists(Guid authorId)
        {
            return context.Authors.Any(a => a.Id == authorId);
        }

        public void DeleteAuthor(Author author)
        {
            context.Authors.Remove(author);
        }

        public void DeleteBook(Book book)
        {
            context.Books.Remove(book);
        }

        public Author GetAuthor(Guid authorId)
        {
            return context.Authors.FirstOrDefault(a => a.Id == authorId);
        }

        public PagedList<Author> GetAuthors(AuthorResourceParams authorResourceParams)
        {
            //var collectionBeforePaging = context.Authors
            //    .OrderBy(a => a.FirstName)
            //    .ThenBy(a => a.LastName).AsQueryable();

            var collectionBeforePaging = context.Authors.ApplySort(authorResourceParams.OrderBy,propertyMappingService.GetPropertyMapping<AuthorVM, Author>());
            if (!string.IsNullOrEmpty(authorResourceParams.Genre))
            {
                var genreWhereClause = authorResourceParams.Genre.Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging.Where(a => a.Genre.ToLowerInvariant() == genreWhereClause);
            }
            if (!string.IsNullOrEmpty(authorResourceParams.SearchQuery))
            {
                // trim & ignore casing
                var searchQueryWhereClause = authorResourceParams.SearchQuery
                    .Trim().ToLowerInvariant();

                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.Genre.ToLowerInvariant().Contains(searchQueryWhereClause)
                    || a.FirstName.ToLowerInvariant().Contains(searchQueryWhereClause)
                    || a.LastName.ToLowerInvariant().Contains(searchQueryWhereClause));
            }
            return PagedList<Author>.Create(collectionBeforePaging,
                authorResourceParams.PageNumber,
                authorResourceParams.PageSize);

        }

        public IEnumerable<Author> GetAuthors(IEnumerable<Guid> authorIds)
        {
            return context.Authors.Where(a => authorIds.Contains(a.Id)).OrderBy(a => a.FirstName).ThenBy(a => a.LastName);
        }

        public Book GetBookForAuthor(Guid authorId, Guid bookId)
        {
            return context.Books.FirstOrDefault(b => b.AuthorId == authorId && b.Id == bookId);
        }

        public IEnumerable<Book> GetBooksForAuthor(Guid authorId)
        {
            return context.Books.Where(b => b.AuthorId == authorId).OrderBy(b => b.Title);
        }

        public bool Save()
        {
            return context.SaveChanges() >= 0;
        }

        public void UpdateAuthor(Author author)
        {

        }

        public void UpdateBookForAuthor(Book book)
        {

        }
    }
}