using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
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
            Console.WriteLine(" monitoring? " + controller.monitoring()); // only monitoring or able to change functions?     
            Console.WriteLine("Current language:" + pendant.currentLanguage()); // pendant language ISO 693-1 code
            Console.WriteLine("Current locale:" + pendant.currentLocale());
        }

        public void setup()
        {
            extension.subscribeLoggingEvents();
            lang = pendant.currentLanguage();
            localeName = pendant.currentLocale();
            // the dispNotice() function is only present in API >= 2.1, so
            //  fall-back to notice() function if running on older SP SDK API
            Yaskawa.Ext.Version requiredMinimumApiVersion = new Yaskawa.Ext.Version(2, 1, 0);
            if (apiVersion.Nmajor.CompareTo(requiredMinimumApiVersion.Nmajor) >= 0 &&
                apiVersion.Nminor.CompareTo(requiredMinimumApiVersion.Nminor) >= 0 &&
                apiVersion.Npatch.CompareTo(requiredMinimumApiVersion.Npatch) >= 0)
                dispNoticeEnabled = true;
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
            pendant.addEventConsumer(PendantEventType.UtilityOpened, onOpened);
            controller.addEventConsumer(ControllerEventType.ServoState,controllerEvents);
            pendant.addItemEventConsumer("SETTINGS", PendantEventType.Clicked, onControlsItemClicked);
            pendant.addItemEventConsumer("START", PendantEventType.Clicked, onControlsItemClicked);
            pendant.addItemEventConsumer("TextField", PendantEventType.Accepted, onControlsItemClicked);
            pendant.addItemEventConsumer("autoCheckBox", PendantEventType.CheckedChanged, onControlsItemClicked);
            pendant.addItemEventConsumer("placeCheckBox", PendantEventType.CheckedChanged, onControlsItemClicked);
        }
        private int _clickCount = 0;

        void controllerEvents(ControllerEvent e)
        {
            Console.WriteLine(e);
        }
        void onControlsItemClicked(PendantEvent e)
        {
            try
            {
                var props = e.Props;
                if (props.ContainsKey("item"))
                {
                    var itemName = props["item"].SValue;
                    Console.WriteLine("name: " + itemName);
                    switch (itemName)
                    {
                        // show a notice in reponse to button clicked
                        case "SETTINGS":
                        {
                            if (dispNoticeEnabled)
                                pendant.dispNotice(Disposition.Positive, "Success", "It worked!");
                            else
                                pendant.notice("Success", "It worked!");
                            pendant.setProperty("A", "color", "green");
                            //pendant.openUtilityWindow("settingsTab");
                            break;
                        }
                        case "START":
                            if (dispNoticeEnabled)
                                pendant.dispNotice(Disposition.Positive, "A Notice", "For your information.");
                            else
                                pendant.notice("A Notice", "For your information.");
                            pendant.setProperty("A", "color", "red");
                            break;
                        case "TextField":
                            if (dispNoticeEnabled)
                                pendant.dispNotice(Disposition.Positive, "word entered: ", props["text"].SValue);
                            else
                                pendant.notice("word entered: ",props["text"].SValue);
                            break;
                        case "placeCheckBox":
                            Console.WriteLine(props["checked"].BValue);
                            break;
                        case "autoCheckBox":
                            Console.WriteLine(props["checked"].BValue);
                            break;
                    }

                }

            }
            catch (Exception ex)
            {
                // display error
                Console.WriteLine("Unable to process Clicked event :" + ex.Message);
            }
        }

        public void onOpened(PendantEvent e)
        {
            Console.WriteLine("screen opened");
        }
        private bool PingPendant()
        {
            try
            {
                var x = 10;
                if (x <= 100)
                {
                    x += 1;
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return true;
            }
        }

        public void close()
        {
            run = false;
            extension.Dispose();
        }

        static void Main()
        {
            //var testExtension = new TestExtension();
            //testExtension.extension.run(testExtension.PingPendant);
            
            var testExtension = new TestExtension();
            // launch
                
            try {
                testExtension.setup();
            } catch (Exception e) {
                Console.WriteLine("Extension failed in setup, aborting: "+e);
                return;
            }

            // run 'forever' (or until API service shuts down)
            try {
                testExtension.extension.run(testExtension.PingPendant);
            } catch (Exception e) {
                Console.WriteLine("Exception occured:"+e);
            }

            finally {
                if (testExtension != null)
                    testExtension.close();
            }
        }
        protected Yaskawa.Ext.Extension extension;
        protected Yaskawa.Ext.Pendant pendant;
        protected Yaskawa.Ext.Controller controller;
        private bool _quit;
        protected Yaskawa.Ext.Version apiVersion;
        protected bool run = new bool();
        protected string lang;
        protected string localeName;
        protected CultureInfo locale;
        protected CultureTypes strings;
        protected private bool dispNoticeEnabled = false;
    }
}  