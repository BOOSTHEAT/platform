using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ImpliciX.SharedKernel.Logger;
using Serilog;
using Serilog.Events;
using Log = ImpliciX.Language.Core.Log;

namespace ImpliciX.SharedKernel.Redis;

public static class RedisTestContainer
{
    public const int REDIS_PORT_LOCAL = 6381;
    public const int REDIS_PORT_CONTAINER = 6379;

    private static IContainer _container; 
    
    static RedisTestContainer()
    {
        var dockerEndpoint = Environment.GetEnvironmentVariable("DOCKER_HOST") 
                             ?? "unix:/var/run/docker.sock";
        _container = new ContainerBuilder()
            .WithDockerEndpoint(dockerEndpoint)
            .WithImage("redis/redis-stack:latest")
            .WithPortBinding(REDIS_PORT_LOCAL, REDIS_PORT_CONTAINER)
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .Build();
    }
    
    public static void Start()
    {
        if(_container.State != TestcontainersStates.Running)
            Task.WaitAll(_container.StopAsync(),_container.StartAsync());
    }
    
    public static async Task<string> Exec(string cmd)
    {
        var result = await _container.ExecAsync(new[] { "/bin/sh", "-c", cmd});
        return result.Stdout;
    }

    public static void Stop()
    {
        _container.StopAsync().Wait();
    }
    

}