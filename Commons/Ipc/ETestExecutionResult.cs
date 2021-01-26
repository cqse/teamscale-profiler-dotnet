using System;
using System.Collections.Generic;
using System.Text;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public enum ETestExecutionResult
    {
        /// <summary>
        /// Test execution was successful.
        /// </summary>
        PASSED,

        /// <summary>
        /// The test is currently marked as "do not execute" (e.g. JUnit @Ignore).
        /// </summary>
        IGNORED,

        /// <summary>
        /// Caused by a failing assumption.
        /// </summary>
        SKIPPED,

        /// <summary>
        ///  Caused by a failing assertion.
        /// </summary>
        FAILURE,

        /// <summary>
        /// Caused by an error during test execution (e.g. exception thrown).
        /// </summary>
        ERROR
    }
}
