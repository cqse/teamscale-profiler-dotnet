﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace UploadDaemon.Report.Testwise
{
    /// <summary>
    /// The coverage below a certain path in a <see cref="TestwiseCoverageReport"/>.
    /// </summary>
    public class CoverageForPath
    {
        public CoverageForPath(params File[] files)
        {
            Files = files.ToList();
        }

        [JsonProperty(PropertyName = "path")]
        public readonly string Path = "";

        [JsonProperty(PropertyName = "files")]
        public IList<File> Files { get; set; }
    }
}
