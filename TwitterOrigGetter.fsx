#r "System.Net.Http.dll"

open System
open System.IO
open System.Net.Http
open System.Net.NetworkInformation

type TwitterConfig = {
    ThumbPath: string
    OrigPath: string
    OrigUrlTemplate: Printf.StringFormat<string -> string>
}

let parseConfig () = 
    let configPath = __SOURCE_DIRECTORY__ + @"\twitterConfig.txt"
    let lines = File.ReadAllLines(configPath)
    let parsedDict = 
        lines
        |> Seq.map (fun line -> 
            let lineArr = line.Split([| "=>" |], StringSplitOptions.RemoveEmptyEntries)
            lineArr.[0], lineArr.[1])
        |> dict
    {
        ThumbPath = parsedDict.["thumbPath"]
        OrigPath = parsedDict.["origPath"]
        OrigUrlTemplate = (Printf.StringFormat<string -> string>)parsedDict.["origUrlTemplate"]
    }

let deleteAll path =
    path
    |> Directory.EnumerateFiles
    |> Seq.iter File.Delete

let isNetworkAvailable () =
    NetworkInterface.GetIsNetworkAvailable()

let run() =
    if not <| isNetworkAvailable() then ()
    else
        let config = parseConfig()
        let thumbPath = config.ThumbPath
        let origPath = config.OrigPath
        let origUrlTemplate = config.OrigUrlTemplate
        
        let origImgRequestParams = 
            thumbPath
            |> Directory.EnumerateFiles
            |> Seq.map (fun filePath ->
                let file = FileInfo filePath
                file.Name, sprintf origUrlTemplate file.Name)
        
        use httpClient = new HttpClient()
        for origImgRequestParam in origImgRequestParams do
            let name, url = origImgRequestParam
            let origImagePath = sprintf @"%s\%s" origPath name
            if File.Exists(origImagePath) then
                ()
            else
                let response = httpClient.GetByteArrayAsync(url).Result
                use imageFile = new FileStream(origImagePath, FileMode.OpenOrCreate)
                imageFile.Write(response, 0, response.Length)
                printfn "%s" origImagePath

        deleteAll thumbPath

run()