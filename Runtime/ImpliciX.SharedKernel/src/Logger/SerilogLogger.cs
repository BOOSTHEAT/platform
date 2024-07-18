using System;
using ImpliciX.Language.Core;
using Serilog;

namespace ImpliciX.SharedKernel.Logger
{
    public class SerilogLogger : ILog
    {
        private readonly ILogger _logger;

        public SerilogLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void Verbose(string messageTemplate)
        {
            _logger.Verbose(messageTemplate);
        }

        public void Verbose(string messageTemplate, params object[] propertyValues)
        {
            _logger.Verbose(messageTemplate, propertyValues);
        }

        public void Debug(string messageTemplate)
        {
            _logger.Debug(messageTemplate);
        }

        public void Debug(string messageTemplate, params object[] propertyValues)
        {
            _logger.Debug(messageTemplate, propertyValues);
        }

        public void Information(string messageTemplate)
        {
            _logger.Information(messageTemplate);
        }

        public void Information(string messageTemplate, params object[] propertyValues)
        {
            _logger.Information(messageTemplate, propertyValues);
        }

        public void Warning(string messageTemplate)
        {
            _logger.Warning(messageTemplate);
        }

        public void Warning(string messageTemplate, params object[] propertyValues)
        {
            _logger.Warning(messageTemplate, propertyValues);
        }

        public void Error(string messageTemplate)
        {
            _logger.Error(messageTemplate);
        }

        public void Error(string messageTemplate, params object[] propertyValues)
        {
            _logger.Error(messageTemplate, propertyValues);
        }

        public void Error(Exception exception, string messageTemplate)
        {
            _logger.Error(exception, messageTemplate);
        }

        public void Error(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            _logger.Error(exception, messageTemplate, propertyValues);
        }

        public void Fatal(string messageTemplate)
        {
            _logger.Fatal(messageTemplate);
        }

        public void Fatal(string messageTemplate, params object[] propertyValues)
        {
            _logger.Fatal(messageTemplate, propertyValues);
        }

        public void Fatal(Exception exception, string messageTemplate)
        {
            _logger.Fatal(exception, messageTemplate);
        }

        public void Fatal(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            _logger.Fatal(exception, messageTemplate, propertyValues);
        }
    }
}