#addin "nuget:?package=Cake.ArgumentHelpers"
#tool "nuget:?package=OctopusTools&Version=6.7.0"
#addin "Cake.Npm"&version=0.8.0
#addin nuget:?package=Cake.SemVer
#addin nuget:?package=semver&version=2.0.4
#module "nuget:?package=Cake.Systems.Module&version=0.3.2"

using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;

var target= Argument("Argument","OctoPush");
var buildoutputpath= "D:/Output_build/" ;
var octopkgpath= "D:/OctoPackages/";
var packageId = "api_1";
var semVer = CreateSemVer(1,0,0);
var sourcepath= "SampleWebApiAspNetCore.sln";
var BuildNumber = ArgumentOrEnvironmentVariable("build.number", "", "0.0.1-local.0");
var octopusApiKey=ArgumentOrEnvironmentVariable("OctopusDeployApiKey","");



var octopusServerUrl="http://localhost:83";

Task("Restore")
    .Does(()=>
    {
      NuGetRestore("SampleWebApiAspNetCore.sln");
      DotNetCoreRestore("SampleWebApiAspNetCore.sln");
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
			Version = semVer.ToString()
		};    

    OctoPack(packageId,octoPackSettings);
	});
	
Task("Version")
  .Does(() =>
{
	GitVersionSettings buildServerSettings = new GitVersionSettings {
		OutputType = GitVersionOutput.BuildServer,
        UpdateAssemblyInfo = true
    };

	SetGitVersionPath(buildServerSettings);

	// Ran twice because the result is empty when using buildserver mode but we need to output to TeamCity
	// and use the result
	GitVersion(buildServerSettings);

	GitVersionSettings localSettings = new GitVersionSettings();

	SetGitVersionPath(localSettings);

	var versionResult = GitVersion(localSettings);


	BuildNumber = versionResult.SemVer;

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

Task("OctoCreateRelease")
	.IsDependentOn("OctoPush")
	.Does(()=>
	{
		var createReleaseSettings = new CreateReleaseSettings
		{
			Server = octopusServerUrl,
			ApiKey = octopusApiKey,
			DeploymentProgress = true,
			Channel = "Develop",
			ReleaseNumber = semVer.ToString(),
			Packages = new Dictionary<string, string>
			{
				{packageId, semVer.ToString()}
			}
       		 };
	
    OctoCreateRelease("app_2",createReleaseSettings);
  
	});

Task("OctoDeploy")
	.IsDependentOn("OctoCreateRelease")
	.Does(()=>
	{
  	  var octoDeploySettings = new OctopusDeployReleaseDeploymentSettings
    	{
        	ShowProgress = true,
        	WaitForDeployment= true
    	};   

    OctoDeployRelease(
        octopusServerUrl,
        octopusApiKey, 
        "app_2", 
        releaseEnvironment, 
        semVer.ToString(),
        octoDeploySettings);
	});

public void SetGitVersionPath(GitVersionSettings settings)
{
	if (TeamCity.IsRunningOnTeamCity)
	{
		Information("Using shared GitVersion");

		settings.ToolPath = "c:\\C:\\users\\vaishnavn\\.nuget\\packages\\gitversion\\3.6.5\\gitversion.3.6.5.nupkg";
	}
}

RunTarget(target);
