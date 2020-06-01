# BUILD - Table of Contents

<!-- TOC -->
- [Prerequisites](#prerequisites)
- [How to Build](#build)

<!-- /TOC -->

# Prerequisites

Building the GenICamXmlValidator plugin from the source, requires having the following SW installed:

	* Visual Studio 2017 with C++ and C# inslalled. It "may" work with Visual Studio 2015, but it was never been tried.
	* Visual Studio 2015 Toolset (v140)
	* .NET Framework 4.6
	* Visual Studio SDK: https://docs.microsoft.com/en-us/visualstudio/extensibility/visual-studio-sdk
	* GenICam 3.1.0, built for VS2015
	* 7-zip acrhiver: https://www.7-zip.org

# How to Build
	* Get the code from github 
	* Open the GenICamXmlValidator.sln in Visual Studio 2017
	* Select Tools- >NuGet Package Manager-> Manage NuGet Packages for the Solution...
	* Message "Some NuGet packages are missing from the solution..." will appear, press "Restore" to get the packages
