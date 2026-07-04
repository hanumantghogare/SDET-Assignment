using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using BikeRental.Data.Models;

namespace BikeRental.Handlers
{
    // Same IHttpHandler pattern as BikeHandler. One action per handler file was the convention.
    // The convention was not always followed. It was more of a suggestion, really.
    // A vibe. An aspiration from a simpler time when HTTP verbs were just GET and POST
    // and PUT was something you did to files via WebDAV.
    public class AccessoryHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Cache.SetExpires(System.DateTime.UtcNow.AddDays(-1));
            context.Response.Cache.SetNoStore();
            context.Response.BufferOutput = true;
            context.Response.ContentType = "application/json";

            var method = context.Request.HttpMethod.ToUpper();

            if (method == "GET")
                WriteAccessories(context);
            else if (method == "POST")
                ProcessOrder(context);
            else
            {
                context.Response.StatusCode = 405;
                context.Response.Write("{\"error\":\"Method not allowed. This handler speaks GET and POST.\"}");
            }
        }

        private void WriteAccessories(HttpContext context)
        {
            // QueryString["bikeType"] returns null if absent. Null is handled gracefully here,
            // less gracefully in the twelve other places this pattern appears in projects from this era.
            var bikeType = context.Request.QueryString["bikeType"];

            IEnumerable<Accessory> accessories = string.IsNullOrEmpty(bikeType)
                ? ApplicationServices.AccessoryService.GetAll()
                : ApplicationServices.AccessoryService.GetCompatibleWith(bikeType);

            var encoded = accessories.Select(a => new Accessory
            {
                AccessoryID    = a.AccessoryID,
                Name           = context.Server.HtmlEncode(a.Name),
                Category       = context.Server.HtmlEncode(a.Category),
                Description    = context.Server.HtmlEncode(a.Description),
                UnitPrice      = a.UnitPrice,
                StockCount     = a.StockCount,
                CompatibleWith = a.CompatibleWith
            }).ToList();

            // JavaScriptSerializer handles arrays and nested types without configuration.
            // It will also happily serialize circular references until it hits a stack overflow.
            // There are no circular references here. Mention this in the code review to seem prepared.
            var serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(encoded));
        }

        private void ProcessOrder(HttpContext context)
        {
            var body = new StreamReader(context.Request.InputStream).ReadToEnd();

            if (string.IsNullOrWhiteSpace(body))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("{\"error\":\"Empty request body. An order with no items is just intent.\"}");
                return;
            }

            var serializer = new JavaScriptSerializer();
            var items = serializer.Deserialize<List<AccessoryOrderItem>>(body);

            if (items == null || !items.Any(i => i.Quantity > 0))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("{\"error\":\"No items with quantity > 0. Nothing to do here.\"}");
                return;
            }

            var quantities = new Dictionary<int, int>();
            foreach (var item in items.Where(i => i.Quantity > 0))
                quantities[item.AccessoryID] = item.Quantity;

            var result = ApplicationServices.AccessoryService.ProcessOrder(quantities);
            context.Response.Write(serializer.Serialize(result));
        }
    }

    // Used by JavaScriptSerializer to deserialize the incoming order array.
    // Public properties, parameterless constructor, no attributes needed.
    // JavaScriptSerializer is not fussy about what it's given. It's also not thorough.
    // The two qualities are related.
    public class AccessoryOrderItem
    {
        public int AccessoryID { get; set; }
        public int Quantity    { get; set; }
    }
}
