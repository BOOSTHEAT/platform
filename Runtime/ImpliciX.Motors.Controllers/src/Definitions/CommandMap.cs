using System;
using System.Collections.Generic;
using ImpliciX.Data;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Domain;

namespace ImpliciX.Motors.Controllers.Definitions
{
    public class CommandMap
    {
        public CommandMap(Motor[] allMotors)
        {
            _dictionaryDefinitions = new Dictionary<Urn, ICommandDefinition>();
            foreach (var (motorId, motor) in allMotors)
            {
                var commandDefinition = new CommandDefinition<RotationalSpeed>(motorId, motor._setpoint, RotationalSpeed.FromFloat(DEFAULT_MOTOR_ROTATIONAL_SPEED).Value);
                _dictionaryDefinitions.Add(motor._setpoint.command, commandDefinition);
            }
        }

        public MotorIds MotorIdFrom(Urn commandUrn)
        {
            return _dictionaryDefinitions[commandUrn].MotorId;
        }
        
        public IDataModelValue[] EvalSuccess(Urn urn, object arg,  TimeSpan at)
        {
            Debug.PreCondition(()=>_dictionaryDefinitions.ContainsKey(urn),()=>$"{urn} is not present in the command map");
            var def = _dictionaryDefinitions[urn];
            return def.EvalSuccess(arg, at);
        }
        
        public IDataModelValue EvalFailed(Urn urn, TimeSpan at)
        {
            Debug.PreCondition(()=>_dictionaryDefinitions.ContainsKey(urn),()=>$"{urn} is not present in the command map");
            var def = _dictionaryDefinitions[urn];
            return def.EvalFailed(at);
        }
        
        public bool ContainsUrn(Urn crUrn) => _dictionaryDefinitions.ContainsKey(crUrn);


        private const int DEFAULT_MOTOR_ROTATIONAL_SPEED = 29;
        private Dictionary<Urn, ICommandDefinition> _dictionaryDefinitions;



    }

    public interface ICommandDefinition
    {
        IDataModelValue[] EvalSuccess(object arg, TimeSpan at);
        IDataModelValue EvalFailed(TimeSpan at);
        MotorIds MotorId { get; }
    }

    public class CommandDefinition<T> : ICommandDefinition
    {
        public MotorIds MotorId { get; }
        private CommandUrn<T> CommandUrn { get; }
        private PropertyUrn<T> MeasureUrn { get; }
        private PropertyUrn<MeasureStatus> StatusUrn { get; }
        
        public T DefaultValue { get; }

        public CommandDefinition(MotorIds motorId, CommandNode<T> commandNode, T defaultValue)
        {
            MotorId = motorId;
            DefaultValue = defaultValue;
            CommandUrn = commandNode.command;
            MeasureUrn = commandNode.measure;
            StatusUrn = commandNode.status;
        }

        public IDataModelValue[] EvalSuccess(object arg, TimeSpan at) => 
            new IDataModelValue[]
            {
                Property<T>.Create(MeasureUrn, (T)arg, at),
                Property<MeasureStatus>.Create(StatusUrn, MeasureStatus.Success, at)
            };
        public IDataModelValue[] EvalDefault(TimeSpan at) =>
            EvalSuccess(DefaultValue, at);
        public IDataModelValue EvalFailed(TimeSpan at) =>
            Property<MeasureStatus>.Create(StatusUrn, MeasureStatus.Failure, at);

       
    }
}