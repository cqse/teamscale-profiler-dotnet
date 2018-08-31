using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class ConfigTest
{
    [Test]
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

        IEnumerable<string> errors = Config.ReadConfig(fileSystem).Validate();
        Assert.That(errors, Is.Empty, "valid configuration must not raise any errors");
    }

    [Test]
    public void MissingAttribute()
    {
        IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            {
                Config.ConfigFilePath, new MockFileData(@"{}")
            }
        });

        IEnumerable<string> errors = Config.ReadConfig(fileSystem).Validate();
        Assert.That(errors, Is.Not.Empty, "Empty configuration should cause errors");
    }
}