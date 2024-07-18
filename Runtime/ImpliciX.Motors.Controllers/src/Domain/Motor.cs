using ImpliciX.Language.Model;

namespace ImpliciX.Motors.Controllers.Domain
{
    public class Motor
    {
        public Motor(MotorIds id, MotorNode model)
        {
            Id = id;
            Model = model;
        }

        public void Deconstruct(out MotorIds id, out MotorNode model)
        {
            id = Id;
            model = Model;
        }

        public MotorIds Id { get; }
        public MotorNode Model { get; }
    }
}