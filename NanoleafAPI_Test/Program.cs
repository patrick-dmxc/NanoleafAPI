using Microsoft.Extensions.Logging;
using NanoleafAPI;
using System;
using System.Linq;
using System.Threading;

namespace NanoleafTest
{
    class Program
    {
        static ILogger _logger;
        const string port = "16021";
        const string ip_static = "";
        const string AUTH_TOKEN_static = "";

        static Controller controller = null;
        static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddFilter(nameof(Communication), LogLevel.Debug);
            });

            Tools.LoggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger("NanoleafTest");
            _logger?.LogInformation("Press Enter 5 times for Shutdown");
            Communication.StartEventListener();
            Communication.DeviceDiscovered += Communication_DeviceDiscovered;
            Communication.StaticOnTouchEvent += Communication_StaticOnTouchEvent;
            Communication.StaticOnLayoutEvent += Communication_StaticOnLayoutEvent;
            Communication.StaticOnGestureEvent += Communication_StaticOnGestureEvent;
            Communication.StaticOnEffectEvent += Communication_StaticOnEffectEvent;
            Communication.StaticOnStateEvent += Communication_StaticOnStateEvent;

            string ip;
            string AUTH_TOKEN;
            if (!String.IsNullOrEmpty(ip_static))
                ip = ip_static;
            else
            {
                Console.WriteLine("Please enter IP address of controller: ");
                ip = Console.ReadLine();
            }

            if (!String.IsNullOrEmpty(AUTH_TOKEN_static))
                AUTH_TOKEN = AUTH_TOKEN_static;
            else
            {
                Console.WriteLine("Please enter AUTH_TOKEN of controller: ");
                AUTH_TOKEN = Console.ReadLine();
            }

            controller = new Controller(ip, port, AUTH_TOKEN);
            bool alive = true;
            Thread taskStream = new Thread(() =>
            {
                byte val = 0;
                byte colorVal;
                while (alive)
                {
                    foreach (var p in controller.SubDevices.Select((value, index)=> new { value, index }))
                    {
                        colorVal = (byte)(val + p.index);
                        var rgbw = new RGBW(colorVal, colorVal, 0, 0);
                        _ = controller.SetSubDeviceColor(p.value.ID, rgbw);
                    }
                    Thread.Sleep(10);
                    val++;
                }
            });
            taskStream.IsBackground = true;
            taskStream.Priority = ThreadPriority.Highest;
            taskStream.Start();

            Thread.Sleep(2000);

            controller.StartStreaming();

            Console.ReadLine();
            _ = controller.SelfDestruction(true);
            _logger?.LogInformation("User Deleted");
            alive = false;

            Console.ReadLine();
        }

        private static void Communication_StaticOnStateEvent(object sender, StateEventArgs e)
        {
            _logger?.LogInformation($"{e.IP}: StateEvent: EventsCount:{e.StateEvents.Events.Count()}");
            foreach (var _event in e.StateEvents.Events)
                _logger?.LogInformation(_event.ToString());
        }

        private static void Communication_StaticOnEffectEvent(object sender, EffectEventArgs e)
        {
            _logger?.LogInformation($"{e.IP}: EffectEvent: EventsCount:{e.EffectEvents.Events.Count()}");
            foreach (var _event in e.EffectEvents.Events)
                _logger?.LogInformation(_event.ToString());
        }

        private static void Communication_StaticOnGestureEvent(object sender, GestureEventArgs e)
        {
            _logger?.LogInformation($"{e.IP}: GestureEvent: EventsCount:{e.GestureEvents.Events.Count()}");
            foreach (var _event in e.GestureEvents.Events)
                _logger?.LogInformation(_event.ToString());
        }

        private static void Communication_StaticOnLayoutEvent(object sender, LayoutEventArgs e)
        {
            foreach (var _event in e.LayoutEvents.Events)
            {
                switch (_event.Attribute)
                {
                    case LayoutEvent.EAttribute.Layout:
                        _logger?.LogInformation($"{e.IP}: Layout Changed: NumberOfPanels: {_event.Layout.Value.NumberOfPanels}");
                        foreach (var pp in _event.Layout.Value.PanelPositions)
                            _logger?.LogInformation(pp.ToString());
                        break;
                    case LayoutEvent.EAttribute.GlobalOrientation:
                        _logger?.LogInformation($"{e.IP}: Layout Changed: GlobalOrientation: {_event.GlobalOrientation}");
                        foreach (var pp in _event.Layout.Value.PanelPositions)
                            _logger?.LogInformation(pp.ToString());
                        break;
                }
            }
        }

        private static void Communication_StaticOnTouchEvent(object sender, TouchEventArgs e)
        {
            _logger?.LogInformation($"{e.IP}: TouchEvent: TouchedPanels{e.TouchEvent.TouchedPanelsNumber} EventsCount:{e.TouchEvent.TouchPanelEvents.Count}");
            foreach (var _event in e.TouchEvent.TouchPanelEvents)
            {
                if (_event.PanelIdSwipedFrom.HasValue)
                    _logger?.LogInformation($"PanelID: {_event.PanelId}, {_event.Type}, SwipedID: {_event.PanelIdSwipedFrom} , Strength:{_event.Strength}");
                else
                    _logger?.LogInformation($"PanelID: {_event.PanelId}, {_event.Type}, Strength:{_event.Strength}");
            }
        }
        private static void Communication_DeviceDiscovered(object sender, DiscoveredEventArgs e)
        {
            _logger?.LogInformation($"Device Discovered: {e.DiscoveredDevice.ToString()}");
        }
    }
}
