using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.MyPlugin.Tests
{
    [ZoneDefinition]
    public class MyPluginTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IMyPluginZone> { }

    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<MyPluginTestEnvironmentZone> { }

    [SetUpFixture]
    public class MyPluginTestsAssembly : ExtensionTestEnvironmentAssembly<MyPluginTestEnvironmentZone> { }
}
