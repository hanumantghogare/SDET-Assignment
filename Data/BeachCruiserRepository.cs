using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Permissions;
using System.Xml.Linq;
using BikeRental.Data.Models;

namespace BikeRental.Data
{
    // Knows where the beach bikes live. Has strong opinions about XML.
    // It chose XML in 2003 and it is staying the course.
    public class BeachCruiserRepository
    {
        private readonly string _filePath;

        public BeachCruiserRepository(string filePath)
        {
            _filePath = filePath;
        }

        // Fast path: check the binary sidecar cache.
        // Slow path: spin up a child AppDomain, parse XML through a proxy, marshal the results
        //            back across the domain boundary, and serialize everything to a .bin file
        //            using a deprecated serializer so the next call can be fast.
        // This is the fast path.
        public List<BeachCruiser> GetAll()
        {
            var cachePath = _filePath + ".bin";

            if (BinaryFormatterCache.IsFresh(cachePath, _filePath))
                return BinaryFormatterCache.Read<List<BeachCruiser>>(cachePath);

            var bikes = IsolatedDataLoader.LoadBeachCruisers(_filePath);
            BinaryFormatterCache.Write(cachePath, bikes);
            return bikes;
        }

        // Persists bikes back to XML. The [FileIOPermission] attribute above this line
        // is fully enforced in .NET 4.8 and completely ignored in .NET 5+,
        // making it a decorative demand — like a "do not enter" sign bolted to an open field.
        [FileIOPermission(SecurityAction.Demand, Unrestricted = true)]
        public void Save(List<BeachCruiser> bikes)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("beach_cruisers",
                    bikes.Select(b => new XElement("bike",
                        new XElement("bike_id",       b.bike_id),
                        new XElement("bike_name",     b.bike_name),
                        new XElement("color",         b.color),
                        new XElement("frame_size",    b.frame_size),
                        new XElement("price_per_day", b.price_per_day.ToString(CultureInfo.InvariantCulture)),
                        new XElement("available",     b.available.ToString().ToLower()),
                        new XElement("description",   b.description)
                    ))
                )
            );
            doc.Save(_filePath);

            // Also update the cache, or the next GetAll() will confidently return stale data.
            // This was discovered during a demo. The demo was not going well already.
            BinaryFormatterCache.Write(_filePath + ".bin", bikes);
        }
    }
}
