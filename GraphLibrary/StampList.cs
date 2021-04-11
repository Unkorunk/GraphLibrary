using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphLibrary
{
    public class StampList
    {
        private readonly List<Stamp> _stamps = new List<Stamp>();
        private readonly List<Stamp> _deletedStamps = new List<Stamp>();

        public void AddStamp(Stamp stamp)
        {
            if (stamp == null || _stamps.Contains(stamp))
                return;

            _stamps.Add(stamp);
        }

        public void DeleteStamp(Stamp stamp)
        {
            if (stamp == null || !_stamps.Contains(stamp) || _deletedStamps.Contains(stamp))
                return;

            _stamps.Remove(stamp);
            _deletedStamps.Add(stamp);
        }

        public IReadOnlyList<Stamp> GetState() => new List<Stamp>(_stamps);

        public bool Contains(Stamp stamp) => _stamps.Contains(stamp);

        /**
         * To detect endless loops
         */
        private readonly List<Stamp> _mapStamp = new List<Stamp>();

        public string SerializeFullState()
        {
            foreach (var stamp in _stamps.Where(stamp => !_mapStamp.Contains(stamp)))
                _mapStamp.Add(stamp);
            foreach (var stamp in _deletedStamps.Where(stamp => !_mapStamp.Contains(stamp)))
                _mapStamp.Add(stamp);

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(string.Join(",", _stamps.Select(it => _mapStamp.IndexOf(it))));
            stringBuilder.Append("&");
            stringBuilder.Append(string.Join(",", _deletedStamps.Select(it => _mapStamp.IndexOf(it))));
            return stringBuilder.ToString();
        }
    }
}