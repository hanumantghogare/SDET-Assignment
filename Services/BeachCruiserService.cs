using System.Collections.Generic;
using System.Linq;
using BikeRental.Data;

namespace BikeRental.Services
{
    // Beach bike business logic. "Business logic" is generous —
    // it's a bool flip and a save call. But it's our bool flip,
    // it has a unit-testable home, and we're proud of it.
    public class BeachCruiserService
    {
        private readonly BeachCruiserRepository _repo;

        // The ground truth for which bikes start available and which are already
        // "mysteriously" out. Hardcoded because the alternative was a config file,
        // and config files have feelings and opinions and sometimes need their own config files.
        private static readonly Dictionary<int, bool> _defaults = new Dictionary<int, bool>
        {
            { 1, true  },
            { 2, true  },
            { 3, false },
            { 4, true  },
            { 5, true  },
            { 6, false },
        };

        public BeachCruiserService(BeachCruiserRepository repo)
        {
            _repo = repo;
        }

        // Marks a bike as rented. Returns true if this worked.
        // Returns false if the bike is already gone, doesn't exist,
        // or has somehow become unavailable between now and the last time you checked.
        public bool RentBike(int bikeId)
        {
            var bikes = _repo.GetAll();
            var bike = bikes.FirstOrDefault(b => b.bike_id == bikeId);

            if (bike == null || !bike.available)
                return false;

            bike.available = false;
            _repo.Save(bikes);
            return true;
        }

        // Restores all bikes to their original availability states.
        // Only reachable by clicking a spinning emoji on the home page.
        // Documented here rather than in a user manual because there is no user manual.
        public void ResetToDefaults()
        {
            var bikes = _repo.GetAll();
            foreach (var bike in bikes)
            {
                bool original;
                if (_defaults.TryGetValue(bike.bike_id, out original))
                    bike.available = original;
            }
            _repo.Save(bikes);
        }
    }
}
