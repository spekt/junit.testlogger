# JUnit Test Logger
JUnit xml report extension for [Visual Studio Test Platform](https://github.com/microsoft/vstest).

**TODO** Add travis/appveyor
<!-- [![Build Status](https://travis-ci.com/spekt/junit.testlogger.svg?branch=master)](https://travis-ci.com/spekt/junit.testlogger)
[![Build status](https://ci.appveyor.com/api/projects/status/2masybxty5kve2dc?svg=true)](https://ci.appveyor.com/project/spekt/junit-testlogger) -->

## Packages
<!-- | Logger | Stable Package | Pre-release Package |
| ------ | -------------- | ------------------- |
| JUnit | [![NuGet](https://img.shields.io/nuget/v/JUnitXml.TestLogger.svg)](https://www.nuget.org/packages/JUnitXml.TestLogger/) | [![MyGet Pre Release](https://img.shields.io/myget/spekt/vpre/junitxml.testlogger.svg)](https://www.myget.org/feed/spekt/package/nuget/JunitXml.TestLogger) | -->

If you're looking for `Junit`, `xunit` or `appveyor` loggers, visit following repositories:
* <https://github.com/spekt/nunit.testlogger>
* <https://github.com/spekt/xunit.testlogger>
* <https://github.com/spekt/appveyor.testlogger>

## Usage
JUnit logger can generate xml reports in the JUnit format. The JUnit format seems to be variable across different implementations, with the JUnit 5 repository referring to the [Ant Junit Format](https://github.com/windyroad/JUnit-Schema) as a de-facto standard. This project does not implement that standard exactly. Please [refer to a sample file](docs/assets/TestResults.xml) to see an example of which parts of the format have been implemented. If you find that this format is missing some featur required by your CI/CD system, please open an issue.

### Default Behavior

1. Add a reference to the [JUnit Logger](https://www.nuget.org/packages/JUnitXml.TestLogger) nuget package in test project
2. Use the following command line in tests
```
> dotnet test --test-adapter-path:. --logger:junit
```
3. Test results are generated in the `TestResults` directory relative to the `test.csproj`

### Customizing Output

There are several options to customize the output file name, location, and contents. 

Platform Specific Recommendations:
* [GitLab CI/CD Recomendation](/docs/gitlab-recommendation.md)




A path for the report file can be specified as follows:
```
> dotnet test --test-adapter-path:. --logger:"junit;LogFilePath=test-result.xml"
```

`test-result.xml` will be generated in the same directory as `test.csproj`.

**Note:** the arguments to `--logger` should be in quotes since `;` is treated as a command delimiter in shell.

## License
MIT
