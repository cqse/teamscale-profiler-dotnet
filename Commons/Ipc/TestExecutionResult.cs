namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public enum TestExecutionResult
    {
        /// <summary>
        /// Test execution was successful.
        /// </summary>
        Passed,

        /// <summary>
        /// The test is currently marked as "do not execute" (e.g. JUnit @Ignore).
        /// </summary>
        Ignored,

        /// <summary>
        /// Caused by a failing assumption.
        /// </summary>
        Skipped,

        /// <summary>
        ///  Caused by a failing assertion.
        /// </summary>
        Failure,

        /// <summary>
        /// Caused by an error during test execution (e.g. exception thrown).
        /// </summary>
        Error
    }
}
