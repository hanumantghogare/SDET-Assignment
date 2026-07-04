using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Text;
using System.Web.Script.Serialization;
using BikeRental.Data.Models;

namespace BikeRental.Data
{
    // Structurally identical to BeachCruiserRepository but uses JSON instead of XML
    // and refuses to acknowledge the resemblance. They went to the same school.
    // They have the same job. They have never spoken.
    public class MountainBikeRepository
    {
        private readonly string _filePath;

        public MountainBikeRepository(string filePath)
        {
            _filePath = filePath;
        }

        // Same two-layer caching strategy as BeachCruiserRepository.
        // Cache hit: fast. Cache miss: AppDomain detour, BinaryFormatter sidecar, then fast.
        // The fast path requires surviving the slow path at least once.
        public List<MountainBike> GetAll()
        {
            var cachePath = _filePath + ".bin";

            if (BinaryFormatterCache.IsFresh(cachePath, _filePath))
                return BinaryFormatterCache.Read<List<MountainBike>>(cachePath);

            var bikes = IsolatedDataLoader.LoadMountainBikes(_filePath);
            BinaryFormatterCache.Write(cachePath, bikes);
            return bikes;
        }

        // The [FileIOPermission] attribute is load-bearing in .NET 4.8.
        // In .NET 5+ it is purely decorative, like a tie on a golden retriever.
        // The retriever is fine with this. The security model is not.
        [FileIOPermission(SecurityAction.Demand, Unrestricted = true)]
        public void Save(List<MountainBike> bikes)
        {
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(bikes);
            File.WriteAllText(_filePath, json, Encoding.UTF8);

            // Update the cache so GetAll() doesn't return yesterday's inventory with confidence.
            // See BeachCruiserRepository.Save for the full cautionary tale.
            BinaryFormatterCache.Write(_filePath + ".bin", bikes);
        }
    }
}
