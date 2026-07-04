using System.Collections.Generic;
using System.Linq;
using BikeRental.Data;

namespace BikeRental.Services
{
    // Mechanically identical to BeachCruiserService but for bikes that go uphill.
    // IDs start at 101 because someone left room for 100 beach cruisers.
    // There are six beach cruisers. There will always be six beach cruisers.
    public class MountainBikeService
    {
        private readonly MountainBikeRepository _repo;

        // Same deal as BeachCruiserService._defaults — the canonical initial state.
        // Modify the JSON file without updating this and ResetToDefaults() will produce
        // a result that is confidently, silently wrong. The bikes will smile and say nothing.
        private static readonly Dictionary<int, bool> _defaults = new Dictionary<int, bool>
        {
            { 101, true  },
            { 102, true  },
            { 103, false },
            { 104, true  },
            { 105, true  },
            { 106, false },
        };

        public MountainBikeService(MountainBikeRepository repo)
        {
            _repo = repo;
        }

        // Sets IsAvailable to false and saves. Two things happen.
        // One line each. The surrounding object graph is moral support.
        public bool RentBike(int bikeId)
        {
            var bikes = _repo.GetAll();
            var bike = bikes.FirstOrDefault(b => b.BikeID == bikeId);

            if (bike == null || !bike.IsAvailable)
                return false;

            bike.IsAvailable = false;
            _repo.Save(bikes);
            return true;
        }

        // Returns all bikes to their default states. Like herding cats,
        // but the cats are booleans and they can't run away.
        public void ResetToDefaults()
        {
            var bikes = _repo.GetAll();
            foreach (var bike in bikes)
            {
                bool original;
                if (_defaults.TryGetValue(bike.BikeID, out original))
                    bike.IsAvailable = original;
            }
            _repo.Save(bikes);
        }
    }
}
