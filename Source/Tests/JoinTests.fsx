open System.Linq

let data = [ "S1"; "S2"; "S3"]

seq {
    for i in 0..data.Length do
        yield String.concat "/"  <| data.Take(i)
} |> printfn "%A"


