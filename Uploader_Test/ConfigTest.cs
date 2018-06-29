using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ConfigTest
{
    [TestMethod]
    public void ValidJson()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            {
                Config.ConfigFilePath, new MockFileData(@"{
                    /* line comment */
                    versionAssembly: ""Assembly"",
                    teamscale: {
                        url: ""url"",
                        username: ""user"",
                        accessToken: ""token"",
                        project: ""project"",
                        partition: ""partition"",
                    },
                }")
            }
        });

        // must not throw an exception
        Config.ReadConfig(fileSystem);
    }

    [TestMethod]
    public void MissingAttribute()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            {
                Config.ConfigFilePath, new MockFileData(@"{
                }")
            }
        });
        
        try
        {
            Config.ReadConfig(fileSystem);
        }
        catch
        {
            return;
        }
        Assert.Fail("Did not throw an exception");
    }
}

