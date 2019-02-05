using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entity;
using Library.API.Helper;
using Library.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Library.API
{

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        IConfiguration Configuration;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddMvcOptions(opt => {
                //return supported formats when unspported formats requested
                opt.ReturnHttpNotAcceptable = true;
                //accept xml output
                opt.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                //accept xml input
                opt.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());
            });
            services.AddDbContext<LibraryDbContext>(o=>o.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<ILibraryRepository, LibraryRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,LibraryDbContext context)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder => {
                appBuilder.Run(async con =>{
                    con.Response.StatusCode = 500;
                    await con.Response.WriteAsync("Unexpexcted fault heppend, please try again later.");
                });
                });
            }
            context.EnsureSeedDataForContext();
            AutoMapper.Mapper.Initialize(config =>
            {
                config.CreateMap<Entity.Author, Models.AuthorVM>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateOfBirth.GetCurrentAge()));

                config.CreateMap<Entity.Book, Models.BookVM>();
                config.CreateMap<Models.AuthorCreateVM, Entity.Author>();
                config.CreateMap<Models.BookCreateVM, Entity.Book>();
                config.CreateMap<Models.BookUpdateVM, Entity.Book>();
                config.CreateMap<Entity.Book, Models.BookUpdateVM>();
                config.CreateMap<Models.AuthorUpdateVM, Entity.Author>();
                config.CreateMap<Entity.Author, Models.AuthorUpdateVM>();

            });
            app.UseMvc();
            //app.Run(async (context) =>
            //{
            //    await context.Response.WriteAsync("Hello World!");
            //});
        }
    }
}
