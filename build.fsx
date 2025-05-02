#!/usr/bin/env -S dotnet fsi
#r "nuget: Fake.IO.FileSystem, 5.23.1"
#r "nuget: Fake.DotNet.Cli, 5.23.1"
#r "nuget: Fake.JavaScript.Yarn, 5.23.1"
#r "nuget: Fake.Core.Target, 5.23.1"
#r "nuget: Fake.Tools.Git, 5.23.1"

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

System.Environment.GetCommandLineArgs() 
|> Array.skip 2 // fsi.exe; build.fsx
|> Array.toList
|> Context.FakeExecutionContext.Create false __SOURCE_FILE__
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

Target.create "Clean" (fun _ ->
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
    DotNet.exec id "fable" "src -o src/out --run webpack" |> ignore
)

Target.create "Watch" (fun _ ->
    DotNet.exec id "fable" "watch src -o src/out -s --run webpack serve" |> ignore
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
  ==> "Install"
  ==> "Build"

"Clean"
  ==> "Install"
  ==> "Watch"
  
"Publish"
  <== [ "Build"
        "ReleaseSample" ]
  
  
// start build
Target.runOrDefault "Build"
