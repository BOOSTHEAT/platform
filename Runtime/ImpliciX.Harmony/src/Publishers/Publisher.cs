using System;
using System.Collections.Generic;
using ImpliciX.Harmony.Messages;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Harmony.Publishers
{
  public abstract class Publisher
  {
    protected Publisher(Queue<IHarmonyMessage> elementsQueue)
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

    protected readonly Queue<IHarmonyMessage> ElementsQueue;
  }
}