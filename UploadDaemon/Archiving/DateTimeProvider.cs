using System;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// DateTime provider supplying DateTime.Now.
    /// </summary>
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        /// <inheritDoc/>
        public DateTime Now => DateTime.Now;
    }
}
