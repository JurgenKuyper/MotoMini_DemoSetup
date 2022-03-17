using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Timers;
using Thrift.Collections;
using Yaskawa.Ext.API;
using Version = System.Version;

//using System.Windows.Forms;

namespace MotoMini_DemoSetup
{
    class TestExtension
    {
        private TestExtension()
        {
            var version = new Yaskawa.Ext.Version(1, 0, 0);
            var languages = new HashSet<string> {"en", "ja"};

            extension = new Yaskawa.Ext.Extension("yeu.test-extension.ext",
                version, "YEU", languages, "10.0.0.4", 10080);
            apiVersion = extension.apiVersion();
            Console.WriteLine("API version: " + apiVersion);

            pendant = extension.pendant();
            extension.subscribeLoggingEvents(); // receive logs from pendant
            extension.copyLoggingToStdOutput = true; // print log() to output
            extension.outputEvents = true; // print out events received
            controller = extension.controller();
            Console.WriteLine("Controller software version:" + controller.softwareVersion());
        }

        public void Run()
        {
            Console.WriteLine(" monitoring? " +
                              controller.monitoring()); // only monitoring or able to change functions?     
            Console.WriteLine("Current language:" + pendant.currentLanguage()); // pendant language ISO 693-1 code
            Console.WriteLine("Current locale:" + pendant.currentLocale());
            Console.WriteLine("Screen Name:" + pendant.currentScreenName());
            List<string> ymlFiles = new List<string>
            {
                "ControlsTab.yml",
                "mainTab.yml",
                "settingsTab.yml",
                "NavTab.yml",
                "NavPanel.yml",
                "UtilWindow.yml"
            };
            foreach (var ymlfile in ymlFiles)
            {
                try
                {
                    pendant.registerYMLFile(ymlfile);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"YML Errors encountered: {e}");
                }
            }

            foreach (var file in Directory.GetFiles("images"))
            {
                pendant.registerImageFile(file);
            }

            //string yml = File.ReadAllText("MotoMini_DemoSetup/settings.yml");
            //var errors = pendant.registerYML(yml);
            // if (errors.Count > 0) {
            //     Console.WriteLine("YML Errors encountered:");
            //     foreach(var e in errors)
            //         Console.WriteLine("  "+e);
            // }
            //pendant.registerUtilityWindow("ymlutil","settings","YML Extension", "YML Extension");
            pendant.registerUtilityWindow(
                "demoWindow", // id
                "UtilWindow", // Item type
                "Demo Extension", // Menu name
                "Demo Utility"); // Window title
            pendant.registerIntegration("navpanel", // id
                IntegrationPoint.NavigationPanel, // where
                "NavPanel", // YML Item type
                "Demo", // Button label
                "images/d-icon-256.png"); // Button icon
            controller.subscribeEventTypes(new THashSet<ControllerEventType>
            {
                ControllerEventType.OperationMode,
                ControllerEventType.ServoState,
                ControllerEventType.ActiveTool,
                ControllerEventType.PlaybackState,
                ControllerEventType.RemoteMode
            });

            pendant.subscribeEventTypes(new THashSet<PendantEventType>
            {
                PendantEventType.Startup,
                PendantEventType.Shutdown,
                PendantEventType.SwitchedScreen,
                PendantEventType.UtilityOpened,
                PendantEventType.UtilityClosed,
                PendantEventType.UtilityMoved,
                PendantEventType.Clicked
            });
            pendant.addItemEventConsumer("settings", PendantEventType.Clicked, onControlsItemClicked);
            pendant.addItemEventConsumer("start", PendantEventType.Clicked, onControlsItemClicked);

            extension.ping();

            //Application.EnableVisualStyles();
            //utility = new Utility();


            //eventPollTimer.AutoReset = true;
            //eventPollTimer.Tick += new EventHandler(PollForEvents);

            _quit = false;
            /*eventPollTimer = new System.Timers.Timer(500);
            pingTimer = new System.Timers.Timer(2000);
            do {
                //Application.DoEvents();
                // pingTimer.Elapsed += new System.Timers.ElapsedEventHandler(PingPendant);
                // pingTimer.Start();
                eventPollTimer.Elapsed += new System.Timers.ElapsedEventHandler(PollForEvents);
                eventPollTimer.Start();
                
                //Console.WriteLine(quit);
            } while (!_quit);
            eventPollTimer.Enabled = false;*/
            extension.Dispose();
        }

        private int _clickCount = 0;

        /*private void PollForEvents(Object o, EventArgs args)
        {
            Any a = new Any();
            foreach (ControllerEvent e in controller.events()) {
                Console.Write("ControllerEvent: "+e.EventType);
                foreach(var p in e.Props) 
                    Console.Write("   "+p.Key+":"+p.Value);
                Console.WriteLine();
            }
            foreach (PendantEvent e in pendant.events()) 
            {
                
                Console.WriteLine("PendantEvent: "+e.EventType);
                //Console.WriteLine(e.Props);
                foreach (var p in e.Props)
                {
                    Console.WriteLine("  " + p.Key + ": " + p.Value);
                }
                switch (e.EventType)
                {
                    case PendantEventType.Clicked:
                    {
                        if (String.Equals(e.Props["item"].SValue, "MYBUTTON"))
                        {

                            a.SValue = "Button clicked " + (++this._clickCount).ToString() + " times.";
                            pendant.setProperty("mytext", "text", a);
                            pendant.setProperty("stacktest", "currentIndex", 2);
                            Console.WriteLine("p.prop: " + pendant.property("mytext", "text"));
                        }
                    } break;
                    case PendantEventType.Shutdown: {
                        _quit = true;
                        Console.WriteLine(_quit);
                    } break;
                    case PendantEventType.Startup:
                    {
                        Console.Write("Pendant started");
                    } break;
                }
            }
        }*/
        void onControlsItemClicked(PendantEvent e)
        {
            try
            {
                var props = e.Props;
                if (props.ContainsKey("item"))
                {
                    var itemName = props["item"].SValue;
                    // show a notice in reponse to button clicked
                    if (itemName.Equals("settings"))
                    {
                        // the dispNotice() function is only present in API >= 2.1, so
                        //  fall-back to notice() function if running on older SP SDK API
                        Yaskawa.Ext.Version requiredMinimumApiVersion = new Yaskawa.Ext.Version(2, 1, 0);
                        if (apiVersion.Nmajor.CompareTo(requiredMinimumApiVersion.Nmajor) >= 0 && apiVersion.Nminor.CompareTo(requiredMinimumApiVersion.Nminor) >= 0 && apiVersion.Npatch.CompareTo(requiredMinimumApiVersion.Npatch) >= 0)
                            pendant.dispNotice(Disposition.Positive, "Success", "It worked!");
                        else
                            pendant.notice("Success", "It worked!");
                    }
                    else if (itemName.Equals("start"))
                    {
                        pendant.notice("A Notice", "For your information.");
                    }

                }

            }
            catch (Exception ex)
            {
                // display error
                Console.WriteLine("Unable to process Clicked event :" + ex.Message);
            }
        }

        private void PingPendant(Object o, EventArgs args)
        {
            Console.WriteLine("pinging");
            var x = pendant.events();
            Console.WriteLine("has pinged {x}");
            //extension.ping();
            Console.WriteLine("has pinged");
        }

        static void Main()
        {
            var testExtension = new TestExtension();
            testExtension.Run();
        }
        protected Yaskawa.Ext.Extension extension;
        protected Yaskawa.Ext.Pendant pendant;
        protected Yaskawa.Ext.Controller controller;
        private bool _quit;
        protected Yaskawa.Ext.Version apiVersion;
    }
}  