module Controller.Sample

open Saturn
open Giraffe.Core
open Giraffe.ResponseWriters
open FSharp.Control.Tasks.V2.ContextInsensitive
open System

let commentController userId = controller {
    index (fun ctx -> (sprintf "Comment Index handler for user %i" userId ) |> Controller.text ctx)
    add (fun ctx -> (sprintf "Comment Add handler for user %i" userId ) |> Controller.text ctx)
    show (fun ctx id -> (sprintf "Show comment %s handler for user %i" id userId ) |> Controller.text ctx)
    edit (fun ctx id -> (sprintf "Edit comment %s handler for user %i" id userId )  |> Controller.text ctx)
}

let userControllerVersion1 = controller {
    version "1"
    subController "/comments" commentController

    index (fun ctx -> "Index handler version 1" |> Controller.text ctx)
    add (fun ctx -> "Add handler version 1" |> Controller.text ctx)
    show (fun ctx id -> (sprintf "Show handler version 1 - %i" id) |> Controller.text ctx)
    edit (fun ctx id -> (sprintf "Edit handler version 1 - %i" id) |> Controller.text ctx)
}

let userController = controller {
    subController "/comments" commentController

    plug [All] (setHttpHeader "user-controller-common" "123")
    plug [Index; Show] (setHttpHeader "user-controller-specialized" "123")

    index (fun ctx -> "Index handler no version" |> Controller.text ctx)
    show (fun ctx id -> (sprintf "Show handler no version - %i" id) |> Controller.text ctx)
    add (fun ctx -> "Add handler no version" |> Controller.text ctx)
    create (fun ctx -> "Create handler no version" |> Controller.text ctx)
    edit (fun ctx id -> (sprintf "Edit handler no version - %i" id) |> Controller.text ctx)
    update (fun ctx id -> (sprintf "Update handler no version - %i" id) |> Controller.text ctx)
    delete (fun ctx id -> failwith (sprintf "Delete handler no version failed - %i" id) |> Controller.text ctx)
    error_handler (fun ctx ex -> sprintf "Error handler no version - %s" ex.Message |> Controller.text ctx)
}

let deleteController = controller {
    show (fun ctx id -> (sprintf "Show controller. This is the id - %s" id) |> Controller.text ctx)
    delete (fun ctx id -> (sprintf "Delete controller. This is the id - %s" id) |> Controller.text ctx)
    error_handler (fun ctx ex -> sprintf "There was an error - %s" ex.Message |> Controller.text ctx)
}

let deleteMe (id:string) =
    text (sprintf "deletMe YOUR ID IS - %s" id)

type Response = {
    a: string
    b: string
}

type DifferentResponse = {
    c: int
    d: DateTime
}

let typedController = controller {
    index (fun _ -> task {
        return {a = "hello"; b = "world"}
    })

    add (fun _ -> task {
        return {c = 123; d = DateTime.Now}
    })
}

let otherRouter = router {
    get "/dsa" (text "")
    getf "/dsa/%s" (text)
    forwardf "/ddd/%s" (fun (_ : string) -> userControllerVersion1)
    not_found_handler (setStatusCode 404 >=> text "Not Found")
}

let topRouter = router {
    case_insensitive
    forward "/users" userControllerVersion1
    forward "/users" userController
    forward "/typed" typedController
    forwardf "/%s/%s/abc" (fun (_ : string * string) -> otherRouter)

    // ISSUE #162
    // The id that ends up in the controller function looks like the path. It should just be the id
    deletef "/delete/%s" (fun (_:string) -> deleteController)
    getf "/delete/%s" (fun (_:string) -> deleteController)
    //getf "/delete/%s" deleteMe
}

let app = application {
    use_router topRouter
    url "http://0.0.0.0:8085/"
}

[<EntryPoint>]
let main _ =
    run app
    0
