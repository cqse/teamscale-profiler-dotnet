using System;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// A provider that supplies the current date time.
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Current date time.
        /// </summary>
        DateTime Now { get; }
    }

    /// <summary>
    /// DateTime provider supplying DateTime.Now.
    /// </summary>
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        /// <summary>
        /// Current date time.
        /// </summary>
        public DateTime Now => DateTime.Now;
    }
}
