using System;
using System.Collections.Generic;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.ThingsBoard.Messages;

namespace ImpliciX.ThingsBoard.Publishers
{
  public abstract class Publisher
  {
    protected Publisher(Queue<IThingsBoardMessage> elementsQueue)
    {
      ElementsQueue = elementsQueue;
    }
    
    public virtual void Handles(PropertiesChanged propertiesChanged)
    {
    }
    public virtual void Handles(SystemTicked systemTicked)
    {
    }

    protected static DateTime GetDateTime(DomainEvent de) => new DateTime(de.At.Ticks, DateTimeKind.Utc);

    protected readonly Queue<IThingsBoardMessage> ElementsQueue;
  }
}