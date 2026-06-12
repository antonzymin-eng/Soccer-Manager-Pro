// File:     tools/dotnet-ci/UnityShim.TestTools/LogAssertVerifyAttribute.cs
// Created:  2026-06-12
// Purpose:  Assembly-level NUnit action attribute that gives every test the
//           Unity Test Framework log contract: LogAssert expectations are reset
//           before each test and verified after it (unmet expectation or
//           unexpected Error/Assert/Exception log fails the test). Injected
//           into each generated test project via LogAssertVerifyAssemblyInfo.cs
//           (see tools/dotnet-ci/generate_projects.py).

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class LogAssertVerifyAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test) => LogAssert.Reset();

        public void AfterTest(ITest test)
        {
            // If the test already failed, don't stack a secondary log-contract
            // failure on top of it — keep the original failure as the signal.
            var result = TestExecutionContext.CurrentContext?.CurrentResult;
            if (result != null && result.ResultState.Status == TestStatus.Failed)
            {
                LogAssertSilentReset();
                return;
            }
            LogAssert.VerifyAtTestEnd();
        }

        private static void LogAssertSilentReset() => LogAssert.Reset();
    }
}
