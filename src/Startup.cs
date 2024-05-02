using System;
using System.IO;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace PostGhost
{
    public class Startup
    {
        private string sasToken = string.Empty;

        private bool sasMode;

        private string storageAccount = string.Empty;
        
        private bool msiMode;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            sasToken = Configuration["sasToken"];
            
            storageAccount = Configuration["storageAccount"];

            if (sasToken != null)
            {
                sasMode = true;
            }
            else if (storageAccount != null)
            {
                msiMode = true;
            }
            else
            {
                throw new Exception("Startup Failed!  No SAS token or Storage Account Details!");
            }
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

            string requestName = $"{DateTime.UtcNow.ToString("yy-MM-dd-hh-mm-ss")}-{ghostHit.HitGuid}.txt";

            try
            {

            }
            catch (Exception)
            {
            }

            if (sasMode)
            {
                BlobClient client = new BlobClient(sasToken, "requests", requestName);

                UploadContextData(client, ghostHit);

                if (context.Request.ContentLength != null)
                {
                    BlobClient fileUploadClient = new BlobClient(sasToken, "bodys", $"{ghostHit.HitGuid}.txt");

                    UploadContextFileData(fileUploadClient, context);
                }
            }
            else if (msiMode)
            {
                BlobServiceClient serviceClient = new BlobServiceClient(
                    new Uri($"https://{storageAccount}.blob.core.windows.net"),
                    new ManagedIdentityCredential());

                BlobContainerClient containerClient = serviceClient.GetBlobContainerClient("requests");

                BlobClient client = containerClient.GetBlobClient(requestName);

                UploadContextData(client, ghostHit);

                if (context.Request.ContentLength != null)
                {
                    BlobContainerClient bodysContainerClient = serviceClient.GetBlobContainerClient("bodys");

                    BlobClient fileUploadClient = bodysContainerClient.GetBlobClient($"{ghostHit.HitGuid}.txt");

                    UploadContextFileData(fileUploadClient, context);
                }
            }
            else
            {
                // We should never get here...
                throw new Exception("Handling Failed!  No SAS token or Storage Account Details!");
            }

            System.Diagnostics.Debug.Write("Upload Completed!");
        }

        public void UploadContextData(BlobClient client, PostGhostHit ghostHit)
        {
            client.Upload(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(ghostHit.ToString())));
        }

        public void UploadContextFileData(BlobClient fileUploadClient, HttpContext context)
        {
            var body = new StreamReader(context.Request.Body).ReadToEndAsync().Result;
            fileUploadClient.Upload(new MemoryStream(System.Text.Encoding.ASCII.GetBytes(body)));
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
