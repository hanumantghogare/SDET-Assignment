using System;
using System.Collections.Generic;
using System.Linq;
using BikeRental.Data;
using BikeRental.Data.Models;

namespace BikeRental.Services
{
    // The most complex service in the application, which is either a sign of good
    // domain modeling or a deeply unhealthy relationship with water bottles.
    public class AccessoryService
    {
        private readonly AccessoryRepository _repo;

        // The bundle deal IDs — hardcoded here because a config file was "too much work."
        // These same two IDs are now hardcoded in this file, the HTML pages, and
        // at least one dream someone had. Distributed consistency through copy-paste.
        private static readonly HashSet<int> _bundleIds = new HashSet<int> { 1, 3 };
        private const double BundleDiscountRate = 0.10;

        // Original stock levels. If you change accessories.json, update this too,
        // or the reset will restore stock to whatever it was when someone wrote this comment,
        // which was a Tuesday, and which may not reflect your current business reality.
        private static readonly Dictionary<int, int> _defaultStock = new Dictionary<int, int>
        {
            { 1, 15 },
            { 2,  8 },
            { 3, 20 },
            { 4,  6 },
        };

        public AccessoryService(AccessoryRepository repo)
        {
            _repo = repo;
        }

        // Restores all accessory stock counts to their original values.
        // Water bottles do not replenish themselves. Unfortunately.
        public void ResetToDefaults()
        {
            var accessories = _repo.GetAll();
            foreach (var acc in accessories)
            {
                int original;
                if (_defaultStock.TryGetValue(acc.AccessoryID, out original))
                    acc.StockCount = original;
            }
            _repo.Save(accessories);
        }

        // Returns every accessory, unfiltered, with no opinions.
        // The most honest method in this file.
        public List<Accessory> GetAll()
        {
            return _repo.GetAll();
        }

        // Returns only accessories that won't cause a compatibility incident on the given bike type.
        // Accepts "mountain" or "beach". Does not accept "hovercraft", "penny-farthing", or null.
        public List<Accessory> GetCompatibleWith(string bikeType)
        {
            var lower = bikeType.ToLower();
            return _repo.GetAll()
                .Where(a => Array.IndexOf(a.CompatibleWith, lower) >= 0
                         || Array.IndexOf(a.CompatibleWith, "all") >= 0)
                .ToList();
        }

        // Validates, prices, discounts, commits, and reports on an accessory order.
        // Three passes over the inventory because doing it in one would require thinking harder.
        //
        // The bundle deal: order both a water bottle (ID 1) AND a bike light (ID 3)
        // and get 10% off the whole order. Marketing called it "synergy."
        // We do not talk to marketing.
        public AccessoryRequestResult ProcessOrder(Dictionary<int, int> quantities)
        {
            if (quantities == null || !quantities.Any(q => q.Value > 0))
                return Fail("No items selected.");

            var inventory = _repo.GetAll();

            // Pass 1: verify we have what they want before touching anything.
            foreach (var pair in quantities.Where(q => q.Value > 0))
            {
                var acc = inventory.FirstOrDefault(a => a.AccessoryID == pair.Key);
                if (acc == null)
                    return Fail(string.Format("Accessory #{0} does not exist. Bold choice.", pair.Key));

                if (acc.StockCount < pair.Value)
                    return Fail(string.Format(
                        "Only {0} {1}(s) in stock. Requesting {2} is ambitious.",
                        acc.StockCount, acc.Name, pair.Value));
            }

            // Pass 2: add up the damage.
            double subtotal = 0;
            foreach (var pair in quantities.Where(q => q.Value > 0))
            {
                var acc = inventory.First(a => a.AccessoryID == pair.Key);
                subtotal += acc.UnitPrice * pair.Value;
            }

            // Check for the bundle deal. Both IDs must be present. Partial credit not awarded.
            var requestedIds = new HashSet<int>(quantities.Where(q => q.Value > 0).Select(q => q.Key));
            bool bundleApplies = _bundleIds.IsSubsetOf(requestedIds);
            double discount = bundleApplies ? Math.Round(subtotal * BundleDiscountRate, 2) : 0;

            // Pass 3: commit. The inventory changes. There is no undo. This is intentional.
            foreach (var pair in quantities.Where(q => q.Value > 0))
            {
                var acc = inventory.First(a => a.AccessoryID == pair.Key);
                acc.StockCount -= pair.Value;
            }
            _repo.Save(inventory);

            return new AccessoryRequestResult
            {
                Success = true,
                Message = bundleApplies
                    ? "Bundle discount applied! Water bottle + light: a lifestyle choice we support."
                    : "Order confirmed! Your accessories await.",
                TotalPrice            = Math.Round(subtotal - discount, 2),
                DiscountAmount        = discount,
                BundleDiscountApplied = bundleApplies
            };
        }

        // Constructs a failure result without repeating yourself.
        // The one method in this file that does exactly what it says with no drama whatsoever.
        private static AccessoryRequestResult Fail(string message)
        {
            return new AccessoryRequestResult { Success = false, Message = message };
        }
    }
}
