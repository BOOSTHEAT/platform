using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath;

internal static class DataModelValueExtensions
{
  public static Option<FloatValueAt> ToFloatValueAtOption(this Option<DataModelValue<float>> dmFloat)
    => dmFloat.Map(modelValue => new FloatValueAt(modelValue.At, modelValue.Value));
}