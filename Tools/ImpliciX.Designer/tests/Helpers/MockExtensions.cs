using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Moq;

namespace ImpliciX.Designer.Tests.Helpers;

public static class MockExtensions
{
  public static void NotifyPropertyChanged<T>(this Mock<T> obj, string propertyName)
    where T : class, INotifyPropertyChanged
  {
    obj.Raise(
      x => x.PropertyChanged += null,
      obj, 
      new PropertyChangedEventArgs(propertyName)
    );
  }
  
  public static Subject<TObs> SetupObservable<T, TObs>(this Mock<T> obj, Expression<Func<T, IObservable<TObs>>> expression)
    where T : class, INotifyPropertyChanged
  {
    var subject = new Subject<TObs>();
    obj.Setup(expression).Returns(subject);
    return subject;
  }
}