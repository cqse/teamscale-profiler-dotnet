using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IUpload
{
    Task<bool> UploadAsync(string filePath, string version, string message, string partition);
}
