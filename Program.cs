using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Thrift.Collections;
using Yaskawa.Ext.API;

//using System.Windows.Forms;

namespace MotoMini_DemoSetup
{
    internal class TestExtension
    {
        private TestExtension()
        {
            Yaskawa.Ext.Version version = new Yaskawa.Ext.Version(1, 0, 0);
            var languages = new HashSet<string> {"en", "ja"};

            extension = new Yaskawa.Ext.Extension("yeu.demo-extension.ext",
                 version, "YEU", languages, "10.0.0.4", 10080);
            // extension = new Yaskawa.Ext.Extension("yeu.test-extension.ext",
            //     version, "YEU", languages, "localhost", 10080);
            
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
            pendant.addItemEventConsumer("RETURNBEADS",PendantEventType.Pressed, OnControlsItemClicked);
            addVariables();
        }

        void addVariables()
        {
            letterPlaced.Aspace = AddressSpace.Int;
            StartVariable.Aspace = AddressSpace.Byte;
            L0.Aspace = AddressSpace.Byte;
            L1.Aspace = AddressSpace.Byte;
            L2.Aspace = AddressSpace.Byte;
            L3.Aspace = AddressSpace.Byte;
            L4.Aspace = AddressSpace.Byte;
            L5.Aspace = AddressSpace.Byte;
            L6.Aspace = AddressSpace.Byte;
            L7.Aspace = AddressSpace.Byte;
            L8.Aspace = AddressSpace.Byte;
            L9.Aspace = AddressSpace.Byte;
            L10.Aspace = AddressSpace.Byte;
            L11.Aspace = AddressSpace.Byte;
            placeMode.Aspace = AddressSpace.Byte;
            Errors.Aspace = AddressSpace.Byte;
            placeDirection.Aspace = AddressSpace.Byte;
            letterPlaced.Address = 27;
            StartVariable.Address = 0;
            L0.Address = 1;
            L1.Address = 2;
            L2.Address = 3;
            L3.Address = 4;
            L4.Address = 5;
            L5.Address = 6;
            L6.Address = 7;
            L7.Address = 8;
            L8.Address = 9;
            L9.Address = 10;
            L10.Address = 11;
            L11.Address = 12;
            placeMode.Address = 13;
            Errors.Address = 14;
            placeDirection.Address = 15;
            controller.setVariableByAddr(StartVariable, 0);
            THashSet<string> perms = new THashSet<string>();
            perms.Add("jobcontrol");
            controller.requestPermissions(perms);
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
                        
                        if (controller.servoState() != ServoState.On)
                        {
                            if (dispNoticeEnabled)
                                pendant.dispNotice(Disposition.Negative, "Servo state", "servos not turned enabled or turned on");
                            else
                                pendant.notice("Servo state", "servos not turned enabled or turned on");
                            break;
                        }
                        controller.setCurrentJob("PICKER", 1);
                        if (wordString.Length < 1)
                        {
                            if (dispNoticeEnabled)
                                pendant.dispNotice(Disposition.Negative, "minLetters", "empty words are not allowed");
                            else
                                pendant.notice("minLetters", "empty words are not allowed");
                            break;
                        }
                        if (getOccurenceOfChar(wordString) > 5)
                        {
                            if (dispNoticeEnabled)
                                pendant.dispNotice(Disposition.Negative, "maxLetters", "entered too many of one letter");
                            else
                                pendant.notice("maxLetters", "entered too many of one letter");
                            break;
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
                                if (pressed)
                                {
                                    direction = false;
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
                                                pendant.setProperty("startButtonImage", "source",
                                                    "images/red-button-off.png");
                                                Console.WriteLine("received shutdown");
                                                break;
                                            }
                                        }
                                    }

                                    RunAutonomously();
                                }

