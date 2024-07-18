using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;

namespace ImpliciX.Alarms
{
    public class AlarmBase
    {
        protected PropertyUrn<AlarmState> _alarmUrn;

        public IDataModelValue Activate(ModelFactory modelFactory, TimeSpan now) =>
            (IDataModelValue) modelFactory.CreateWithLog(_alarmUrn, AlarmState.Inactive, now).Value;
    }
}