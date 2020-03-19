#tool "nuget:?package=OctopusTools&Version=6.7.0"
#tool "nuget:?package=GitVersion.CommandLine&Version=4.0.0"
#addin "nuget:?package=Cake.ArgumentHelpers"
#addin "Cake.Npm"&version=0.8.0
#addin nuget:?package=Cake.SemVer
#addin nuget:?package=semver&version=2.0.4
#module "nuget:?package=Cake.BuildSystems.Module&version=0.3.2"

using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;

var target= Argument("Argument","Default");
var BuildNumber = ArgumentOrEnvironmentVariable("build.number", "", "0.0.1-local.0");
var buildoutputpath= "D:/Output_build/" ;
var octopkgpath= "D:/OctoPackages/";
var packageId = "api_1";
var sourcepath="SampleWebApiAspNetCore.sln";
var octopusApiKey=ArgumentOrEnvironmentVariable("OctopusDeployApiKey","");
string BranchName = null;

var octopusServerUrl="http://localhost:83";

Task("Restore")
    .Does(() =>
	     {
			NuGetRestore("SampleWebApiAspNetCore.sln");
	     });

Task("Build")
	.IsDependentOn("Restore")
	.IsDependentOn("Version")
    .Does(() => 
    {
        MSBuild(sourcepath, new MSBuildSettings()
              .WithProperty("OutDir", buildoutputpath)
                );

    });

Task("OctoPack")
	.IsDependentOn("Build")
	.Does(()=>
	{    
		var octoPackSettings = new OctopusPackSettings()
		{
			BasePath = buildoutputpath,
			OutFolder = octopkgpath,
			Overwrite = true,
			Version = BuildNumber
		};    

    OctoPack(packageId,octoPackSettings);
	});

Task("OctoPush")
	.IsDependentOn("OctoPack")
	.Does(()=>
	{	
       var octoPushSettings = new OctopusPushSettings()
    {        
        ReplaceExisting =true
    };
    
    OctoPush(octopusServerUrl, 
        octopusApiKey, 
        GetFiles("D:/OctoPackages/*.*"), 
        octoPushSettings);
	});

Task("Version")
  .Does(() =>
{
	GitVersionSettings buildServerSettings = new GitVersionSettings {
		OutputType = GitVersionOutput.BuildServer,
        UpdateAssemblyInfo = true
    };

	GitVersion(buildServerSettings);

	GitVersionSettings localSettings = new GitVersionSettings();

	var versionResult = GitVersion(localSettings);

	BuildNumber = versionResult.SemVer;
	BranchName = versionResult.BranchName;
});


Task("Default")  
    .IsDependentOn("OctoPush"); 

RunTarget(target);