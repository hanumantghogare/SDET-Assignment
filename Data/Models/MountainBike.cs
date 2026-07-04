using System;

namespace BikeRental.Data.Models
{
    // Also a bike, but angrier. PascalCase because mountains demand respect.
    // Completely different schema from BeachCruiser — different field names, different types,
    // different capitalization conventions. This was a deliberate choice and not an accident
    // caused by two developers who never talked to each other. Definitely deliberate.
    //
    // [Serializable]: see BeachCruiser.cs for the full eulogy. It applies here too.
    [Serializable]
    public class MountainBike
    {
        public int BikeID { get; set; }
        public string ModelName { get; set; }
        public string Brand { get; set; }
        public int GearCount { get; set; }
        public string SuspensionType { get; set; }
        public string FrameMaterial { get; set; }
        public double DailyRate { get; set; }
        public bool IsAvailable { get; set; }
        public string Terrain { get; set; }
        public double WeightKg { get; set; }
    }
}
