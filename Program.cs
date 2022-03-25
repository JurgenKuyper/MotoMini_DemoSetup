using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Thrift.Collections;
using Yaskawa.Ext.API;
using Version = System.Version;

//using System.Windows.Forms;

namespace MotoMini_DemoSetup
{
    internal class TestExtension
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
        private void Setup()
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
            pendant.addEventConsumer(PendantEventType.UtilityOpened, OnOpened);
            controller.addEventConsumer(ControllerEventType.ServoState,controllerEvents);
            pendant.addItemEventConsumer("SETTINGS", PendantEventType.Clicked, OnControlsItemClicked);
            pendant.addItemEventConsumer("START", PendantEventType.Clicked, OnControlsItemClicked);
            pendant.addItemEventConsumer("TextField", PendantEventType.Accepted, OnControlsItemClicked);
            pendant.addItemEventConsumer("autoCheckBox", PendantEventType.CheckedChanged, OnControlsItemClicked);
            pendant.addItemEventConsumer("placeComboBox", PendantEventType.Activated, OnControlsItemClicked);
            pendant.addItemEventConsumer("MainButton",PendantEventType.Clicked, OnControlsItemClicked);
        }
        
        void controllerEvents(ControllerEvent e)
        {
            Console.WriteLine(e);
        }
        void OnControlsItemClicked(PendantEvent e)
        {
            try
            {
                var props = e.Props;
                if (!props.ContainsKey("item")) return;
                var itemName = props["item"].SValue;
                Console.WriteLine("name: " + itemName);
                switch (itemName)
                {
                    case "MainButton":
                    {
                        pendant.setProperty("TabBar", "currentIndex", 0);
                        break;
                    }
                    case "SETTINGS":
                    {
                        //pendant.openUtilityWindow("settingsTab");
                        pendant.setProperty("TabBar", "currentIndex", 1);
                        break;
                    }
                    case "START":
                    {
                        pendant.setProperty(("pole"), "color", "blue");
                        for (var element = 0; element < 12; element++)
                        {
                            pendant.setProperty(("place" + element), "color", "blue");
                        }
                        if (dispNoticeEnabled)
                            pendant.dispNotice(Disposition.Positive, "Started sequence", "It worked!");
                        else
                            pendant.notice("Started sequence", "It worked!");
                        if (props["checked"].BValue)
                        {
                            pressed = true;
                            pendant.setProperty("startButtonImage", "source", "images/red-button-on.png");
                        }

                        if (!props["checked"].BValue)
                        {
                            pressed = false;
                            pendant.setProperty("startButtonImage", "source", "images/red-button-off.png");
                        }
                        switch (AutonomousMode)
                        {
                            case true:
                            {
                                RunAutonomously();
                                Console.WriteLine("started");
                                foreach (PendantEvent ev in pendant.events())
                                {
                                    if (!ev.Props.ContainsKey("item")) return;
                                    var item = ev.Props["item"].SValue;
                                    if (item == "START")
                                    {
                                        if (!ev.Props["checked"].BValue)
                                        {
                                            pendant.setProperty("startButtonImage", "source", "images/red-button-off.png");
                                            Console.WriteLine("recieved shutdown");
                                            break;
                                        }
                                    }
                                }
                                RunAutonomously();
                                break;
                            }
                            case false:
                            {
                                BuildWord(wordString, PlacementMode);
                                break;
                            }
                        }
                        break;
                    }
                    case "TextField":
                    {
                        if (dispNoticeEnabled)
                            pendant.dispNotice(Disposition.Positive, "word entered: ", props["text"].SValue);
                        else
                            pendant.notice("word entered: ", props["text"].SValue);
                        wordString = props["text"].SValue;
                        Console.WriteLine(wordString);
                        break;
                    }
                    case "placeComboBox":
                    {
                        foreach (var p in e.Props)
                            Console.Write("   " + p.Key + ":" + p.Value);
                        Console.WriteLine(props["index"].IValue);
                        PlacementMode = props["index"].IValue == 1;
                        break;
                    }
                    case "autoCheckBox":
                    {
                        Console.WriteLine(props["checked"].BValue);
                        AutonomousMode = props["checked"].BValue;
                        switch (AutonomousMode)
                        {
                            case true:
                            {
                                pendant.setProperty("autoButtonImage", "source", "images/green-button-on.png");
                                break;
                            }
                            case false:
                            {
                                pendant.setProperty("autoButtonImage", "source", "images/green-button-off.png");
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // display error
                Console.WriteLine("Unable to process Clicked event :" + ex);
            }
        }

        private static void OnOpened(PendantEvent e)
        {
            Console.WriteLine("screen opened");
        }

        private static bool PingPendant()
        {
            try
            {
                //CheckStatus();
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return true;
            }
        }
        private static void RunAutonomously()
        {
            foreach (var word in words)
            {
                BuildStaticWord(word);
                //pendant.setProperty(("pole"), "color", "blue");
                for (var element = 0; element < 12; element++)
                {
                    pendant.setProperty(("place" + element), "color", "blue");
                }
                //Thread.Sleep(1000);
            }
        }

        private void Close()
        {
            run = false;
            extension.Dispose();
        }
        private static void BuildStaticWord(string word)
        {
            for(var l = 0; l < word.Length; l++)
            {
                pendant.setProperty("percentageText","text",l*100/word.Length+"%");
                pendant.setProperty(word[l].ToString(), "color", "orange");
                //controller.variableByAddr("I001");
                pendant.setProperty("place" + l, "color", "orange");
                pendant.setProperty(word[l].ToString(), "color", "blue");
            }
            pendant.setProperty("percentageText","text","100%");
        }
        private void BuildWord(string word, bool buildType)
        {
            var position = "pole";
            for(var t = 0; t < word.Length; t++)
            {
                pendant.setProperty(word[t].ToString(), "color", "orange");
                //controller.variableByAddr("I001");
                if (buildType)
                {
                    position = "place";
                    pendant.setProperty((position + t), "color", "orange");
                    pendant.setProperty("percentageText","text",t*100/word.Length+"%");
                }
                else
                {
                    pendant.setProperty((position), "color", "orange");
                    pendant.setProperty("percentageText","text",t*100/word.Length+"%");
                }
                pendant.setProperty(word[t].ToString(), "color", "blue");
            }
            pendant.setProperty("percentageText","text","100%");
        }

        private static void Main()
        {
            //var testExtension = new TestExtension();
            //testExtension.extension.run(testExtension.PingPendant);
            
            var testExtension = new TestExtension();
            // launch
                
            try {
                testExtension.Setup();
            } catch (Exception e) {
                Console.WriteLine("Extension failed in setup, aborting: "+e);
                return;
            }

            // run 'forever' (or until API service shuts down)
            try {
                testExtension.extension.run(PingPendant);
            } catch (Exception e) {
                Console.WriteLine("Exception occured:"+e);
            }

            finally {
                if (testExtension != null)
                    testExtension.Close();
            }
        }

        private Yaskawa.Ext.Extension extension;
        private static Yaskawa.Ext.Pendant pendant;
        private Yaskawa.Ext.Controller controller;
        private bool _quit;
        private Yaskawa.Ext.Version apiVersion;
        private bool run = new bool();
        private string wordString;
        private string lang;
        private string localeName;
        private bool PlacementMode;
        private bool AutonomousMode;
        protected CultureInfo locale;
        protected CultureTypes strings;
        protected bool dispNoticeEnabled = false;
        protected bool pressed = false;
        private static List<string> words = new List<string>
        {
            "YASKAWA",
            "ATOMETIZING",
            "MECHATRONICS",
            "ROBOTICS",
            "YOLO",
            "LABORATORY",
            "FABULOUSNESS",
            "AGREGATIONS",
            "ALPHANUMERIC",
            "ANTIMAGNETIC"
        };
        Thread t = new Thread(RunAutonomously);
    }
}  