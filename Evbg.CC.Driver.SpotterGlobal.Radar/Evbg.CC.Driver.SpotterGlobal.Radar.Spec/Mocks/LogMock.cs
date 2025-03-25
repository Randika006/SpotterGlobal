using log4net;
using log4net.Core;
using System;
using System.Threading;
using static System.Diagnostics.Debug;

namespace Evbg.CC.Driver.SpotterGlobal.Radar.Spec
{
    public class LogMock : ILog
    {
        private string Prefix => $"{DateTime.Now} [{Thread.CurrentThread.ManagedThreadId}]: ";

        public ILogger Logger { get; }

        public void Debug(object message)
        {
            WriteLine(Prefix + message);
        }

        public void Debug(object message, Exception exception)
        {
            WriteLine($"{Prefix}{message}, Exception: {exception.Message}");
        }

        public void DebugFormat(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        public void DebugFormat(string format, object arg0)
        {
            WriteLine(format, arg0);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            WriteLine(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            WriteLine(format, arg0, arg1, arg2);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            //not implemented yet
        }

        public void Info(object message)
        {
            WriteLine(Prefix + message);
        }

        public void Info(object message, Exception exception)
        {
            WriteLine($"{Prefix}{message}, Exception: {exception.Message}");
        }

        public void InfoFormat(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        public void InfoFormat(string format, object arg0)
        {
            WriteLine(format, arg0);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            WriteLine(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            //not implemented yet
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            //not implemented yet
        }

        public void Warn(object message)
        {
            WriteLine(Prefix + message);
        }

        public void Warn(object message, Exception exception)
        {
            WriteLine($"{Prefix}{message}, Exception: {exception.Message}");
        }

        public void WarnFormat(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        public void WarnFormat(string format, object arg0)
        {
            //not implemented yet
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            //not implemented yet
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            //not implemented yet
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            //not implemented yet
        }

        public void Error(object message)
        {
            WriteLine(Prefix + message);
        }

        public void Error(object message, Exception exception)
        {
            WriteLine($"{Prefix}{message}, Exception: {exception.Message}");
        }

        public void ErrorFormat(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        public void ErrorFormat(string format, object arg0)
        {
            //not implemented yet
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            //not implemented yet
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            //not implemented yet
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            //not implemented yet
        }

        public void Fatal(object message)
        {
            WriteLine(Prefix + message);
        }

        public void Fatal(object message, Exception exception)
        {
            WriteLine($"{Prefix}{message}, Exception: {exception.Message}");
        }

        public void FatalFormat(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        public void FatalFormat(string format, object arg0)
        {
            //not implemented yet
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            //not implemented yet
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            //not implemented yet
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            //not implemented yet
        }

        public bool IsDebugEnabled => true;
        public bool IsInfoEnabled => true;
        public bool IsWarnEnabled => true;
        public bool IsErrorEnabled => true;
        public bool IsFatalEnabled => true;
    }
}