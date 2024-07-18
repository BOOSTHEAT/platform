// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.ReactiveUI.Controls;
using ImpliciX.Designer.ViewModels;

namespace ImpliciX.Designer
{
  public class ViewLocator : IDataTemplate
  {
    public bool SupportsRecycling => false;

    public Control Build(object data) => Build(data.GetType().FullName, data, data.GetType());

    private Control Build(string model, object data, Type vType)
    {
      var name = vType.FullName!.Replace("ViewModel", "View");
      var type = Type.GetType(name);

      if (type != null)
      {
        var obj = Activator.CreateInstance(type);
        var view = obj is IBuild ib ? (Control)ib.Build() : (Control)obj;
        if (data is IKnowMyUniqueView ikmv)
          ikmv.View = view;
        return view;
      }

      if(vType.BaseType == typeof(Object))
        return new TextBlock { Text = "Cannot find view for " + model };
      
      return Build(model, data, vType.BaseType);
    }

    public bool Match(object data)
    {
      return data is ViewModelBase || data is DockableViewModel || data is Document || data is InvisibleEdgeViewModel;
    }
  }

  public interface IBuild
  {
    object Build();
  }
}