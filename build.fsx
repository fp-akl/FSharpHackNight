#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let awsLambdaName = "FpHackNight"

let currentDir = __SOURCE_DIRECTORY__

let release_3_1 (option: DotNet.CliInstallOptions) =
    { option with
        InstallerOptions = (fun io ->
            { io with
                Branch = "release/3.1"
            })
        Channel = None
        Version = DotNet.CliVersion.Version "3.1"
    }

// Lazily install DotNet SDK in the correct version if not available
let install = lazy DotNet.install release_3_1

// Set general properties without arguments
let inline dotnetSimple arg = DotNet.Options.lift install.Value arg

let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

Target.initEnvironment ()

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)
Target.create "Test" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.test id)
)

Target.create "Deploy" (fun _ -> 
  DotNet.exec 
    (Path.combine currentDir "src" |> withWorkDir) 
    (sprintf "lambda deploy-function %s" awsLambdaName) 
    ""
  |> ignore
  )

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "Test"
  //==> "Deploy"
  ==> "All"

Target.runOrDefault "All"
