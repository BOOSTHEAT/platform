using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Logger;

namespace ImpliciX.Driver.Common.State
{
    public class DriverStateKeeper : IDriverStateKeeper
    {
        private ConcurrentDictionary<string, IDriverState> _data;

        public DriverStateKeeper()
        {
            _data = new ConcurrentDictionary<string, IDriverState>();
            Log = new SerilogLogger(Serilog.Log.Logger);
        }

        public DriverStateKeeper(IDictionary<string, IDriverState> initialData)
        {
            _data = new ConcurrentDictionary<string, IDriverState>(initialData);
            Log = new SerilogLogger(Serilog.Log.Logger);
        }

        public Result<IDriverState> TryRead(Urn urn)
        {
            return Result<IDriverState>.Create(Read(urn));
        }

        public IDriverState Read(Urn urn)
        {
            if (_data.ContainsKey(urn))
            {
                return _data[urn];
            }
            var state =
                _data.Values
                    .Where(s => s.Id.IsPartOf(urn))
                    .OrderByDescending(it => it.Id.Value)
                    .FirstOrDefault();
            if (state != null)
                return state;
            Log.Verbose("DriverStateKeeper.Read : no data for {@urn}", urn); 
            return DriverState.Empty(urn);
        }

        public IDriverState Update(IDriverState state)
        {
            return _data.AddOrUpdate(state.Id, _ => state, (_, __) => state);
        }

        public ILog Log { get; }

        public Result<Unit> TryUpdate(IDriverState state)
        {
            if (state != null && !state.IsEmpty) Update(state);
            return default(Unit);
        }
    }
}