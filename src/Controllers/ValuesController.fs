namespace HackNightPlanningPoker.Controllers
open Microsoft.AspNetCore.Mvc
open System

// Critera - features:

// joining / player
// rounds / history 

// voting - state 

type Player = {
    Name: string
}

type Guess = int

type UnfinishedRound = {
    Guesses: (Player * Guess) Set
}

type FinishRound = { 
    Guesses: (Player * Guess) Set
}

module Round = 
    
    let addPlayerGuess (round: UnfinishedRound) player guess = 
        let g = (player, guess)
        { round with Guesses = Set.add g round.Guesses }
        
    let reveal (round: UnfinishedRound) = 
        { FinishRound.Guesses = round.Guesses }

type State = {
    Players: Player list
    CurrentRound: UnfinishedRound
    PastRounds: FinishRound list
} with 
    static member Default = 
        { 
            Players = []
            CurrentRound = { Guesses = Set.empty }
            PastRounds = [] 
        }

module State = 

    let addPlayer player state = 
        let player = { Name = player }

        { state with
            Players = player :: state.Players }

    let addGuess player guess state = 
        { state with 
            CurrentRound = Round.addPlayerGuess state.CurrentRound player guess }

    let getPlayer player state =    
        state.Players |> List.tryFind (fun x -> x.Name = player)

    let reveal state = 
        let finishedRound = state.CurrentRound |> Round.reveal

        { state with 
            CurrentRound = { Guesses = Set.empty}
            PastRounds = finishedRound :: state.PastRounds
            }
        
    
type StateManager() = 

    let mutable state = State.Default

    member this.GetState () = state

    //member this.UpdateState s = 
    //    state <- s

    member this.Modify(f) = 
        let result, s = f state
        state <- s
        result

    member this.Modify'(f) = 
        state <- f state


type AddPlayer() = 
    member val Name = null with get,set

type AddPlayerGuess () = 
    member val Name = null with get,set
    member val Guess = -1 with get, set

type Reveal () = 
    member val Name = null with get,set

[<Route("api/[controller]")>]
type PlayerController(stateManager: StateManager) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.Get() =
        stateManager.GetState() |> OkObjectResult

    [<HttpPost("reveal")>]
    member this.Reveal([<FromBody>] value: Reveal) =
        stateManager.Modify (fun state -> 
            let s = State.reveal state
            s.PastRounds.Head, s
        )
        |> OkObjectResult 
        
    [<HttpPost("guess")>]
    member this.Guess([<FromBody>] value: AddPlayerGuess) =
        value.Name
        |> fun x -> if String.IsNullOrEmpty x then null else x
        |> Option.ofObj
        |> function 
            | None -> 
                BadRequestObjectResult("Player name cannot be null") :> IActionResult

            | Some name -> 
                stateManager.Modify(fun x -> 
                    let p = State.getPlayer name x
                    match p with 
                    | None -> false, x
                    | Some player -> 
                        true, State.addGuess player value.Guess x
                    ) |> OkObjectResult :> IActionResult
       

    [<HttpPost>]
    member this.Post([<FromBody>] value: AddPlayer) =
        value.Name
        |> fun x -> if String.IsNullOrEmpty x then null else x
        |> Option.ofObj
        |> function 
        | None -> 
            BadRequestObjectResult("Player name cannot be null") :> IActionResult
        | Some value -> 
            stateManager.Modify'(State.addPlayer value)
            OkObjectResult("") :> IActionResult