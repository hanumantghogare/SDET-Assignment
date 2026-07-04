using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using BikeRental.Data.Models;

namespace BikeRental.Data
{
    // Loads data files inside freshly-created child AppDomains because loading them
    // directly was apparently too straightforward. The child domain does the same work
    // the parent could have done, just with significantly more scaffolding and slightly
    // worse performance. This is called "isolation." It is also called "a lot."
    //
    // AppDomain.CreateDomain() was removed in .NET 5 with the official explanation that
    // it was "confusing and error-prone." The removal was also confusing and error-prone.
    // We consider the irony load-bearing.
    internal static class IsolatedDataLoader
    {
        // Hires a temporary AppDomain to read a small XML file, then immediately fires it.
        // The domain is given a name, a purpose, and a lifespan of roughly 40 milliseconds.
        // It does not get benefits.
        public static List<BeachCruiser> LoadBeachCruisers(string filePath)
        {
            var domain = AppDomain.CreateDomain(
                "BeachCruiserLoader",
                null,
                new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(typeof(DataLoaderProxy).Assembly.Location) });
            try
            {
                var proxy = (DataLoaderProxy)domain.CreateInstanceAndUnwrap(
                    typeof(DataLoaderProxy).Assembly.FullName,
                    typeof(DataLoaderProxy).FullName);
                return proxy.LoadBeachCruisers(filePath);
            }
            finally
            {
                // The domain has served its purpose. Unload it before it starts asking questions.
                AppDomain.Unload(domain);
            }
        }

        // Same architecture. Different file format. The AppDomain does not know or care.
        public static List<MountainBike> LoadMountainBikes(string filePath)
        {
            var domain = AppDomain.CreateDomain(
                "MountainBikeLoader",
                null,
                new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(typeof(DataLoaderProxy).Assembly.Location) });
            try
            {
                var proxy = (DataLoaderProxy)domain.CreateInstanceAndUnwrap(
                    typeof(DataLoaderProxy).Assembly.FullName,
                    typeof(DataLoaderProxy).FullName);
                return proxy.LoadMountainBikes(filePath);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }

        // Water bottles and bike lights also get their own private AppDomain.
        // Nobody asked whether this was proportionate. Nobody is asking now.
        public static List<Accessory> LoadAccessories(string filePath)
        {
            var domain = AppDomain.CreateDomain(
                "AccessoryLoader",
                null,
                new AppDomainSetup { ApplicationBase = Path.GetDirectoryName(typeof(DataLoaderProxy).Assembly.Location) });
            try
            {
                var proxy = (DataLoaderProxy)domain.CreateInstanceAndUnwrap(
                    typeof(DataLoaderProxy).Assembly.FullName,
                    typeof(DataLoaderProxy).FullName);
                return proxy.LoadAccessories(filePath);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
    }

    // The short-lived resident of a child AppDomain. Given exactly one task and then evicted.
    // Must be public so the runtime can construct it via reflection across the domain wall.
    // Must inherit MarshalByRefObject so method calls are proxied rather than copied.
    // Return types must be [Serializable] so results can cross back to the parent domain.
    // Several of these requirements become compile errors or runtime exceptions in .NET 5+.
    // How many, and which ones first, is left as a surprise.
    public class DataLoaderProxy : MarshalByRefObject
    {
        // Parses XML while confined to a sandboxed process. Results are marshalled home
        // via proxy, like a report from someone who was never allowed to leave the building.
        public List<BeachCruiser> LoadBeachCruisers(string filePath)
        {
            var bikes = new List<BeachCruiser>();
            var doc = XDocument.Load(filePath);

            foreach (var el in doc.Root.Elements("bike"))
            {
                bikes.Add(new BeachCruiser
                {
                    bike_id       = int.Parse(el.Element("bike_id").Value),
                    bike_name     = el.Element("bike_name").Value,
                    color         = el.Element("color").Value,
                    frame_size    = el.Element("frame_size").Value,
                    price_per_day = decimal.Parse(el.Element("price_per_day").Value, CultureInfo.InvariantCulture),
                    available     = bool.Parse(el.Element("available").Value),
                    description   = el.Element("description").Value
                });
            }

            return bikes;
        }

        // Parses JSON in exile. The JSON has no idea it's in a child AppDomain.
        // The JSON is fine. Everyone is fine.
        public List<MountainBike> LoadMountainBikes(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<List<MountainBike>>(json);
        }

        // A third method doing the same thing for a third type. The pattern is obvious.
        // The refactoring is left as an exercise for the next developer, who will also not do it.
        public List<Accessory> LoadAccessories(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<List<Accessory>>(json);
        }
    }
}
