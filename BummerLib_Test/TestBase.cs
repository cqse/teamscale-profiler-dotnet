﻿//------------------------------------------------------------------------------
// <copyright company="CQSE GmbH">
//   Copyright (c) CQSE GmbH
// </copyright>
// <license>
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </license>
//------------------------------------------------------------------------------

using NUnit.Framework;
using System.IO;


namespace Cqse.ConQAT.Dotnet.Bummer
{
    public class TestBase
    {
        protected FileInfo GetGlobalTestFile(string resource) {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "../..", "TestData", resource);
            return new FileInfo(path);
        }
    }
}
