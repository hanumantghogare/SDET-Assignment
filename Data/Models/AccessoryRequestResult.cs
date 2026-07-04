using System;

namespace BikeRental.Data.Models
{
    // The receipt. Success=true means you got your stuff. Success=false means you didn't,
    // and Message contains an explanation written under mild pressure.
    // [Serializable] because it needs to cross the AppDomain boundary on the way back.
    // Or rather, it used to. .NET 5 removed the boundary. The attribute stays anyway,
    // like a stamp on an envelope that was never sent.
    [Serializable]
    public class AccessoryRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public double TotalPrice { get; set; }

        // How much money the bundle deal saved the customer.
        // Usually zero. Sometimes not zero. The math is always correct,
        // which is more than can be said for the architecture.
        public double DiscountAmount { get; set; }
        public bool BundleDiscountApplied { get; set; }
    }
}
