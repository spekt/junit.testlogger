dotnet pack
if ($?) {
    dotnet test test/NUnit.Xml.TestLogger.UnitTests/NUnit.Xml.TestLogger.UnitTests.csproj
}
if ($?) {
    dotnet test test/NUnit.Xml.TestLogger.AcceptanceTests/NUnit.Xml.TestLogger.AcceptanceTests.csproj
}
