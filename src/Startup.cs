using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PostGhost
{
    public class Startup
    {
        private string sasToken = string.Empty;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            sasToken = Configuration["sasToken"];
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("{*AllValues}", async context => {
                    await context.Response.WriteAsync("Accepted!!!");
                    HandleRequest(context);
                });

                endpoints.MapPost("{*AllValues}", async context => {
                    await context.Response.WriteAsync("Accepted!!!");
                    HandleRequest(context);
                });

                endpoints.MapPut("{*AllValues}", async context => {
                    await context.Response.WriteAsync("Accepted!!!");
                    HandleRequest(context);
                });

                endpoints.MapDelete("{*AllValues}", async context => {
                    await context.Response.WriteAsync("Accepted!!!");
                    HandleRequest(context);
                });

                endpoints.Map("{*AllValues}", async context => {
                    await context.Response.WriteAsync("Accepted!!!");
                    HandleRequest(context);
                });
            });
        }

        public void HandleRequest(HttpContext context)
        {
            PostGhostHit ghostHit = new PostGhostHit(context);

            BlobClient client = new BlobClient(sasToken, "requests", $"{DateTime.UtcNow.ToString("yy-MM-dd-hh-mm-ss")}-{ghostHit.HitGuid}.txt");

            client.Upload(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(ghostHit.ToString())));

            if(context.Request.ContentLength != null)
            try
            {
                BlobClient fileUploadClient = new BlobClient(sasToken, "bodys", $"{ghostHit.HitGuid}.txt");
                var body = new StreamReader(context.Request.Body).ReadToEndAsync().Result;
                fileUploadClient.Upload(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(body)));
            }
            catch(Exception ex)
            {
            }

            System.Diagnostics.Debug.Write("Upload Completed!");
        }
    }

    public class PostGhostHit
    {
        public PostGhostHit(HttpContext context)
        {
            HitGuid = Guid.NewGuid().ToString();

            HitTime = DateTime.UtcNow.ToString();

            Headers = JsonConvert.SerializeObject(context.Request.Headers);

            Method = context.Request.Method;

            Path = $"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}";
        }

        public string HitGuid { get; set; }

        public string HitTime { get; set; }

        public string Headers { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string CallerData { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
