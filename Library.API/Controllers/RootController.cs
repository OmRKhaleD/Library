using Library.API.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    [Route("api")]
    public class RootController : Controller
    {
        private IUrlHelper urlHelper;

        public RootController(IUrlHelper UrlHelper)
        {
            urlHelper = UrlHelper;
        }

        [HttpGet(Name = "GetRoot")]
        public IActionResult GetRoot([FromHeader(Name = "Accept")] string mediaType)
        {
            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                var links = new List<LinkVM>();
                links.Add(new LinkVM(urlHelper.Link("GetRoot", new { }),"self","GET"));
                links.Add(new LinkVM(urlHelper.Link("GetAuthors", new { }),"authors","GET"));
                links.Add(new LinkVM(urlHelper.Link("CreateAuthor", new { }),"create_author","POST"));
                return Ok(links);
            }

            return NoContent();
        }
    }
}
