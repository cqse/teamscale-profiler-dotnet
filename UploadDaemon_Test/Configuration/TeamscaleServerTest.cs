using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UploadDaemon.Configuration
{
    [TestFixture]
    public class TeamscaleServerTest
    {
        private const string YamlUser = "yaml-user";
        private const string YamlAccessKey = "yaml-access-key";
        private const string EnvironmentUser = "environment-user";
        private const string EnvironmentAccessKey = "environment-access-key";

        private string originalUsername;
        private string originalAccessKey;

        /// <summary>
        /// The environment variables are process-global, so we must remember whatever the
        /// developer/build machine has set and start from a clean slate in every test.
        /// </summary>
        [SetUp]
        public void ClearEnvironmentVariables()
        {
            originalUsername = Environment.GetEnvironmentVariable(TeamscaleServer.UsernameEnvironmentVariable);
            originalAccessKey = Environment.GetEnvironmentVariable(TeamscaleServer.AccessKeyEnvironmentVariable);
            SetEnvironmentVariables(null, null);
        }

        [TearDown]
        public void RestoreEnvironmentVariables()
        {
            SetEnvironmentVariables(originalUsername, originalAccessKey);
        }

        [Test]
        public void EnvironmentVariablesProvideTheCredentialsIfTheYamlDoesNotContainThem()
        {
            SetEnvironmentVariables(EnvironmentUser, EnvironmentAccessKey);

            TeamscaleServer server = ServerWithoutCredentials();
            Assert.Multiple(() =>
            {
                Assert.That(server.Username, Is.EqualTo(EnvironmentUser), "username");
                Assert.That(server.AccessKey, Is.EqualTo(EnvironmentAccessKey), "access key");
            });

            IEnumerable<string> errors = server.Validate();
            Assert.That(errors, Is.Empty, "credentials from the environment must not raise any errors");
        }

        [Test]
        public void EnvironmentVariablesTakePrecedenceOverTheYamlProperties()
        {
            SetEnvironmentVariables(EnvironmentUser, EnvironmentAccessKey);

            TeamscaleServer server = ServerWithCredentials();

            Assert.Multiple(() =>
            {
                Assert.That(server.Username, Is.EqualTo(EnvironmentUser), "username");
                Assert.That(server.AccessKey, Is.EqualTo(EnvironmentAccessKey), "access key");
            });
        }

        [Test]
        public void BlankEnvironmentVariablesAreTreatedAsUnset()
        {
            SetEnvironmentVariables("", "   ");

            TeamscaleServer server = ServerWithCredentials();

            Assert.Multiple(() =>
            {
                Assert.That(server.Username, Is.EqualTo(YamlUser), "username");
                Assert.That(server.AccessKey, Is.EqualTo(YamlAccessKey), "access key");
            });
        }

        [Test]
        public void YamlWithoutCredentialsIsInvalidIfTheEnvironmentVariablesAreNotSet()
        {
            Exception exception = Assert.Throws<Config.InvalidConfigException>(() => Config.Read(@"
                match:
                  - profiler:
                      targetdir: C:\test
                    uploader:
                      pdbDirectory: C:\pdbs
                      revisionFile: C:\revision
                      teamscale:
                        url: url
                        project: project
                        partition: partition
            ").CreateConfigForProcess("foo.exe"));

            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Does.Contain(TeamscaleServer.UsernameEnvironmentVariable), "username error hints at the environment variable");
                Assert.That(exception.Message, Does.Contain(TeamscaleServer.AccessKeyEnvironmentVariable), "access key error hints at the environment variable");
            });
        }

        private static void SetEnvironmentVariables(string username, string accessKey)
        {
            Environment.SetEnvironmentVariable(TeamscaleServer.UsernameEnvironmentVariable, username);
            Environment.SetEnvironmentVariable(TeamscaleServer.AccessKeyEnvironmentVariable, accessKey);
        }

        /// <summary>
        /// A server as it results from a YAML config that provides the credentials itself.
        /// </summary>
        private static TeamscaleServer ServerWithCredentials()
        {
            TeamscaleServer server = ServerWithoutCredentials();
            server.Username = YamlUser;
            server.AccessKey = YamlAccessKey;
            return server;
        }

        /// <summary>
        /// A server as it results from a YAML config that omits the credentials.
        /// </summary>
        private static TeamscaleServer ServerWithoutCredentials()
        {
            return new TeamscaleServer
            {
                Url = "url",
                Project = "project",
                Partition = "partition",
            };
        }
    }
}
