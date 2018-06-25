using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Formats the message for an upload commit.
/// </summary>
public class MessageFormatter
{
    private readonly Config config;

    public MessageFormatter(Config config)
    {
        this.config = config;
    }

    /// <summary>
    /// Formats the configured message template by replacing all placeholders with actual values.
    /// </summary>
    /// <param name="assemblyVersion">The version read from the version assembly</param>
    /// <returns></returns>
    public string Format(string assemblyVersion)
    {
        string formattedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return config.Message.Replace("%v", assemblyVersion).Replace("%p", config.Partition).Replace("%t", formattedTime);
    }
}

