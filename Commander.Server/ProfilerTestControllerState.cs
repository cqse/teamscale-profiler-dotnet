namespace Cqse.Teamscale.Profiler.Commander.Server
{
    /// <summary>
    /// As the controller is stateless, we store infos about the current test run in this state class.
    /// </summary>
    public class ProfilerTestControllerState
    {
        /// <summary>
        /// Start timestamp of the last test case.
        /// </summary>
        public long TestStartMS { get; set; } = 0;
    }
}
