// include Fake libs
#r "./packages/build/FAKE/tools/FakeLib.dll"
#r "System.IO.Compression.FileSystem"

open System
open System.IO
open Fake
open Fake.NpmHelper
open Fake.Git


let yarn = 
    if EnvironmentHelper.isWindows then "yarn.cmd" else "yarn"
    |> ProcessHelper.tryFindFileOnPath
    |> function
       | Some yarn -> yarn
       | ex -> failwith ( sprintf "yarn not found (%A)\n" ex )

let gitName = "sample-react-todomvc"
let gitOwner = "fable-elmish"
let gitHome = sprintf "https://github.com/%s" gitOwner

// Filesets
let projects  =
      !! "src/**.fsproj"


let dotnetcliVersion = "1.0.1"
let mutable dotnetExePath = "dotnet"

let runDotnet workingDir args =
    printfn "CWD: %s" workingDir
    // printfn "dotnet %s" args
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "Command failed: dotnet %s" args
    

Target "InstallDotNetCore" (fun _ ->
    let dotnetSDKPath = FullName "./dotnetsdk"
    let correctVersionInstalled = 
        try
            let processResult = 
                ExecProcessAndReturnMessages (fun info ->  
                info.FileName <- dotnetExePath
                info.WorkingDirectory <- Environment.CurrentDirectory
                info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

            processResult.Messages |> separated "" = dotnetcliVersion
        with 
        | _ -> false

    if correctVersionInstalled then
        tracefn "dotnetcli %s already installed" dotnetcliVersion
    else
        CleanDir dotnetSDKPath
        let archiveFileName = 
            if isWindows then
                sprintf "dotnet-dev-win-x64.%s.zip" dotnetcliVersion
            elif isLinux then
                sprintf "dotnet-dev-ubuntu-x64.%s.tar.gz" dotnetcliVersion
            else
                sprintf "dotnet-dev-osx-x64.%s.tar.gz" dotnetcliVersion
        let downloadPath = 
                sprintf "https://dotnetcli.azureedge.net/dotnet/Sdk/%s/%s" dotnetcliVersion archiveFileName
        let localPath = Path.Combine(dotnetSDKPath, archiveFileName)

        tracefn "Installing '%s' to '%s'" downloadPath localPath
        
        use webclient = new Net.WebClient()
        webclient.DownloadFile(downloadPath, localPath)

        if not isWindows then
            let assertExitCodeZero x =
                if x = 0 then () else
                failwithf "Command failed with exit code %i" x

            Shell.Exec("tar", sprintf """-xvf "%s" -C "%s" """ localPath dotnetSDKPath)
            |> assertExitCodeZero
        else  
            Compression.ZipFile.ExtractToDirectory(localPath, dotnetSDKPath)
        
        tracefn "dotnet cli path - %s" dotnetSDKPath
        System.IO.Directory.EnumerateFiles dotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s" path)
        System.IO.Directory.EnumerateDirectories dotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s%c" path System.IO.Path.DirectorySeparatorChar)

        dotnetExePath <- dotnetSDKPath </> (if isWindows then "dotnet.exe" else "dotnet")
)


Target "Clean" (fun _ ->
    CleanDir "build"
)

Target "Install" (fun _ ->
    Npm (fun p ->
        { p with
            NpmFilePath = yarn
            Command = Install Standard
        })
    projects
    |> Seq.iter (fun s -> 
        let dir = IO.Path.GetDirectoryName s
        runDotnet dir "restore"
    )
)

Target "Build" (fun _ ->
    projects
    |> Seq.iter (fun s -> 
        let dir = IO.Path.GetDirectoryName s
        runDotnet dir "fable npm-run build")
)

Target "Watch" (fun _ ->
    projects
    |> Seq.iter (fun s -> 
        let dir = IO.Path.GetDirectoryName s
        runDotnet dir "fable npm-run start")
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseSample" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    CopyRecursive "build" tempDocsDir true |> tracefn "%A"

    StageAll tempDocsDir
    Git.Commit.Commit tempDocsDir (sprintf "Update generated sample")
    Branches.push tempDocsDir
)

Target "Publish" DoNothing

// Build order
"Clean"
  ==> "InstallDotNetCore"
  ==> "Install"
  ==> "Build"

"Clean"
  ==> "InstallDotNetCore"
  ==> "Install"
  ==> "Watch"
  
"Publish"
  <== [ "Build"
        "ReleaseSample" ]
  
  
// start build
RunTargetOrDefault "Build"
