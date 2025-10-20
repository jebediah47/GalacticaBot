namespace GalacticaBot.EnvManager;

using DotNetEnv;
using Microsoft.Extensions.Hosting;

public static class EnvManager
{
    public static void EnsureEnvironment(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            Env.TraversePath().Load();
        }
    }
}

