using System;

namespace UploadDaemon.Archiving
{
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
