using System;

namespace BikeRental.Data.Models
{
    // A thing you can attach to a bike, or just buy, or theoretically return.
    // We don't track returns. That would require another service and another repository
    // and another AppDomain and frankly we've all been through enough.
    // [Serializable] because bytes love this class and it has learned to accept that.
    [Serializable]
    public class Accessory
    {
        public int AccessoryID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public double UnitPrice { get; set; }
        public int StockCount { get; set; }

        // Valid values: "mountain", "beach", "all". Not "hovercraft". We checked.
        public string[] CompatibleWith { get; set; }
    }
}
