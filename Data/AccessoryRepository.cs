using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Text;
using System.Web.Script.Serialization;
using BikeRental.Data.Models;

namespace BikeRental.Data
{
    // Third repository. Same pattern as the other two. Yes, this could be a generic base class.
    // The moment has passed. We move forward together, in silence, with three nearly identical files.
    public class AccessoryRepository
    {
        private readonly string _filePath;

        public AccessoryRepository(string filePath)
        {
            _filePath = filePath;
        }

        // Retrieves accessories via the standard two-layer cache strategy.
        // One AppDomain is spun up, one JSON file is parsed, one domain is immediately destroyed.
        // The JSON file is about 400 bytes. The infrastructure surrounding it is not.
        public List<Accessory> GetAll()
        {
            var cachePath = _filePath + ".bin";

            if (BinaryFormatterCache.IsFresh(cachePath, _filePath))
                return BinaryFormatterCache.Read<List<Accessory>>(cachePath);

            var accessories = IsolatedDataLoader.LoadAccessories(_filePath);
            BinaryFormatterCache.Write(cachePath, accessories);
            return accessories;
        }

        // Writes accessories to disk under the legal authority of a [FileIOPermission] attribute
        // that is fully enforced in .NET 4.8 and silently ignored in .NET 5+.
        // It's a bouncer that retired and forgot to take down the velvet rope.
        [FileIOPermission(SecurityAction.Demand, Unrestricted = true)]
        public void Save(List<Accessory> accessories)
        {
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(accessories);
            File.WriteAllText(_filePath, json, Encoding.UTF8);

            // Keep the cache honest. Unlike certain unnamed developers.
            BinaryFormatterCache.Write(_filePath + ".bin", accessories);
        }
    }
}
