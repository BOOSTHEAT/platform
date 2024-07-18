using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Functions
{
  public class Interpolation
  {
    public static FuncRef Func => new FuncRef(nameof(Interpolation), () => Runner, xUrns=>xUrns);
    public static FunctionRun Runner
    {
      get
      {
        var interpol = new Interpolation();
        return (functionDefinition, xs) => interpol.Execute(functionDefinition, xs);
      }
    }
    
    public float Execute(FunctionDefinition functionDefinition, (float value, TimeSpan at)[] xs)
    {
      Contract.Assert(xs.Length == 1, "Scaling function takes one variable");
      var points = GetPoints(functionDefinition);
      var intervals = points
        .Zip(points.Skip(1), (p1,p2)=>(p1,p2))
        .Select(IntervalSlope);
      return Compute(xs[0].value, intervals.ToArray());
    }
    
    

    private static (float x, float y)[] GetPoints(FunctionDefinition fd)
    {
      if (fd.ParamExist("Xmin"))
      {
        float Xmin = fd.GetValueParam(nameof(Xmin));
        float Xmax = fd.GetValueParam(nameof(Xmax));
        float Ymin = fd.GetValueParam(nameof(Ymin));
        float Ymax = fd.GetValueParam(nameof(Ymax));
        return new[] { (Xmin, Ymin), (Xmax, Ymax) };
      }

      var points = new List<(float, float)>();
      for (int i = 0;; i++)
      {
        var xname = "X" + i;
        if (!fd.ParamExist(xname))
          return points.ToArray();
        var point = (fd.GetValueParam(xname), fd.GetValueParam("Y" + i));
        points.Add(point);
      }
    }

    private static ((float x, float y) p1,(float x, float y) p2, float slope) IntervalSlope(((float x, float y) p1,(float x, float y) p2) interval)
    {
      var slope = (interval.p2.y - interval.p1.y) / (interval.p2.x - interval.p1.x);
      return (interval.p1,interval.p2,slope);
    }

    private static float Compute(float x, ((float x, float y) p1,(float x, float y) p2, float slope)[] intervals)
    {
      var p0 = intervals.First().p1;
      if (x < p0.x)
        return p0.y;
      foreach (var (p1,p2,slope) in intervals)
      {
        if(x < p2.x)
          return slope * (x - p1.x) + p1.y;
      } 
      return intervals.Last().p2.y;
    }
  }
}