using Broadlink.NET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using Tocsoft.StreamDeck;

namespace Tocsoft.BroadlinkIRStreamDeckAction
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices(s => {
                s.AddSingleton(new Client());
                s.AddSingleton<DeviceManager>(); 
            })
            .ConfigureStreamDeck(args, c =>
                {
                    c.AddAction<TriggerHandler>(a =>
                    {
                        a.Name = "Send Keypress";

                    });
                });
    }
}
