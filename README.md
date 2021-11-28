This is a port of [TodoMVC in Elm](https://github.com/evancz/elm-todomvc) implemented in F# and targeting Fable and React.
========
The app is live at https://elmish.github.io/sample-react-todomvc.

## Building and running the sample
Pre-requisites:
* .NET Core [SDK 5.*](https://docs.microsoft.com/en-us/dotnet/core/install/sdk)
* `yarn` installed as a global `npm` or a platform package and available in the path 

To build locally and start the webpack-devserver:
* once: `dotnet tool restore`
* `dotnet fake build -t Watch`

open [localhost:8090](http://localhost:8090)