                                break;
                            }
                            case false:
                            {
                                if(pressed)
                                    BuildWord(wordString, PlacementMode);
                                Thread.Sleep(5000);
                                direction = true;
                                PickWord(wordString,PlacementMode);
                                direction = false;
                                pendant.setProperty("startButtonImage", "source",
                                    "images/red-button-off.png");
                                pendant.setProperty("START", "checked", false);
                                break;
                            }
                        }
                        break;
                    }
                    case "RETURNBEADS":
                    {
                        direction = !direction;
                        break;
                    }
                    case "TextField":
                    {
                        // if (dispNoticeEnabled)
                        //     pendant.dispNotice(Disposition.Positive, "word entered: ", props["text"].SValue);
                        // else
                        //     pendant.notice("word entered: ", props["text"].SValue);
                        wordString = props["text"].SValue;
                        Console.WriteLine(wordString);
                        break;
                    }
                    case "placeComboBox":
                    {
                        // foreach (var p in e.Props)
                        //     Console.Write("   " + p.Key + ":" + p.Value);
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
                Console.WriteLine("Unable to process Clicked event: " + ex.Message + ex.StackTrace);
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
        private void RunAutonomously()
        {
            foreach (var word in words)
            {
                BuildWord(word, true);
                //pendant.setProperty(("pole"), "color", "blue");
                for (var element = 0; element < 12; element++)
                {
                    pendant.setProperty(("place" + element), "color", "blue");
                }
                direction = true;
                PickWord(word, true);
                //Thread.Sleep(1000);
            }
        }

        private void Close()
        {
            run = false;
            extension.Dispose();
        }

        private int getOccurenceOfChar(string str)
        {
            int ASCII_SIZE = 256;
            int max = -1; // Initialize max count
            int []count = new int[ASCII_SIZE];
     
            // Construct character count array
            // from the input string.
            int len = str.Length;
            for (int i = 0; i < len; i++)
                count[str[i]]++;
            
            // Traversing through the string and
            // maintaining the count of each character
            for (int i = 0; i < len; i++) {
                if (max < count[str[i]]) {
                    max = count[str[i]];
                }
            }
            return max;
        }
        private void PickWord(string word, bool buildType)
        {
            string position = "pole";
            string wordFilled = "";
            char[] wordArray = word.ToCharArray();
            word = new string(wordArray);
            //Console.WriteLine("PDA+DIR: " + placeDirection.Address + direction);
            controller.setVariableByAddr(placeDirection, direction ? 1 : 0);
            if (word.Length < 12)
            {
                wordFilled = word.PadRight(12);
                //Console.WriteLine(word);
            }
            List<Any> wordList = new List<Any>() {letter0, letter1, letter2, letter3, letter4, letter5, letter6, 
                letter7, letter8, letter9, letter10, letter11};
            List<VariableAddress> addressList = new List<VariableAddress>() {L0, L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11};
            for(int letter = 0; letter < wordList.Count; letter++)
            {
                var letterInt = char.ToUpper(wordFilled[letter]) - 65;
                if (letterInt == -33)
                    letterInt = 41;
                //Console.WriteLine(letterInt);
                wordList[letter].IValue = letterInt;
                controller.setVariableByAddr(addressList[letter], wordList[letter]);
            }
            controller.setVariableByAddr(placeMode, buildType ? 1 : 0);
            for(var t = 1; t <= word.Length; t++)
            {
                controller.setVariableByAddr(letterPlaced, 0);
                Console.WriteLine(word[word.Length - t]);
                pendant.setProperty(word[word.Length - t].ToString(), "color", "orange");
                if (buildType)
                {
                    position = "place";
                    pendant.setProperty(position + (word.Length - t), "color", "orange");
                    pendant.setProperty("percentageText","text",t*100/word.Length+"%");
                }
                else
                {
                    pendant.setProperty((position), "color", "orange");
                    pendant.setProperty("percentageText","text",t*100/word.Length+"%");
                }
                controller.setVariableByAddr(StartVariable,1);
                while (controller.variableByAddr(letterPlaced).IValue < 1)
                {
                    //Console.WriteLine(controller.variableByAddr(letterPlaced).IValue + " " + t);
                    //Console.WriteLine(controller.variableByAddr(letterPlaced).IValue < 1);
                    Thread.Sleep(2000);
                }
                pendant.setProperty(word[word.Length - t].ToString(), "color", "blue");
                Console.WriteLine(word.Length - t);
                pendant.setProperty(buildType ? position[word.Length - t].ToString() : position, "color", "blue");
            }
            pendant.setProperty("percentageText","text","100%");
            pendant.setProperty(("pole"), "color", "blue");
        }
        private void BuildWord(string word, bool buildType)
        {
            controller.setVariableByAddr(placeDirection, direction ? 1 : 0);
            string position = "pole";
            string wordFilled = "";
            if (word.Length < 12)
            {
                wordFilled = word.PadRight(12);
                //Console.WriteLine(word);
            }
            List<Any> wordList = new List<Any>() {letter0, letter1, letter2, letter3, letter4, letter5, letter6, 
                letter7, letter8, letter9, letter10, letter11};
            List<VariableAddress> addressList = new List<VariableAddress>() {L0, L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11};
            for(int letter = 0; letter < wordList.Count; letter++)
            {
                var letterInt = char.ToUpper(wordFilled[letter]) - 65;
                if (letterInt == -33)
                    letterInt = 41;
                //Console.WriteLine(letterInt);
                wordList[letter].IValue = letterInt;
                controller.setVariableByAddr(addressList[letter], wordList[letter]);
            }
            controller.setVariableByAddr(placeMode, buildType ? 1 : 0);
            for(var t = 0; t < word.Length; t++)
            {
                controller.setVariableByAddr(letterPlaced, 0);
                //Console.WriteLine(word + t);
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
                controller.setVariableByAddr(StartVariable,1);
                while (controller.variableByAddr(letterPlaced).IValue < 1)
                {
                    //Console.WriteLine(controller.variableByAddr(letterPlaced).IValue + " " + t);
                    //Console.WriteLine(controller.variableByAddr(letterPlaced).IValue < 1);
                    Thread.Sleep(2000);
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
        private bool PlacementMode;
        private bool AutonomousMode;
        private bool dispNoticeEnabled = false;
        private bool pressed = false;
        private bool direction;
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

        VariableAddress letterPlaced = new VariableAddress();
        VariableAddress StartVariable = new VariableAddress();
        VariableAddress L0 = new VariableAddress();
        VariableAddress L1 = new VariableAddress();
        VariableAddress L2 = new VariableAddress();
        VariableAddress L3 = new VariableAddress();
        VariableAddress L4 = new VariableAddress();
        VariableAddress L5 = new VariableAddress();
        VariableAddress L6 = new VariableAddress();
        VariableAddress L7 = new VariableAddress();
        VariableAddress L8 = new VariableAddress();
        VariableAddress L9 = new VariableAddress();
        VariableAddress L10 = new VariableAddress();
        VariableAddress L11 = new VariableAddress();
        VariableAddress placeMode = new VariableAddress();
        VariableAddress Errors = new VariableAddress();
        VariableAddress placeDirection = new VariableAddress();
        Any letter0 = new Any();
        Any letter1 = new Any();
        Any letter2 = new Any();
        Any letter3 = new Any();
        Any letter4 = new Any();
        Any letter5 = new Any();
        Any letter6 = new Any();
        Any letter7 = new Any();
        Any letter8 = new Any();
        Any letter9 = new Any();
        Any letter10 = new Any();
        Any letter11 = new Any();
    }
}  