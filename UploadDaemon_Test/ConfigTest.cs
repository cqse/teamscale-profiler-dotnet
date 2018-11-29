using Common;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class ConfigTest
{
    [Test]
    public void ValidTeamscaleJson()
    {
        IEnumerable<string> errors = ParseConfig(@"{
            /* line comment */
            versionAssembly: ""Assembly"",
            teamscale: {
                url: ""url"",
                username: ""user"",
                accessKey: ""token"",
                project: ""project"",
                partition: ""partition"",
            },
        }");

        Assert.That(errors, Is.Empty, "valid configuration must not raise any errors");
    }

    [Test]
	public void AzureFileStorageUploadConfigurationIsValid()
	{
		IFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
		{
			{
				Config.ConfigFilePath, new MockFileData(@"{
                    /* line comment */
                    versionAssembly: ""Assembly"",
                    azureFileStorage: {
                        connectionString: ""<connection-string>"",
                        shareName: ""<share>"",
                        directory: ""<dir>""
                    },
                }")
			}
		});

		IEnumerable<string> errors = Config.ReadConfig(fileSystem).Validate();
		Assert.That(errors, Is.Empty, "a configuration with an Azure File Storage for upload should be valid");
	}

	[Test]
    public void ValidDirectoryJson()
    {
        IEnumerable<string> errors = ParseConfig(@"{
            /* line comment */
            versionAssembly: ""Assembly"",
            directory: "".""
        }");

        Assert.That(errors, Is.Empty, "valid configuration must not raise any errors");
    }

    [Test]
    public void MissingAttribute()
    {
        IEnumerable<string> errors = ParseConfig(@"{}");
        Assert.That(errors, Is.Not.Empty, "Empty configuration should cause errors");
    }

    private IEnumerable<string> ParseConfig(string configJson)
    {
        UploadConfig config = JsonConvert.DeserializeObject<UploadConfig>(configJson);
        return config.Validate();
    }
}
