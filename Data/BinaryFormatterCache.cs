using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BikeRental.Data
{
    // Converts objects to bytes and back again using BinaryFormatter —
    // deprecated in .NET 5, removed in .NET 7, and mourned by nobody
    // except codebases that didn't read the release notes.
    // The bytes it produces are perfectly fine. The security implications are not.
    // But this is a bike shop, so probably fine.
    internal static class BinaryFormatterCache
    {
        // Returns true if the .bin sidecar is newer than the source file it was made from.
        // Returns false in all other cases, including existential ones.
        public static bool IsFresh(string cachePath, string sourcePath)
        {
            return File.Exists(cachePath)
                && File.GetLastWriteTimeUtc(cachePath) >= File.GetLastWriteTimeUtc(sourcePath);
        }

        // Cracks open the .bin file and reconstitutes your object like digital instant noodles.
        // Requires the type to be [Serializable]. Explodes in .NET 7+ before you even ask why.
        public static T Read<T>(string cachePath)
        {
            var formatter = new BinaryFormatter();
            using (var stream = File.OpenRead(cachePath))
                return (T)formatter.Deserialize(stream);
        }

        // Flattens your entire object graph into a binary file with zero ceremony.
        // No recovery. No questions. No second thoughts. BinaryFormatter is committed.
        public static void Write<T>(string cachePath, T data)
        {
            var formatter = new BinaryFormatter();
            using (var stream = File.Create(cachePath))
                formatter.Serialize(stream, data);
        }
    }
}
