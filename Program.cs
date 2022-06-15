using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            Yaskawa.Ext.Version version = new Yaskawa.Ext.Version(1, 0, 0); // set extension version
            var languages = new HashSet<string> {"en", "ja"}; // set supported languages
            
            Console.WriteLine(RuntimeInformation.ProcessArchitecture); // log system processor architecture to see if running on pendant or on development pc
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64) //switch according to processor architecture
            {
                extension = new Yaskawa.Ext.Extension("yeu.demo-extension.ext", // register new extension with development settings in pendant
                    version, "YEU", languages, "10.0.0.4", 10080);
            }
            else
            {
                extension = new Yaskawa.Ext.Extension("yeu.test-extension.ext", // register new extension with "release" settings in pendant
                    version, "YEU", languages, "10.0.0.4", 10080);
            }

            apiVersion = extension.apiVersion(); // get pendant API version
            Console.WriteLine("API version: " + apiVersion); // log API version
            pendant = extension.pendant();
            
            extension.subscribeLoggingEvents(); // receive logs from pendant
            extension.copyLoggingToStdOutput = true; // print log() to output
            extension.outputEvents = true; // print out events received
            controller = extension.controller();
            Console.WriteLine("Controller software version:" + controller.softwareVersion()); // log software version
            Console.WriteLine(" monitoring? " + controller.monitoring()); // only monitoring or able to change functions?     
            Console.WriteLine("Current language:" + pendant.currentLanguage()); // pendant language ISO 693-1 code
            Console.WriteLine("Current locale:" + pendant.currentLocale()); // log used locale
        }
        private void Setup()
        {
            // the dispNotice() function is only present in API >= 2.1, so
            //  fall-back to notice() function if running on older SP SDK API
            Yaskawa.Ext.Version requiredMinimumApiVersion = new Yaskawa.Ext.Version(2, 1, 0); // evaluate running apiversion with minimum required version
            if (apiVersion.Nmajor.CompareTo(requiredMinimumApiVersion.Nmajor) >= 0 &&
                apiVersion.Nminor.CompareTo(requiredMinimumApiVersion.Nminor) >= 0 &&
                apiVersion.Npatch.CompareTo(requiredMinimumApiVersion.Npatch) >= 0)
                dispNoticeEnabled = true;
            controller.subscribeEventTypes(new THashSet<ControllerEventType> // subscribe to controller events
            {
                ControllerEventType.OperationMode,
                ControllerEventType.ServoState,
                ControllerEventType.ActiveTool,
                ControllerEventType.PlaybackState,
                ControllerEventType.RemoteMode
            });

            pendant.subscribeEventTypes(new THashSet<PendantEventType> // subscribe to pendant events
            {
                PendantEventType.Startup,
                PendantEventType.Shutdown,
                PendantEventType.SwitchedScreen,
                PendantEventType.UtilityOpened,
                PendantEventType.UtilityClosed,
                PendantEventType.UtilityMoved,
                PendantEventType.Clicked,
                PendantEventType.TextEdited,
                PendantEventType.EditingFinished
            });
            
            List<string> ymlFiles = new List<string> // create list of yml files to be registered in the pendant
            {
                "mainTab.yml",
                "settingsTab.yml",
                "NavTab.yml",
                "NavPanel.yml",
                "UtilWindow.yml"
            };
            foreach (var ymlfile in ymlFiles) // register the yml files
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

            foreach (var file in Directory.GetFiles("images")) // register images to the frontend
            {
                pendant.registerImageFile(file);
            }
            
            pendant.registerUtilityWindow( // register frontend window
                "demoWindow", // id
                "UtilWindow", // Item type
                "MotoMini Demo Extension", // Menu name
                "Demo Utility"); // Window title
            // pendant.registerIntegration("navpanel", // id
            //     IntegrationPoint.NavigationPanel, // where
            //     "NavPanel", // YML Item type
            //     "Demo", // Button label
            //     "images/d-icon-256.png"); // Button icon
            pendant.addEventConsumer(PendantEventType.UtilityOpened, OnOpened); // add onOpened call
            controller.addEventConsumer(ControllerEventType.ServoState, controllerEvents); // add servo call
            pendant.addItemEventConsumer("SETTINGS", PendantEventType.Clicked, OnControlsItemClicked); // add settings button clicked call
            pendant.addItemEventConsumer("START", PendantEventType.Clicked, OnControlsItemClicked); // add start button clicked call 
            pendant.addItemEventConsumer("TextField", PendantEventType.Accepted, OnControlsItemClicked); // add textField.Accepted call
            pendant.addItemEventConsumer("TextField", PendantEventType.EditingFinished, OnControlsItemClicked); // add textField.EditingFinished call
            pendant.addItemEventConsumer("autoCheckBox", PendantEventType.CheckedChanged, OnControlsItemClicked); // add auto checkbox changed call
            pendant.addItemEventConsumer("placeComboBox", PendantEventType.Activated, OnControlsItemClicked); //add place checkbox activated call
            pendant.addItemEventConsumer("MainButton",PendantEventType.Clicked, OnControlsItemClicked); // add main button clicked call
            pendant.addItemEventConsumer("RETURNBEADS",PendantEventType.Pressed, OnControlsItemClicked); // add return beads button pressed call
            pendant.addItemEventConsumer("GPOpen",PendantEventType.Pressed, OnControlsItemClicked); // add gripper open button pressed call
            pendant.addItemEventConsumer("GPClose",PendantEventType.Pressed, OnControlsItemClicked); // add gripper closed button pressed call
            pendant.addItemEventConsumer("settingsTab",PendantEventType.Clicked, OnControlsItemClicked); // add settingsTab button clicked call
            addVariables(); // add controller variables to pendant
        }

        void addVariables()
        {
            letterPlaced.Aspace = AddressSpace.Int; //set the address space for variables
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
            
            letterPlaced.Address = 27; // set variable address
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
            
            controller.setVariableByAddr(StartVariable, 0); // initialize start variable so robot doesn't move immediately
            
            THashSet<string> perms = new THashSet<string>(); // request and add jobcontrol permission to set current job
            perms.Add("jobcontrol");
            controller.requestPermissions(perms);
        }
        void controllerEvents(ControllerEvent e) // log controller events
        {
            Console.WriteLine(e);
        }
        void OnControlsItemClicked(PendantEvent e) // log and do stuff according to pendant events
        {
            try
            {
                var props = e.Props; // extract itemName from PendantEvent
                if (!props.ContainsKey("item")) return;
                var itemName = props["item"].SValue;
                Console.WriteLine("name: " + itemName);
                
                switch (itemName)
                {
                    case "MainButton": // if mainbutton is pressed, switch to main tab
                    {
                        pendant.setProperty("TabBar", "currentIndex", 0);
                        break;
                    }
                    case "SETTINGS": // if settings button OR settingsTab button is pressed, switch to settings tab and update visualisations
                    case "settingsTab":
                    {
                        pendant.setProperty("TabBar", "currentIndex", 1);
                        pendant.setProperty("Open", "color", controller.inputValue(1) ? "green":"blue");
                        pendant.setProperty("Closed", "color", controller.inputValue(2) ? "green":"blue");
                        break;
                    }
                    case "START": // if start button is pressed, run through start sequence
                    {
                        initializeScreen(); // initialise screen
                        if (controller.servoState() != ServoState.On) // check if servos are enabled, if not, throw notice and exit loop
                        {
                            if (dispNoticeEnabled)
                                pendant.dispNotice(Disposition.Negative, "Servo state", "servos not turned enabled or turned on");
                            else
                                pendant.notice("Servo state", "servos not turned enabled or turned on");
                            break;
                        }
                        if (props["checked"].BValue) //check whether the button was pressed or released
                        {
                            pressed = true;
                            pendant.setProperty("startButtonImage", "source", "images/red-button-on.png");
                        }

                        if (!props["checked"].BValue) //check whether the button was pressed or released
                        {
                            pressed = false;
                            pendant.setProperty("startButtonImage", "source", "images/red-button-off.png");
                        }
                        switch (AutonomousMode) // if autonomous radiobutton was selected, start auto sequence
                        {
                            case true:
                            {
                                if (pressed) // if start button was pressed, start word sequence on second thread.
                                { 
                                    pendant.setProperty("autoButtonImage", "source", "images/green-button-on.png");
                                    //T.Priority = ThreadPriority.Lowest;
                                    //T.Start();
                                    Console.WriteLine("Auto sequence started");
                                }

                                break;
                            }
                            case false:
                            {
                                if (pressed) // if start button was pressed, evaluate word and if OK, start sequence
                                {
                                    if (string.IsNullOrEmpty(wordString))
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
                                    Console.WriteLine("direction: " + direction + " mode: " + PlacementMode);
                                    buildWord(wordString, PlacementMode);
                                }
                                Console.WriteLine(PlacementMode);
                                if(PlacementMode)
                                {
                                    Thread.Sleep(5000);
                                    direction = true;
                                    buildWord(wordString, PlacementMode);
                                    direction = false;
                                }

                                initializeScreen(); // reset screen after sequence
                                break;
                            }
                        }
                        break;
                    }
                    case "RETURNBEADS": // if returnbeads button is pressed, flip direction
                    {
                        direction = !direction;
                        break;
                    }
                    case "TextField": // if text is filled, log word
                    {
                        wordString = props["text"].SValue;
                        Console.WriteLine(wordString);
                        break;
                    }
                    case "placeComboBox": // if placebox is activated, store placement value
                    {
                        Console.WriteLine(props["index"].IValue);
                        PlacementMode = props["index"].IValue == 1;
                        break;
                    }
                    case "autoCheckBox": // if auto checkbox is changed, store value and update screen
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
                    case "GPOpen": // if gripper open button is clicked, command gripper open and update screen
                    {
                        if (dispNoticeEnabled)
                            pendant.dispNotice(Disposition.Positive, "Gripper ", "started opening gripper");
                        else
                            pendant.notice("Gripper", "started opening gripper");
                        Console.WriteLine("opening");
                        controller.setOutput(1, false);
                        controller.setOutput(2, true);
                        Thread.Sleep(2000);
                        pendant.setProperty("Open", "color", controller.inputValue(2) ? "green":"blue");
                        pendant.setProperty("Closed", "color", controller.inputValue(1) ? "green":"blue");

                        break;
                    }
                    case "GPClose":// if gripper close button is clicked, command gripper closed and update screen
                    {
                        
                        if (dispNoticeEnabled)
                            pendant.dispNotice(Disposition.Positive, "Gripper ", "started closing gripper");
                        else
                            pendant.notice("Gripper", "started closing gripper");
                        Console.WriteLine("closing");
                        controller.setOutput(1, true);
                        controller.setOutput(2, false);
                        Thread.Sleep(2000);
                        pendant.setProperty("Open", "color", controller.inputValue(2) ? "green":"blue");
                        pendant.setProperty("Closed", "color", controller.inputValue(1) ? "green":"blue");
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

        private static void OnOpened(PendantEvent e) // log screen opened
        {
            Console.WriteLine("screen opened");
        }

        private void initializeScreen() 
        {
            pendant.setProperty("pole", "color", "blue"); // set all elements to their base setting
            pendant.setProperty("poleText", "text", " ");
            for (var element = 0; element < 12; element++)
            {
                pendant.setProperty(("place" + element), "color", "blue");
                pendant.setProperty(("placeText" + element), "text", " ");
            }
            pendant.setProperty("startButtonImage", "source",
                "images/red-button-off.png");
            pendant.setProperty("autoButtonImage", "source",
                "images/green-button-off.png");
            pendant.setProperty("START", "checked", false);
        }
        private bool PingPendant() // ping pendant so it is kept alive
        {
            try
            {
                switch (AutonomousMode && pressed)
                {
                    case true:
                    { 
                        runAutonomously();
                        break;
                    }
                    case false:
                        break;
                }
                     
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return true;
            }
        }
        private void runAutonomously()
        {
            direction = false; // set direction to normal (pick from storage place on poles
            Console.WriteLine("started"); // log started
            foreach (var word in words) // build and pick up each word in wordslist
            {
                buildWord(word, true); // build the word
                direction = true;
                buildWord(word, true);
                direction = false;
            }
        }

        private void close()
        {
            extension.Dispose(); // shutdown extension
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
        
        private void buildWord(string word, bool buildType)
        {
            controller.setVariableByAddr(placeDirection, direction ? 1 : 0); // set placement direction (storage vs places)
            string position = "pole";
            string wordFilled = "";
            if (word.Length < 13) // fill word to be 12 entries long
            {
                wordFilled = word.PadRight(12);
                Console.WriteLine("word: " + word +" word Filled: " + wordFilled + ";");
            }
            // send letters to controller
            List<Any> wordList = new List<Any>() {letter0, letter1, letter2, letter3, letter4, letter5, letter6, 
                letter7, letter8, letter9, letter10, letter11};
            List<VariableAddress> addressList = new List<VariableAddress>() {L0, L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, L11};
            for(int letter = 0; letter < wordList.Count; letter++)
            {
                Console.WriteLine(letter);
                var letterInt = char.ToUpper(wordFilled[letter]) - 65;
                if (letterInt == -33)
                    letterInt = 41;
                Console.WriteLine("LTTnumber: " + letterInt);
                wordList[letter].IValue = letterInt;
                controller.setVariableByAddr(addressList[letter], wordList[letter]);
            }
            controller.setVariableByAddr(placeMode, buildType ? 1 : 0); // set placement on pole or places
            Console.WriteLine("world.length: " + word.Length); // log word length
            for(var t = 0; t < word.Length; t++) // iterate over letters, wait on robot to cycle and update the screen accordingly
            {
                controller.setVariableByAddr(letterPlaced, 0);
                Console.WriteLine(word + " " + word[t]);
                pendant.setProperty(word[t].ToString(), "color", "orange");
                if (buildType)
                {
                    position = "place";
                    pendant.setProperty((position + t), "color", "orange");
                    pendant.setProperty(position + "Text" + t, "text", wordFilled[t].ToString());
                    pendant.setProperty("percentageText","text",t*100/word.Length+"%");
                }
                else
                {
                    pendant.setProperty((position), "color", "orange");
                    Console.WriteLine(word[t]);
                    pendant.setProperty(position+"Text", "text", wordFilled[t].ToString());
                    pendant.setProperty("percentageText","text",t*100/word.Length+"%"); // update percentage of word done
                }
                controller.setVariableByAddr(StartVariable,1);
                while (controller.variableByAddr(letterPlaced).IValue < 1)
                {
                    Console.WriteLine(controller.variableByAddr(letterPlaced).IValue + " " + t);
                    Console.WriteLine(controller.variableByAddr(letterPlaced).IValue < 1);
                }
                pendant.setProperty(word[t].ToString(), "color", "blue");
            }
            pendant.setProperty("percentageText","text","100%"); // set percentage finished
        }

        private static void Main()
        {
            var testExtension = new TestExtension();
            // launch
            try {
                // T = new Thread(testExtension.runAutonomously);
                // T.Priority = ThreadPriority.Highest;
                testExtension.Setup();
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

        private Yaskawa.Ext.Extension extension;
        public static Yaskawa.Ext.Pendant pendant;
        public Yaskawa.Ext.Controller controller;
        private Yaskawa.Ext.Version apiVersion;
        private string wordString;
        private bool PlacementMode;
        private bool AutonomousMode;
        private bool dispNoticeEnabled = false;
        private bool pressed = false;
        private bool direction;
        private static List<string> words = new List<string>
        {
            "YASKAWA",
            "AUTOMATION",
            "MECHATRONICS",
            "ROBOTICS",
            "MACHINE",
            "LABORATORY",
            "FABULOUSNESS",
            "CONTROLLER",
            "ALPHANUMERIC",
            "PERSEVERANCE"
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
        //private static Thread T;
    }
}  