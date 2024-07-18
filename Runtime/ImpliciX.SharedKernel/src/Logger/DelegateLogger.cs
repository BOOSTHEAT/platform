using System;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Logger
{
    public class DelegateLogger : ILog
    {
        private readonly ILog _delegate;

        public static ILog Create(ILog logger) =>
            new DelegateLogger(logger);

        private DelegateLogger(ILog @delegate)
        {
            _delegate = @delegate;
        }

        public void Verbose(string messageTemplate)
        {
            _delegate.Verbose(messageTemplate);
        }

        public void Verbose(string messageTemplate, params object[] propertyValues)
        {
            _delegate.Verbose(messageTemplate, propertyValues);
        }

        public void Debug(string messageTemplate)
        {
            _delegate.Debug(messageTemplate);
        }

        public void Debug(string messageTemplate, params object[] propertyValues)
        {
            _delegate.Debug(messageTemplate, propertyValues);
        }

        public void Information(string messageTemplate)
        {
            _delegate.Information(messageTemplate);
        }

        public void Information(string messageTemplate, params object[] propertyValues)
        {
            _delegate.Information(messageTemplate, propertyValues);
        }

        public void Warning(string messageTemplate)
        {
            _delegate.Warning(messageTemplate);
        }

        public void Warning(string messageTemplate, params object[] propertyValues)
        {
            _delegate.Warning(messageTemplate, propertyValues);
        }

        public void Error(string messageTemplate)
        {
            _delegate.Error(messageTemplate);
        }

        public void Error(string messageTemplate, params object[] propertyValues)
        {
            _delegate.Error(messageTemplate, propertyValues);
        }

        public void Error(Exception exception, string messageTemplate)
        {
            _delegate.Error(exception, messageTemplate);
        }

        public void Error(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            _delegate.Error(exception, messageTemplate, propertyValues);
        }

        public void Fatal(string messageTemplate)
        {
            _delegate.Fatal(messageTemplate);
        }

        public void Fatal(string messageTemplate, params object[] propertyValues)
        {
            _delegate.Fatal(messageTemplate, propertyValues);
        }

        public void Fatal(Exception exception, string messageTemplate)
        {
            _delegate.Fatal(exception, messageTemplate);
        }

        public void Fatal(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            _delegate.Fatal(exception, messageTemplate, propertyValues);
        }
    }
}