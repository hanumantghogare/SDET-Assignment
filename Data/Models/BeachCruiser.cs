using System;

namespace BikeRental.Data.Models
{
    // A bike for the beach. Relaxed. Easygoing. snake_case properties because it's a beach thing
    // and nobody is in a hurry. PascalCase is for mountain bikes. Mountains demand formality.
    //
    // [Serializable] does two jobs here:
    //   1. Lets BinaryFormatter flatten it into bytes (deprecated in .NET 5, removed in .NET 7)
    //   2. Lets it survive crossing an AppDomain boundary (the boundary itself removed in .NET 5)
    // Both jobs become redundant on upgrade. The attribute stays and wonders what happened.
    [Serializable]
    public class BeachCruiser
    {
        public int bike_id { get; set; }
        public string bike_name { get; set; }
        public string color { get; set; }
        public string frame_size { get; set; }
        public decimal price_per_day { get; set; }
        public bool available { get; set; }
        public string description { get; set; }
    }
}
