/*
Copyright (c) 2020 - 0xCD7F82BcFa333B4072A11Bd0B1da95c9b5f9E869 m/44'/60'/0'/0/0

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents.Indexes;
using Vidyano.Service;
using Vidyano.Service.RavenDB;
using RockStats.Service;
using System;
using System.Threading.Tasks;

namespace RockStats
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddVidyanoRavenDB(Configuration, options =>
            {
                var settings = new DatabaseSettings();
                Configuration.Bind("Database", settings);
                if (settings.CertPath != null)
                    settings.CertPath = Path.Combine(Environment.ContentRootPath, settings.CertPath);

                var store = settings.CreateStore();
                store.Conventions.AsyncDocumentIdGenerator = (dbName, entity) => // TEMP: Set settings.UseGuidIdentifiers when available (before calling CreateStore)
                {
                    var prefix = store.Conventions.GetCollectionName(entity);
                    return Task.FromResult($"{prefix[..1].ToLower() + prefix[1..]}/{Guid.NewGuid():N}");
                };

                options.Store = store;

                options.OnInitialized = () =>
                {
                    IndexCreation.CreateIndexes(typeof(Startup).Assembly, store);
                };
            });
            services.AddTransient<RockStatsContext>();
            services.AddTransient<RequestScopeProvider<RockStatsContext>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseVidyano(Environment, Configuration);
        }
    }
}