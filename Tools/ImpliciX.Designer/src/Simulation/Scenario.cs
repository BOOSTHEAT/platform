using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using ImpliciX.Language.Core;

namespace ImpliciX.Designer.Simulation
{
    public class Scenario
    {
        public IList<MessageEvent> Events { get; }
        public Scenario(List<MessageEvent> events)
        {
            Events = events.OrderBy(e => e.At).ToList();
        }

        public Scenario(StreamReader reader)
        {
            var events = new List<MessageEvent>();
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            csvReader.Configuration.HasHeaderRecord = false;
            MessageEvent currentMessageEvent = null;
            var add = true;
            foreach (var line in csvReader.GetRecords<Line>())
            {
                (currentMessageEvent, add) = line.Type switch
                {
                    MessageEvent.command => (new MessageEvent(TimeSpan.Parse(line.At, CultureInfo.InvariantCulture), line.Type).Add(
                        line.Argument, line.Value), true),
                    MessageEvent.properties => (new MessageEvent(TimeSpan.Parse(line.At, CultureInfo.InvariantCulture), line.Type).Add(
                        line.Argument, line.Value), true),
                    MessageEvent.empty when currentMessageEvent != null && !currentMessageEvent.IsCommand() => 
                        (currentMessageEvent.Add(line.Argument, line.Value), false),
                    MessageEvent.empty when currentMessageEvent == null => 
                        throw new NotSupportedException($"Invalid definition:{Environment.NewLine} '{line.At},{line.Type},{line.Argument},{line.Value}'"),
                    _ => throw new NotSupportedException($"The command {line.Type} is not supported.")
                };
                if (add) events.Add(currentMessageEvent);
            }

            Events = events.OrderBy(e => e.At).ToList();
        }

        public static Result<Scenario> Create(StreamReader reader)
        {
            try
            {
                return Result<Scenario>.Create(new Scenario(reader));
            }
            catch (Exception e)
            {
                return new Error("scenario_creation", $"Error creating a scenario:{Environment.NewLine}{e.Message}");
            }
        }
        
        public static Result<Scenario> Create(string scenarioFilePath)
        {
            try
            {
                return Create(new StreamReader(scenarioFilePath));
            }
            catch (Exception e)
            {
                return new Error("scenario_creation", $"Error creating a scenario:{Environment.NewLine}{e.Message}");
            }
        }
    }
}