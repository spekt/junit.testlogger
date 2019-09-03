#!/usr/bin/env sh
# vi: set tw=0

dotnet pack &&\
dotnet test test/JUnit.Xml.TestLogger.UnitTests/JUnit.Xml.TestLogger.UnitTests.csproj &&\
dotnet test test/JUnit.Xml.TestLogger.AcceptanceTests/JUnit.Xml.TestLogger.AcceptanceTests.csproj
