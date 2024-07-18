using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Functions
{
  public static class Helpers
  {
    public static (float YMin, float YMax) ScalingParameters(FunctionDefinition functionDefinition, float MaxValue, float MinValue)
    {
      float ScaledMinValue = 0;
      float ScaledMaxValue = 0;
      var needScale = functionDefinition.ParamExist(nameof(ScaledMinValue)) && functionDefinition.ParamExist(nameof(ScaledMaxValue));

      if (needScale)
      {
        ScaledMinValue = functionDefinition.GetValueParam(nameof(ScaledMinValue));
        ScaledMaxValue = functionDefinition.GetValueParam(nameof(ScaledMaxValue));
      }
      else
      {
        ScaledMinValue = MinValue;
        ScaledMaxValue = MaxValue;
      }

      return (ScaledMinValue, ScaledMaxValue);
    }
    public static float Scaling(float x, float Xmin, float Xmax, float Ymin, float Ymax)
      => (Ymax - Ymin) / (Xmax - Xmin) * (x - Xmin) + Ymin;

    public static float Unscaling(float y, float Xmin, float Xmax, float Ymin, float Ymax)
      => (Xmax - Xmin) / (Ymax - Ymin) * (y - Ymin) + Xmin;
  }
}