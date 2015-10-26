﻿using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public interface ILogFilter
    {
        bool ShouldLogError(LogEntry Error);
        bool ShouldLogWarning(LogEntry Warning);
        bool ShouldLogMessage(LogEntry Message);
        bool ShouldLogEvent(LogEntry Status);
    }

    /// <summary>
    /// Defines a type of compiler log that filters 
    /// its messages based on a log filter, and 
    /// manages the exact meaning of warnings, 
    /// as well as the fatality of errors.
    /// </summary>
    public class FilteredLog : ICompilerLog
    {
        public FilteredLog(ILogFilter Filter, ICompilerLog Log, bool LogWarningsAsErrors, int? MaxErrorCount)
        {
            this.Filter = Filter;
            this.Log = Log;
            this.LogWarningsAsErrors = LogWarningsAsErrors;
            this.MaxErrorCount = MaxErrorCount;
            this.errCount = (int)0;
        }
        public FilteredLog(ILogFilter Filter, ICompilerLog Log)
            : this(Filter, Log, ShouldTreatWarningsAsErrors(Log.Options), GetMaxErrorCount(Log.Options))
        { }

        /// <summary>
        /// Gets this filtered log's filter.
        /// </summary>
        public ILogFilter Filter { get; private set; }

        /// <summary>
        /// Gets the underlying compiler log this filtered
        /// log uses.
        /// </summary>
        public ICompilerLog Log { get; private set; }

        /// <summary>
        /// Gets a boolean flag that tells whether warnings
        /// are actually logged as errors.
        /// </summary>
        public bool LogWarningsAsErrors { get; private set; }

        /// Gets the number of error messages to print before the compiler
        /// bails out. Null means that there is no limit to the 
        /// number of errors to print. Otherwise, an integer value
        /// specifies the maximal number of error messages to print.
        public int? MaxErrorCount { get; private set; }

        /// <summary>
        /// Gets the number of error messages that have been printed so far.
        /// </summary>
        public int ErrorCount { get { lock (errCount) { return (int)errCount; } } }

        // The error count is stored as its boxed
        // representation so we can use it 
        // for locking.
        private object errCount;

        public void LogError(LogEntry Entry)
        {
            lock (errCount)
            {
                int newCount = (int)errCount + 1;
                int maxErrCount = MaxErrorCount.GetValueOrDefault();
                if (MaxErrorCount.HasValue && maxErrCount > 0 && newCount > maxErrCount)
                {
                    throw new AbortCompilationException("Maximal error count exceeded. Aborting compilation. [-" + MaxErrorCountName + "=" + maxErrCount + "]");
                }
                if (Filter.ShouldLogError(Entry))
                {
                    Log.LogError(Entry);
                }
                if (MaxErrorCount.HasValue && maxErrCount <= 0)
                {
                    throw new AbortCompilationException("Encountered an error. Aborting compilation. [-" + FatalErrorsName + "]");
                }
                errCount = newCount;
            }
        }

        public void LogEvent(LogEntry Entry)
        {
            if (Filter.ShouldLogEvent(Entry))
            {
                Log.LogEvent(Entry);
            }
        }

        public void LogMessage(LogEntry Entry)
        {
            if (Filter.ShouldLogMessage(Entry))
            {
                Log.LogMessage(Entry);
            }
        }

        public void LogWarning(LogEntry Entry)
        {
            if (LogWarningsAsErrors)
            {
                LogError(Entry);
            }
            else if (Filter.ShouldLogWarning(Entry))
            {
                Log.LogWarning(Entry);
            }
        }

        public ICompilerOptions Options
        {
            get { return Log.Options; }
        }

        public void Dispose()
        {
            Log.Dispose();
        }

        #region Static

        // These options should match GCC's (and Clang's) behavior.
        // GCC docs: https://gcc.gnu.org/onlinedocs/gcc/Warning-Options.html

        /// <summary>
        /// Make all warnings into errors.
        /// </summary>
        public const string TreatWarningsAsErrorsName = "Werror";

        /// <summary>
        /// This option causes the compiler to abort compilation on the first error
        /// occurred rather than trying to keep going and printing further error messages. 
        /// </summary>
        public const string FatalErrorsName = "Wfatal-errors";

        /// <summary>
        /// Limits the maximum number of error messages to n, 
        /// at which point the compiler bails out rather than attempting to continue processing the source code. 
        /// If n is 0 (the default), there is no limit on the number of error messages produced. 
        /// If -Wfatal-errors is also specified, then -Wfatal-errors takes precedence over this option. 
        /// </summary>
        public const string MaxErrorCountName = "fmax-errors";

        /// <summary>
        /// Gets a boolean value that indicates whether
        /// are warnings are to be made into errors.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static bool ShouldTreatWarningsAsErrors(ICompilerOptions Options)
        {
            return Options.GetOption<bool>(TreatWarningsAsErrorsName, false);
        }

        /// <summary>
        /// Gets the number of error messages to print before the compiler
        /// bails out. Null is returned if there is no limit to the 
        /// number of errors to print. Otherwise, an integer value
        /// is returned that specifies the maximal number of error messages to print.
        /// This is dependent on the -Wfatal-errors and -fmax-errors options.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static int? GetMaxErrorCount(ICompilerOptions Options)
        {
            if (Options.GetOption<bool>(FatalErrorsName, false))
            {
                return 0;
            }
            else
            {
                int maxErrorCount = Options.GetOption<int>(MaxErrorCountName, 0);
                if (maxErrorCount == 0)
                {
                    return null;
                }
                else
                {
                    return maxErrorCount;
                }
            }
        }

        #endregion
    }
}
