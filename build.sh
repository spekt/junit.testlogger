#!/usr/bin/env sh
# vi: set tw=0

dotnet pack &&\
dotnet test test/NUnit.Xml.TestLogger.UnitTests/NUnit.Xml.TestLogger.UnitTests.csproj &&\
dotnet test test/NUnit.Xml.TestLogger.AcceptanceTests/NUnit.Xml.TestLogger.AcceptanceTests.csproj
