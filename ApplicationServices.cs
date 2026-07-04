using System.IO;
using BikeRental.Data;
using BikeRental.Services;

namespace BikeRental
{
    // The application's object graph, assembled by hand and stored in static fields.
    // In ASP.NET Core, this would be replaced by IServiceCollection.AddSingleton<T>()
    // and constructor injection, and everything would have an interface, and there would
    // be unit tests, and the tests would pass, and everyone would feel good about themselves.
    //
    // This is not that. This is a static class. It was simpler at the time.
    // It is still simpler. Whether "simpler" is good is left as an exercise.
    public static class ApplicationServices
    {
        public static BeachCruiserRepository BeachRepo { get; private set; }
        public static MountainBikeRepository MountainRepo { get; private set; }
        public static AccessoryRepository    AccessoryRepo { get; private set; }
        public static BeachCruiserService    BeachService { get; private set; }
        public static MountainBikeService    MountainService { get; private set; }
        public static AccessoryService       AccessoryService { get; private set; }

        // Called once from Application_Start. Calling it twice recreates all repositories,
        // abandons the old ones mid-operation, and makes the background thread very confused.
        // There is no guard against this. The application extends trust freely and naively.
        public static void Initialize(string dataFolder)
        {
            BeachRepo    = new BeachCruiserRepository(Path.Combine(dataFolder, "beach_cruisers.xml"));
            MountainRepo = new MountainBikeRepository(Path.Combine(dataFolder, "mountain_bikes.json"));
            AccessoryRepo = new AccessoryRepository(Path.Combine(dataFolder, "accessories.json"));

            BeachService    = new BeachCruiserService(BeachRepo);
            MountainService = new MountainBikeService(MountainRepo);
            AccessoryService = new AccessoryService(AccessoryRepo);
        }
    }
}
