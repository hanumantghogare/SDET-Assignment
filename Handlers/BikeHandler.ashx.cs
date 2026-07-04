using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using BikeRental.Data.Models;

namespace BikeRental.Handlers
{
    // IHttpHandler: the way you handled HTTP requests before Web API existed.
    // Introduced in ASP.NET 1.0. Still functional in .NET 4.8. Spiritually retired.
    // The .ashx extension stands for "Active Server Handler." Nobody says that out loud.
    //
    // When Web API 2 arrived in 2012, handlers like this quietly moved to the back of the room.
    // They still work. They have always worked. They are just no longer the cool thing.
    // They have made their peace with this.
    public class BikeHandler : IHttpHandler
    {
        // IsReusable: if true, the runtime can share one handler instance across requests
        // and you are personally responsible for making it thread-safe.
        // Most developers in 2006 set this to false and didn't think about it again.
        // Most developers in 2006 were correct.
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            // The old three-header cache-busting ritual. Required if you wanted IE6 to
            // actually send a fresh request. IE6 is gone. The ritual remains.
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            context.Response.Cache.SetNoStore();

            // Response.Buffer was the original property name. It was deprecated in favor
            // of Response.BufferOutput in .NET 2.0. Both still work in 4.8. Neither was removed.
            // This is a pattern in .NET 4.x: things accumulate rather than leaving.
            context.Response.BufferOutput = true;
            context.Response.ContentType = "application/json";

            var action = (context.Request.QueryString["action"] ?? string.Empty).ToLower();
            var method = context.Request.HttpMethod.ToUpper();

            if      (method == "GET"  && action == "beach")    WriteBeachCruisers(context);
            else if (method == "GET"  && action == "mountain") WriteMountainBikes(context);
            else if (method == "POST" && action == "rent")     ProcessRent(context);
            else if (method == "POST" && action == "reset")    ProcessReset(context);
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Write("{\"error\":\"Unknown action. Try harder.\"}");
            }
        }

        private void WriteBeachCruisers(HttpContext context)
        {
            var bikes = ApplicationServices.BeachRepo.GetAll().Select(b => new BeachCruiser
            {
                bike_id       = b.bike_id,
                // context.Server.HtmlEncode is the HttpServerUtility form of HtmlEncode.
                // It requires an active HttpContext, which you have here and will not have
                // the moment someone moves this code into a background thread or a library.
                // HttpUtility.HtmlEncode is the portable version. We use the other one.
                bike_name     = context.Server.HtmlEncode(b.bike_name),
                color         = context.Server.HtmlEncode(b.color),
                frame_size    = context.Server.HtmlEncode(b.frame_size),
                price_per_day = b.price_per_day,
                available     = b.available,
                description   = context.Server.HtmlEncode(b.description)
            }).ToList();

            // JavaScriptSerializer: introduced in .NET 3.5, deprecated in .NET 5, absent in .NET 6+.
            // Serializes public properties by name. Has a 2MB default max JSON length that will
            // bite you in production with no useful error message. Raises MaxJsonLength like a
            // gentleman and provides no stack trace hint about which field caused it.
            // Newtonsoft.Json and System.Text.Json are the replacements. Both have opinions.
            var serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(bikes));
        }

        private void WriteMountainBikes(HttpContext context)
        {
            var bikes = ApplicationServices.MountainRepo.GetAll().Select(b => new MountainBike
            {
                BikeID         = b.BikeID,
                ModelName      = context.Server.HtmlEncode(b.ModelName),
                Brand          = context.Server.HtmlEncode(b.Brand),
                GearCount      = b.GearCount,
                SuspensionType = context.Server.HtmlEncode(b.SuspensionType),
                FrameMaterial  = context.Server.HtmlEncode(b.FrameMaterial),
                DailyRate      = b.DailyRate,
                IsAvailable    = b.IsAvailable,
                Terrain        = context.Server.HtmlEncode(b.Terrain),
                WeightKg       = b.WeightKg
            }).ToList();

            var serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(bikes));
        }

        private void ProcessRent(HttpContext context)
        {
            // Reading the request body by hand. Web API's model binding did this automatically.
            // IHttpHandler does not. You get a stream. What you do with the stream is your business.
            // The stream will not read itself. Dispatch has been notified.
            var body = new StreamReader(context.Request.InputStream).ReadToEnd();
            var serializer = new JavaScriptSerializer();
            var request = serializer.Deserialize<RentBikeRequest>(body);

            if (request == null || string.IsNullOrEmpty(request.BikeType) || string.IsNullOrEmpty(request.BikeId))
            {
                context.Response.StatusCode = 400;
                context.Response.Write(serializer.Serialize(new { Success = false, Message = "Missing BikeType or BikeId. The bike cannot rent itself." }));
                return;
            }

            bool success;
            if (request.BikeType.ToLower() == "beach")
                success = ApplicationServices.BeachService.RentBike(int.Parse(request.BikeId));
            else
                success = ApplicationServices.MountainService.RentBike(int.Parse(request.BikeId));

            context.Response.Write(serializer.Serialize(new
            {
                Success = success,
                Message = success
                    ? "Rental confirmed. The bike is yours. Treat it well."
                    : "Sorry, that bike is no longer available. Someone was faster."
            }));
        }

        private void ProcessReset(HttpContext context)
        {
            ApplicationServices.BeachService.ResetToDefaults();
            ApplicationServices.MountainService.ResetToDefaults();
            ApplicationServices.AccessoryService.ResetToDefaults();

            var serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(new
            {
                Success = true,
                Message = "Fleet reset. All bikes returned. Accessories restocked. The chaos has been undone."
            }));
        }
    }

    // A POCO for deserializing the rent request body via JavaScriptSerializer.
    // JavaScriptSerializer requires a public parameterless constructor and public settable properties.
    // If a JSON field doesn't match a property, it silently ignores it rather than telling you.
    // This is JavaScriptSerializer's approach to conflict resolution: pretend it didn't happen.
    public class RentBikeRequest
    {
        public string BikeType { get; set; }
        public string BikeId   { get; set; }
    }
}
