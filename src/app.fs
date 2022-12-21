(**
 - title: Todo MVC
 - tagline: The famous todo mvc ported from elm-todomvc
*)

module App

open Fable.Core
open Fable.Import
open Elmish

let [<Literal>] ENTER_KEY = "Enter"
let [<Literal>] ALL_TODOS = "all"
let [<Literal>] ACTIVE_TODOS = "active"
let [<Literal>] COMPLETED_TODOS = "completed"


// MODEL
type Entry =
    { description : string
      completed : bool
      editing : bool
      id : int }

// The full application state of our todo app.
type Model = 
    { entries : Entry list
      field : string
      uid : int
      visibility : string }

let emptyModel =
    { entries = []
      visibility = ALL_TODOS
      field = ""
      uid = 0 }

let newEntry desc id =
  { description = desc
    completed = false
    editing = false
    id = id }


let init = function
  | Some savedModel -> savedModel, []
  | _ -> emptyModel, []


// UPDATE


(** Users of our app can trigger messages by clicking and typing. These
messages are fed into the `update` function as they occur, letting us react
to them.
*)
type Msg =
    | Failure of string
    | UpdateField of string
    | EditingEntry of int*bool
    | UpdateEntry of int*string
    | Add
    | Delete of int
    | DeleteComplete
    | Check of int*bool
    | CheckAll of bool
    | ChangeVisibility of string



// How we update our Model on a given Msg?
let update (msg:Msg) (model:Model) : Model*Cmd<Msg>=
    match msg with
    | Failure err ->
        Fable.Core.JS.console.error(err)
        model, []

    | Add ->
        let xs = if System.String.IsNullOrEmpty model.field then
                    model.entries
                 else
                    model.entries @ [newEntry model.field model.uid]
        { model with
            uid = model.uid + 1
            field = ""
            entries = xs },
        []

    | UpdateField str ->
      { model with field = str }, []

    | EditingEntry (id,isEditing) ->
        let updateEntry t =
          if t.id = id then { t with editing = isEditing } else t
        { model with entries = List.map updateEntry model.entries }, []

    | UpdateEntry (id,task) ->
        let updateEntry t =
          if t.id = id then { t with description = task } else t
        { model with entries = List.map updateEntry model.entries }, []

    | Delete id ->
        { model with entries = List.filter (fun t -> t.id <> id) model.entries }, []

    | DeleteComplete ->
        { model with entries = List.filter (fun t -> not t.completed) model.entries }, []

    | Check (id,isCompleted) ->
        let updateEntry t =
          if t.id = id then { t with completed = isCompleted } else t
        { model with entries = List.map updateEntry model.entries }, []

    | CheckAll isCompleted ->
        let updateEntry t = { t with completed = isCompleted }
        { model with entries = List.map updateEntry model.entries }, []

    | ChangeVisibility visibility ->
        { model with visibility = visibility },
        []

// Local storage interface
module S =
    let private STORAGE_KEY = "elmish-react-todomvc"
    let private decoder = Thoth.Json.Decode.Auto.generateDecoder<Model>()
    let load (): Model option =
        Browser.WebStorage.localStorage.getItem(STORAGE_KEY)
        |> unbox
        |> Core.Option.bind (Thoth.Json.Decode.fromString decoder >> function | Ok r -> Some r | _ -> None)

    let save (model: Model) =
        Browser.WebStorage.localStorage.setItem(STORAGE_KEY, Thoth.Json.Encode.Auto.toString(1,model))


let setStorage (model:Model) : Cmd<Msg> =
    Cmd.OfFunc.attempt S.save model (string >> Failure)

let updateWithStorage (msg:Msg) (model:Model) =
  match msg with
  // If the Msg is Failure we know the model hasn't changed
  | Failure _ -> model, []
  | _ ->
    let (newModel, cmds) = update msg model
    newModel, Cmd.batch [ setStorage newModel; cmds ]

// rendering views with React
open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Elmish.React

let internal onEnter msg dispatch =
    function
    | (ev:Browser.Types.KeyboardEvent) when ev.key = ENTER_KEY ->
        ev.target?value <- ""
        dispatch msg
    | _ -> ()
    |> OnKeyDown

let viewInput (model:string) dispatch =
    header [ ClassName "header" ] [
        h1 [] [ str "todos" ]
        input [
            ClassName "new-todo"
            Placeholder "What needs to be done?"
            valueOrDefault model
            onEnter Add dispatch
            OnChange (fun ev -> !!ev.target?value |> UpdateField |> dispatch)
            AutoFocus true
        ]
    ]

