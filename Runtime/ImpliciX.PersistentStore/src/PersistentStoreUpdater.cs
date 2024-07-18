using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.PersistentStore
{
    public class PersistentStoreUpdater
    {
        private readonly IWriteToStorage _storageWriter;

        public PersistentStoreUpdater(IWriteToStorage storageWriter)
        {
            _storageWriter = storageWriter;
        }

        private HashValue CreateHashValue(IDataModelValue modelValue) =>
            modelValue.ModelValue() switch
            {
                FunctionDefinition f =>
                    new HashValue(
                            modelValue.Urn,
                            f.Params.Select(p => (p.Key, p.Value.ToString(CultureInfo.InvariantCulture))).ToArray())
                        .SetAtField(modelValue.At),
                _ => new HashValue(modelValue.Urn, modelValue.ModelValue().ToString(), modelValue.At)
            };

        public DomainEventHandler<PersistentChangeRequest> HandlePersistentChangeRequested(
            IDictionary<Type, int> settingKinds) =>
            persistentChangeRequest =>
                (from modelValue in persistentChangeRequest.ModelValues
                    let hashValue = CreateHashValue(modelValue)
                    let settingKind = modelValue.Urn.GetType().GetGenericTypeDefinition()
                    where settingKinds.ContainsKey(settingKind)
                    let db = settingKinds[settingKind]
                    select _storageWriter.WriteHash(db, hashValue)).Traverse()
                .Match(
                    _ => new DomainEvent[] { },
                    __ => new DomainEvent[] { }
                );
    }
}