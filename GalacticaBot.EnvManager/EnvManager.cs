using DotNetEnv;
using Microsoft.Extensions.Hosting;

namespace GalacticaBot.EnvManager;

public static class EnvManager
{
    public static void EnsureEnvironment(IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            Env.TraversePath().Load();
    }
}