let internal classList classes =
    classes
    |> List.fold (fun complete -> function | (name,true) -> complete + " " + name | _ -> complete) ""
    |> ClassName

let viewEntry todo dispatch =
  li
    [ classList [ ("completed", todo.completed); ("editing", todo.editing) ]]
    [ div
        [ ClassName "view" ]
        [ input
            [ ClassName "toggle"
              Type "checkbox"
              Checked todo.completed
              OnChange (fun _ -> Check (todo.id,(not todo.completed)) |> dispatch) ]
          label
            [ OnDoubleClick (fun _ -> EditingEntry (todo.id,true) |> dispatch) ]
            [ str todo.description ]
          button
            [ ClassName "destroy"
              OnClick (fun _-> Delete todo.id |> dispatch) ]
            []
        ]
      input
        [ ClassName "edit"
          valueOrDefault todo.description
          Name "title"
          Id ("todo-" + (string todo.id))
          OnInput (fun ev -> UpdateEntry (todo.id, !!ev.target?value) |> dispatch)
          OnBlur (fun _ -> EditingEntry (todo.id,false) |> dispatch)
          onEnter (EditingEntry (todo.id,false)) dispatch ]
    ]

let viewEntries visibility entries dispatch =
    let isVisible todo =
        match visibility with
        | COMPLETED_TODOS -> todo.completed
        | ACTIVE_TODOS -> not todo.completed
        | _ -> true

    let allCompleted =
        List.forall (fun t -> t.completed) entries

    let cssVisibility =
        if List.isEmpty entries then "hidden" else "visible"

    section
      [ ClassName "main"
        Style [ Visibility cssVisibility ]]
      [ input
          [ ClassName "toggle-all"
            Type "checkbox"
            Name "toggle"
            Checked allCompleted
            OnChange (fun _ -> CheckAll (not allCompleted) |> dispatch)]
        label
          [ HtmlFor "toggle-all" ]
          [ str "Mark all as complete" ]
        ul
          [ ClassName "todo-list" ]
          (entries
           |> List.filter isVisible
           |> List.map (fun i -> lazyView2 viewEntry i dispatch)) ]

// VIEW CONTROLS AND FOOTER
let visibilitySwap uri visibility actualVisibility dispatch =
  li
    [ OnClick (fun _ -> ChangeVisibility visibility |> dispatch) ]
    [ a [ Href uri
          classList ["selected", visibility = actualVisibility] ]
          [ str visibility ] ]

let viewControlsFilters visibility dispatch =
  ul
    [ ClassName "filters" ]
    [ visibilitySwap "#/" ALL_TODOS visibility dispatch
      str " "
      visibilitySwap "#/active" ACTIVE_TODOS visibility dispatch
      str " "
      visibilitySwap "#/completed" COMPLETED_TODOS visibility dispatch ]

let viewControlsCount entriesLeft =
  let item =
      if entriesLeft = 1 then " item" else " items"

  span
      [ ClassName "todo-count" ]
      [ strong [] [ str (string entriesLeft) ]
        str (item + " left") ]

let viewControlsClear entriesCompleted dispatch =
  button
    [ ClassName "clear-completed"
      Hidden (entriesCompleted = 0)
      OnClick (fun _ -> DeleteComplete |> dispatch)]
    [ str ("Clear completed (" + (string entriesCompleted) + ")") ]

let viewControls visibility entries dispatch =
  let entriesCompleted =
      entries
      |> List.filter (fun t -> t.completed)
      |> List.length

  let entriesLeft =
      List.length entries - entriesCompleted

  footer
      [ ClassName "footer"
        Hidden (List.isEmpty entries) ]
      [ lazyView viewControlsCount entriesLeft
        lazyView2 viewControlsFilters visibility dispatch
        lazyView2 viewControlsClear entriesCompleted dispatch ]


let infoFooter =
  footer [ ClassName "info" ]
    [ p []
        [ str "Double-click to edit a todo" ]
      p []
        [ str "Ported from Elm by "
          a [ Href "https://github.com/et1975" ] [ str "Eugene Tolmachev" ]]
      p []
        [ str "Part of "
          a [ Href "http://todomvc.com" ] [ str "TodoMVC" ]]
    ]

let view model dispatch =
  div
    [ ClassName "todomvc-wrapper"]
    [ section
        [ ClassName "todoapp" ]
        [ lazyView2 viewInput model.field dispatch
          lazyView3 viewEntries model.visibility model.entries dispatch
          lazyView3 viewControls model.visibility model.entries dispatch ]
      infoFooter ]

open Elmish.Debug
// App
Program.mkProgram (S.load >> init) updateWithStorage view
|> Program.withReactBatched "todoapp"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
