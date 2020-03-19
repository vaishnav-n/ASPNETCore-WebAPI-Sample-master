#tool "nuget:?package=OctopusTools&Version=6.7.0"
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
var buildoutputpath= "D:/Output_build/" ;
var octopkgpath= "D:/OctoPackages/";
var packageId = "app_2";
var semVer = CreateSemVer(1,0,0);
var sourcepath= "D:/Application_2-master/Application_2-master/Application_2.sln";
var octopusApiKey=EnvironmentVariable("API-OXYIK7IMOBLB12HRG66CLEI24");

var octopusServerUrl=EnvironmentVariable("http://localhost:81");


Task("Build")
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
			Version = semVer.ToString()
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
    
    var physicalFilePath = System.IO.Path.Combine( Directory(octopkgpath), $"{packageId}.{semVer}.nupkg");
    OctoPush(octopusServerUrl, 
        octopusApiKey, 
        octopkgpath, 
        octoPushSettings);
	});

Task("OctoCreateRelease")
	.IsDependentOn("OctoPush")
	.Does(()=>
	{
		var createReleaseSettings = new CreateReleaseSettings
		{
			Server = octopusServerUrl,
			ApiKey = octopusApiKey,
			DeploymentProgress = true,
			Packages = new Dictionary<string, string>
			{
				{packageId, semVer.ToString()}
			}
        };

    OctoCreateRelease("app_2", createReleaseSettings);
	});

Task("Default")  
    .IsDependentOn("OctoCreateRelease"); 

RunTarget(target);