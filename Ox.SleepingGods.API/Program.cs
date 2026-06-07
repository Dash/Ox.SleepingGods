using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Ox.SleepingGods.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateSlimBuilder(args);

			// Adds support for notify service type
			builder.Host.UseSystemd();

			// Add support for socket based activation
			builder.WebHost.ConfigureKestrel(static opt => opt.UseSystemd());

			builder.Services.ConfigureHttpJsonOptions(static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonTypesContext.Default));

			// CORS support allows any header and any origin to query this
			builder.Services.AddCors(static opts => opts.AddDefaultPolicy(static pb => pb.AllowAnyHeader()
						.AllowAnyOrigin()
						.WithMethods("OPTIONS", "GET", "PUT")
						.WithExposedHeaders("*")));

			var app = builder.Build();

			app.UseCors();

			string BuildPath(string uid)
			{
				var basePath = app.Configuration.GetValue<string>("StoreDirectory", "store");

				if (!Path.Exists(basePath))
					Directory.CreateDirectory(basePath);

				return Path.Combine(basePath, Path.GetFileNameWithoutExtension(uid) + ".json");
			}

			/**
			 * Configure idle timeout for systemd Socket Activated services.
			 * In this scenario where systemd starts the service on-demand, we want the process to
			 * shutdown to release memory etc.  This is managed by a the IdleSeconds option.
			 */
			DateTime lastRequest = DateTime.MaxValue;
			int idleTimeout;
			if ((idleTimeout = app.Configuration.GetValue<int>("IdleSeconds", 0)) > 0)
			{
				Task monitor = Task.Run(async () =>
				{
					while ((DateTime.UtcNow - lastRequest).TotalSeconds < 30)
					{
						await Task.Delay(idleTimeout * 1000);
					}
					app.Logger.LogInformation("Closing due to inactivity");
					app.Lifetime.StopApplication();
				});
			}

			app.Use(async (context, next) =>
			{
				lastRequest = DateTime.UtcNow;
				await next.Invoke(context);
			});

			int maxUploadSize = app.Configuration.GetValue<int>("MaxUploadBytes", 1024000);


			// Returns cached file, 403, or 404
			app.MapGet("/{uid}", async (HttpRequest req, string uid) =>
			{
				long seconds = -1;
				string? etag = req.Headers.IfNoneMatch.FirstOrDefault();

				if (String.IsNullOrEmpty(etag) || !Int64.TryParse(etag.AsSpan(1, etag.Length - 2), out seconds))
				{
					var cache = req.Headers.IfModifiedSince.FirstOrDefault();
					if (!String.IsNullOrEmpty(cache) && DateTimeOffset.TryParse(cache, out DateTimeOffset timestamp))
						seconds = timestamp.ToUnixTimeSeconds();
				}

				string target = BuildPath(uid);

				// 404 if the file doesn't exist
				if (!File.Exists(target))
				{
					app.Logger.LogDebug("Database {UID} doesn't exist.", uid);
					return Results.NotFound();
				}

				FileInfo info = new(target);
				long fileAge = (long)(info.LastWriteTimeUtc - DateTime.UnixEpoch).TotalSeconds;

				// 304 Not modified
				if (Math.Abs(fileAge - seconds) < 1)
				{
					app.Logger.LogDebug("Database {UID} hasn't been modified.", uid);
					return Results.StatusCode(304);
				}

				Microsoft.Net.Http.Headers.EntityTagHeaderValue hash = new($"\"{fileAge}\"");

				app.Logger.LogInformation("Serving database {UID}.", uid);

				return TypedResults.Stream(async (stream) =>
				{
					using var fs = File.OpenRead(target);
					await fs.CopyToAsync(stream);
				}, "application/json", uid + ".json", DateTimeOffset.FromUnixTimeSeconds(fileAge), hash);
			})
				.RequireCors();

			// Accept file to be stored
			app.MapPut("/{uid}", async (HttpRequest req, string uid) =>
			{
				DateTimeOffset timestamp = DateTime.UtcNow;

				if (req.Headers.ContentLength > maxUploadSize)
				{
					app.Logger.LogWarning("Upload too large {Size}", req.Headers.ContentLength);
					return Results.Problem(detail: "File size too large.", statusCode: (int)HttpStatusCode.BadRequest);
				}

				string target = BuildPath(uid);

				using (FileStream fs = new(target, FileMode.Create))
				{
					await req.Body.CopyToAsync(fs, req.HttpContext.RequestAborted);
					await fs.FlushAsync(req.HttpContext.RequestAborted);
				}

				File.SetLastAccessTimeUtc(target, timestamp.UtcDateTime);

				app.Logger.LogInformation("Database {UID} saved.", uid);

				req.HttpContext.Response.Headers.ETag = $"\"{timestamp.ToUnixTimeSeconds()}\"";
				return Results.Accepted();
			})
				.Accepts<Stream>("application/json")
				.RequireCors();

			app.Run();
		}

	}

	[JsonSerializable(typeof(ProblemDetails))]
	internal partial class JsonTypesContext : JsonSerializerContext
	{

	}
}
