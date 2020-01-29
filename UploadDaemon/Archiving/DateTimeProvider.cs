using System;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// DateTime provider supplying DateTime.Now.
    /// </summary>
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        /// <inheritdoc/>
        public DateTime Now => DateTime.Now;
    }
}
