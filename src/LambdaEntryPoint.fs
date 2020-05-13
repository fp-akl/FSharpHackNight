namespace HackNightPlanningPoker
open Microsoft.AspNetCore.Hosting

type LambdaEntryPoint() =

    inherit Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction()

    override this.Init(builder: IWebHostBuilder) =
        builder
            .UseStartup<Startup>()
        |> ignore
