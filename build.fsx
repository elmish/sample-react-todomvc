#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.JavaScript.Yarn
nuget Fake.Core.Target
nuget Fake.Tools.Git //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open System
open Fake.JavaScript


let gitName = "sample-react-todo"
let gitOwner = "fable-elmish"
let gitHome = sprintf "https://github.com/%s" gitOwner

// Filesets
let projects  =
      !! "src/**.fsproj"

Target.create "InstallDotNetCore" (fun _ ->
   DotNet.Options.Create() |> DotNet.install id |> ignore
)

Target.create "Clean" (fun _ ->
    Shell.cleanDir ".fable"
    Shell.cleanDir "build"
)

Target.create "Install" (fun _ ->
    Yarn.install id
    projects
    |> Seq.iter (fun s -> 
        let dir = IO.Path.GetDirectoryName s
        DotNet.restore id dir
    )
)

Target.create "Build" (fun _ ->
    Yarn.exec "build" id
)

Target.create "Watch" (fun _ ->
    Yarn.exec "start" id
)

// --------------------------------------------------------------------------------------
// Release Scripts
open Fake.Tools.Git
Target.create "ReleaseSample" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    Shell.cleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    Shell.copyRecursive "build" tempDocsDir true |> ignore

    Staging.stageAll tempDocsDir
    Commit.exec tempDocsDir (sprintf "Update generated sample")
    Branches.push tempDocsDir
)

Target.create "Publish" ignore

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
Target.runOrDefault "Build"
