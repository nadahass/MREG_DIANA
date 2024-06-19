/*
This script parses natural language input into forms VoxSim can work with

Reads:      user:speech (StringValue, transcribed speech)
            user:intent:isServoLeft (BoolValue, whether or not the user is
                gesturing "servo left")
            user:intent:isServoRight (BoolValue, whether or not the user is
                gesturing "servo right")
            user:intent:isServoFront (BoolValue, whether or not the user is
                gesturing "servo front" -- we don't really have this gesture)
            user:intent:isServoBack (BoolValue, whether or not the user is
                gesturing "servo back")  
            user:intent:replaceContent (StringValue, optional replacement
                content with an undo)           

            me:standingBy (BoolValue, whether or not Diana is standing by)
            me:question:isAnswering (BoolValue, whether or not Diana is
                currently answering a question) 

Writes:     user:intent:event (StringValue, a predicate-argument representation
                of an event, with NO outstanding variables)
            user:intent:partialEvent (StringValue, a predicate-argument
                representation of an event, but with outstanding variables -
                variables are indicated by numbers in curly braces, e.g., {0},
                the number indicates typing information; {0} = object,
                {1} = location)          
            user:intent:isPosack (BoolValue, whether or not the user is making
                negack -- either modality)
            user:intent:isNegack (BoolValue, whether or not the user is making 
                negack -- either modality)
            user:intent:isServoLeft (BoolValue, whether or not the user is
                gesturing "servo left")
            user:intent:isServoRight (BoolValue, whether or not the user is
                gesturing "servo right")
            user:intent:isServoFront (BoolValue, whether or not the user is
                gesturing "servo front" -- we don't really have this gesture)
            user:intent:isServoBack (BoolValue, whether or not the user is
                gesturing "servo back")  
            user:intent:isNevermind (BoolValue, whether or not the user is making
                nevermind -- either modality)
            user:intent:replaceContent (StringValue, optional replacement
                content with an undo) 
            user:intent:isQuestion (StringValue, whether or not what the user
                just said was a question)

            me:isCheckingServo (BoolValue, whether or not Diana is looking for
                another iteration on a servo gesture)
            me:isInServoLoop (BoolValue, whether or not Diana is executing a servo
                action, and therefore keeps doing it until she receives a cue to
                stop it)           
            me:question:type (StringValue, the "type" or top predicate --
                Wh-word or do/is -- of the question; only "what" or "where"
                currently supported)               
            me:question:content (StringValue, the argument of the question
                predicate)
*/

using UnityEngine;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using static Semantics.LocationSpec;
using System.IO;
using UnityEngine.SceneManagement;
#if !UNITY_WEBGL
using VoxSimPlatform.Network;
using VoxSimPlatform.NLU;
using System.Text;
using VoxSimPlatform.Interaction;
using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning;
using VoxSimPlatform.Vox;
#endif

public class NLUModule : ModuleBase
{
    public enum ParserType
    {
        SimpleParser,
        NLTKParser,
        DianaParser,
        // add other parser types here
    };

    public StreamWriter logFileStream;
    public UnityEngine.UI.InputField InputField;

    // Nada: added is and you for relational and historical REs
    string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "bottle", "one", "is",
        "you", "red", "yellow", "pink", "blue", "white", "purple", "green", "black", "gray" };

    string[] objects = new string[] { "block", "cup", "knife", "plate", "bottle", "one",
        "red", "yellow", "pink", "blue", "white", "purple", "green", "black", "gray", "object" };
    List<string> _relations = new List<string>(new[] {
               
                "touching",
                "in",
                "on",
                "atop", // prithee sirrah, put the black block atop the yellow block
                "port",
                "starboard",
                "afore",
                "astern",
                "at",
                "behind",
                "in front of",
                "beside",
                "near",
                "left of",
                "right of",
                "center of",
                "edge of",
                "under",
                "underneath",
                "below",
                "against",
                "here",
                "there",
                "right",
                "left"

            });
    private static System.Timers.Timer timer;
    private static List<String> trans_no_goal = new List<string>(new[] { "bring", "select", "pick up", "lift", "grab", "grasp", "take", "let go of", "ungrasp", "drop", "release", "find", "go to" });
    private static List<String> trans_goal = new List<string>(new[] { "move", "put", "push", "pull", "slide", "place", "shift", "scoot", "servo", "bring" });
    EventManagementModule emm;
    DialogueInteractionModule dim;

    DianaParser dp;
#if !UNITY_WEBGL
    public CommunicationsBridge communicationsBridge;
#else
    VoxSimPlatform.NLU.INLParser communicationsBridge;
#endif
    public ParserType parser;

    bool parserConnected = false;
    bool parserInited = false;

    static string _parsed;

    static string UserSpeech;
    public string GetUserSpeech() { return UserSpeech; }
    public void setUserSpeech(string value) { UserSpeech = value; }

    //static string plf;
    //public string GetPLF() { return plf; }
    //public void setPLF(string value) { plf = value; }
    string dianaspeech;
    static string speechTime;
    public string GetspeechTime() { return speechTime; }
    public void setspeechTime(string value) { speechTime = value; }

    public string parsed
    {
        get
        {
            return _parsed;
        }
        set
        {
            _parsed = value;
            OnNewParse(_parsed);
        }
    }

    static bool isneg;
    public bool Getisneg() { return isneg; }
    public void setisneg(bool value) { isneg = value; }

    static bool Posack;
    public bool GetPosack() { return Posack; }
    public void setPosack(bool value) { Posack = value; }

    static string mapped2;
    public string Getmapped2() { return mapped2; }
    public void setmapped2(string value) { mapped2 = value; }

    // Use this for initialization
    private void Awake()
    {
       
        emm = GameObject.FindObjectOfType<EventManagementModule>();
        dim = new DialogueInteractionModule();
        dp = new DianaParser();

    }
    void Start()
    {
        base.Start();
        dianaspeech = DataStore.GetStringValue("me:speech:intent");
        DataStore.Subscribe("user:speech", ParseLanguageInput);
        SetTimer();
        parsed = string.Empty;
    }

    // Update is called once per frame
    void Update()
    {
        //SetTimer();
        if (!string.IsNullOrEmpty(DataStore.GetStringValue("me:speech:intent")))
        {
            dianaspeech = DataStore.GetStringValue("me:speech:intent");
        }
        // set the parser here (and not in Start()) because we need to be sure that CommunicationsBridge
        // has fully started before trying access elements of it
        if (!parserConnected)
        {
            switch (parser)
            {
                case ParserType.SimpleParser:
                    communicationsBridge.parser = new VoxSimPlatform.NLU.SimpleParser();
                    break;
#if !UNITY_WEBGL
                case ParserType.DianaParser:
                    communicationsBridge.parser = new VoxSimPlatform.NLU.DianaParser();

                    break;
                case ParserType.NLTKParser:
                    // find the client that handles this connection
                    string clientLabel = "NLTK";
                    VoxSimPlatform.Examples.RESTClients.NLURESTClient nluClient =
                            (VoxSimPlatform.Examples.RESTClients.NLURESTClient)communicationsBridge.FindRESTClientByLabel(clientLabel);

                    // if client found
                    if (nluClient != null)
                    {
                        // the REST client design pattern means we have to add
                        //  a connection checker event handler
                        nluClient.GetOkay += CheckConnection;
                    }
                    else
                    {
                        // if client is not found, the communications bridge's assigned parser will
                        //  still be the default parser
                        Debug.LogWarningFormat("Parser type is set to {0} but no client of label {1} could be found " +
                            "(maybe check your socket configuration)! " +
                            "Parser will remain VoxSim SimpleParser.", parser.ToString(), clientLabel);
                    }
                    break;
#endif

                // in case of other parser types
                // set communiationsBridge.parser = instance of parser class

                default:
                    break;
            }
        }
        else
        {
            if (!parserInited)
            {
                switch (parser)
                {
                    case ParserType.SimpleParser:
                        break;

#if !UNITY_WEBGL
                    case ParserType.DianaParser:
                        communicationsBridge.parser = new VoxSimPlatform.NLU.DianaParser();
                        break;

                    case ParserType.NLTKParser:
                        string clientLabel = "NLTK";
                        VoxSimPlatform.Examples.RESTClients.NLURESTClient nluClient =
                            (VoxSimPlatform.Examples.RESTClients.NLURESTClient)communicationsBridge.FindRESTClientByLabel(clientLabel);

                        // if client found
                        if ((nluClient != null) && (nluClient.isConnected))
                        {
                            // if the client is connected
                            //  create the parser with its expected syntax
                            //  and set an event handler that will call
                            //  communicationsBridge.GrabParse()
                            //  (remove the connection checker event handler
                            //  so it doesn't keep getting invoked)
                            communicationsBridge.parser = new VoxSimPlatform.Examples.NLParsers.PythonJSONParser();
                            communicationsBridge.parser.InitParserService(nluClient,
                                typeof(VoxSimPlatform.Examples.Syntaxes.NLTKSyntax));
                            nluClient.GetOkay -= CheckConnection;
                            nluClient.PostOkay += LookForNewParse;
                        }
                        else
                        {
                            // if client is not found, the communications bridge's assigned parser will
                            //  still be the default parser
                            Debug.LogWarningFormat("Parser type is set to {0} but no client of label {1} could be found " +
                                "(make sure the client is running)! " +
                                "Parser will default to VoxSim SimpleParser.", parser.ToString(), clientLabel);
                        }
                        break;
#endif

                    default:
                        break;
                }
                parserInited = true;
            }
        }

        if (logFileStream != null)
        {
            try { logFileStream.FlushAsync(); }
            catch (System.InvalidOperationException) { }
        }
    }

    void Log(string msg)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        if (!sceneName.Equals("Scene0"))
        {
            if (logFileStream == null)
            {
                var fname = string.Format(sceneName + "_NLUModule.log");

                FileStream fs = new FileStream(fname, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                logFileStream = new StreamWriter(fs);

                //var fname = string.Format(sceneName + "_NLUModule.log");

                //logFileStream = File.AppendText(fname);
                //Debug.Log("Opened log file at " + fname);
            }
            string line = string.Format("[{0}] {1}",
                System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), msg);
            Debug.Log("<color=green>" + line + "</color>");
            try
            {
                logFileStream.WriteLine(line);

            }
            catch (System.InvalidOperationException) { }
        }
        logFileStream.Flush();
    }

    public void CheckConnection(object sender, EventArgs e)
    {
#if !UNITY_WEBGL
        if (sender.GetType().IsSubclassOf(typeof(SocketConnection)))
        {
            if (((SocketConnection)sender).IsConnected())
            {
                parserConnected = true;
            }
        }
        else if (sender.GetType().IsSubclassOf(typeof(RESTClient)))
        {
            if (((RESTClient)sender).isConnected)
            {
                parserConnected = true;
            }
        }
#else
		parserConnected = true;
#endif
    }

    public void LookForNewParse(object sender, EventArgs e)
    {
#if !UNITY_WEBGL
        parsed = communicationsBridge.GrabParse();
#endif
    }

    // callback when user:speech changes
    void ParseLanguageInput(string key, DataStore.IValue value)
    {


        Log("Diana speech" + "  |  " + dianaspeech);
        
        
        
        // if standing by, ignore speech
        if (DataStore.GetBoolValue("me:standingBy"))
        {
            return;
        }
        //if (emm.getDisam())
        
        // exit if input is empty for some reason
        string input = value.ToString().Trim();
        if (string.IsNullOrEmpty(input)) return;
        input =input.ToLower();
        Log("user speech" + "  |  " + input);
        UserSpeech = input;
        speechTime = System.DateTime.Now.ToString("HH:mm:ss");
        Debug.Log(string.Format("Diana's World: Heard you was talkin' \"{0}\".", input));

        // bunch of if-else statements handling various dialogue particles
        if (input.StartsWith(@"\"))
        {
            input = input.Replace(@"\", "").Trim();

        }
        // "okay" in equivalent to posack when question answering
        //  acknowledges answer
        else if (input.StartsWith("okay"))
        {
            if (DataStore.GetBoolValue("me:question:isAnswering"))
            {
                // do posack
                SetValue("user:intent:isPosack", true, string.Empty);
                SetValue("user:intent:isPosack", false, string.Empty);
            }
        }
        // "yes" = posack
        //  may be followed by something contentful, so trim it and move on
        else if (input.StartsWith("yes"))
        {
            Posack = true;
            emm.setisprompt(true);
            // do posack
            SetValue("user:intent:isPosack", true, string.Empty);
            SetValue("user:intent:isPosack", false, string.Empty);
            SetValue("me:speech:intent", "OK, what is next?", string.Empty);

            // to save data in csv file 
            //SetValue("user:intent:object", DataStore.GetStringValue("me:intent:targetObj"), string.Empty);
            //dim.setfocusObject(DataStore.GetStringValue("me:intent:targetObj"));
            //dim.settargetObject("None");
            //dim.AddToDialogueHist();

            input = input.Replace("yes", "").Trim();
        }
        // "no" = negack
        //  may be followeed by replacement content (e.g., "no the white one")
        else if (input.StartsWith("no"))
        {
            isneg = true;
            emm.setisprompt(true);
            input = input.Replace("no", "").Trim();
            if (input != string.Empty)
            {
                // try to replace content
                Debug.Log(string.Format("Nada: replaceContent {0}.", input));
                // nada: if nevermind the replaced content and replacecontent was not empty, save the last event and replace the content
                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:replaceContent", communicationsBridge.NLParse(MapTerms(input)), string.Empty);
                }
                else
                {
                    SetValue("user:intent:replaceContent", communicationsBridge.NLParse(MapTerms(input)), string.Empty);
                }
            }
            else
            {
                SetValue("user:intent:replaceContent", string.Empty, string.Empty);
                //SetValue("user:intent:append:event", string.Empty, string.Empty);
                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                SetValue("user:intent:append:partialEvent", string.Empty, string.Empty);
            }
            // Nada: exit from disambiguation mood 
            if (emm.getDisam())
            {
                emm.setDisam(false);
                // send replaceContent to last event to execute it 
                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
                else
                {
                    SetValue("user:intent:lastEvent", string.Empty, string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }

            }

            // Nada: exit from disambiguation mood 
            if (emm.getdemdis())
            {
                emm.setdemdis(false);
                // send replaceContent to last event to execute it 
                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
                else
                {
                    SetValue("user:intent:lastEvent", string.Empty, string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }

            }

            // Nada: exit from disambiguation mood 
            if (emm.getCdisam())
            {
                emm.setCdisam(false);
                dp.setColor("");
                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
                else
                {
                    SetValue("user:intent:lastEvent", string.Empty, string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
            }
            // Nada: exit from both events when composing two events e.g., take this block and put it on the red block 
            if (emm.getcompose())
            {
                emm.setcompose(false);
                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                SetValue("user:intent:append:event", string.Empty, string.Empty);

            }

            // do negack
            SetValue("user:intent:isNegack", true, string.Empty);
            SetValue("user:intent:isNegack", false, string.Empty);
        }

        // "never mind" or "wait" serve as trigger words for replacement content
        //  trim them, and then see if anything remains that can be used as replacement content
        else if (input.StartsWith("never mind") || input.StartsWith("nevermind") || input.StartsWith("wait"))
        {
            emm.setisprompt(true);
            input = input.Replace("never mind", "").
            Replace("nevermind", "").
            Replace("wait", "").Trim();


            if (input != string.Empty)
            {

                // try to replace content
                Debug.Log(string.Format("Nada: replaceContent {0}.", input));
                // nada: if nevermind the replaced content and replacecontent was not empty, save the last event and replace the content

                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:replaceContent", communicationsBridge.NLParse(MapTerms(input)), string.Empty);

                }
                else
                {

                    SetValue("user:intent:replaceContent", communicationsBridge.NLParse(MapTerms(input)), string.Empty);
                }
            }
            else
            {
                SetValue("user:intent:replaceContent", string.Empty, string.Empty);
//               SetValue("user:intent:append:event", string.Empty, string.Empty);
                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                SetValue("user:intent:append:partialEvent", string.Empty, string.Empty);
            }
            // Nada: exit from disambiguation mood 
            if (emm.getDisam())
            {
                emm.setDisam(false);
                // send replaceContent to last event to execute it 
                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
                else
                {
                    SetValue("user:intent:lastEvent", string.Empty, string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }

            }

            // Nada: exit from disambiguation mood 
            if (emm.getdemdis())
            {
                emm.setdemdis(false);
                // send replaceContent to last event to execute it 
                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
                else
                {
                    SetValue("user:intent:lastEvent", string.Empty, string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }

            }
            // Nada: exit from disambiguation mood 
            if (emm.getCdisam())
            {
                emm.setCdisam(false);
                dp.setColor("");
                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                {
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
                else
                {
                    SetValue("user:intent:lastEvent", string.Empty, string.Empty);
                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                }
            }
            // Nada: exit from both events when composing two events e.g., take this block and put it on the red block 
            if (emm.getcompose())
            {
                emm.setcompose(false);
                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                SetValue("user:intent:append:event", string.Empty, string.Empty);

            }
            // do nevermind
            SetValue("user:intent:isNevermind", true, string.Empty);
            SetValue("user:intent:isNevermind", false, string.Empty);
            return;
        }
        // "stop", "enough", etc., interrupts a servo
        //  e.g. if the servo is started using language only
        else if (input.StartsWith("stop") || input.StartsWith("that's enough") ||
            input.StartsWith("enough"))
        {
            // Nada: exit from disambiguation mood
            if (emm.getDisam()) emm.setDisam(false);
            if (emm.getCdisam()) emm.setCdisam(false);
            // Nada: exit from disambiguation mood 
            if (emm.getdemdis())emm.setdemdis(false);

            // Nada: exit from both events when composing two events e.g., take this block and put it on the red block 
            if (emm.getcompose()) SetValue("user:intent:partialEvent", string.Empty, string.Empty);

            if (DataStore.GetStringValue("user:intent:partialEvent").Equals("stop()")) SetValue("user:intent:partialEvent", string.Empty, string.Empty);

            if (DataStore.GetBoolValue("user:intent:isServoLeft"))
            {
                SetValue("user:intent:isServoLeft", false, string.Empty);
                SetValue("me:isCheckingServo", false, string.Empty);
                SetValue("me:isInServoLoop", false, string.Empty);
            }

            if (DataStore.GetBoolValue("user:intent:isServoRight"))
            {
                SetValue("user:intent:isServoRight", false, string.Empty);
                SetValue("me:isCheckingServo", false, string.Empty);
                SetValue("me:isInServoLoop", false, string.Empty);
            }

            if (DataStore.GetBoolValue("user:intent:isServoFront"))
            {
                SetValue("user:intent:isServoFront", false, string.Empty);
                SetValue("me:isCheckingServo", false, string.Empty);
                SetValue("me:isInServoLoop", false, string.Empty);
            }

            if (DataStore.GetBoolValue("user:intent:isServoBack"))
            {
                SetValue("user:intent:isServoBack", false, string.Empty);
                SetValue("me:isCheckingServo", false, string.Empty);
                SetValue("me:isInServoLoop", false, string.Empty);
            }
            SetValue("user:intent:replaceContent", string.Empty, string.Empty);
            SetValue("user:intent:partialEvent", string.Empty, string.Empty);
            //return;
        }
        // questions
        else if (input.StartsWith("what is") || input.StartsWith("where is"))
        {
            // set question
            SetValue("user:intent:isQuestion", true, string.Empty);

            // set the question type
            if (input.StartsWith("what is"))
            {
                SetValue("me:question:type", "what", string.Empty);
            }
            else if (input.StartsWith("where is"))
            {
                SetValue("me:question:type", "where", string.Empty);
            }
            // trim the question word
            SetValue("me:question:content", input.Replace("what is", "").Replace("where is", "").Trim(), string.Empty);
        }


        // if there is nothing set in replacement content
        //  map words in the input to known vocabulary items
        if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
        {
            if (!string.IsNullOrEmpty(input))
            {
                string mapped = MapTerms(input);
                mapped2 = mapped;                        

                Debug.Log(string.Format("Diana's World: Heard you was talkin' \"{0}\".", mapped));
                Debug.Log(string.Format("1mapped to parsing' \"{0}\".", mapped));

#if !UNITY_WEBGL
                parsed = communicationsBridge.NLParse(mapped);
                Debug.Log(string.Format("2mapped to parsing' \"{0}\".", parsed));

                // plf = parsed;
                Log("Parsed speech" + "  |  " + parsed);
                //Debug.Log(string.Format("nlu plf: ",plf));

                if (parsed == null)
                {
                    SetValue("me:speech:intent", string.Format("The parser is turned off! Please ask the developer to enable the parser."), string.Empty);

                }
#endif
            }
        }
        //else
        //{
        //    SetValue("me:speech:intent", string.Format("replace content is not null"), string.Empty);
        //}
    }

    // map terms in input to known vocabulary items
    //  include synonyms and common mishears
    string MapTerms(string input)
    {

        string mapped = input;

        // do verb mapping
        if (mapped.StartsWith("pick up"))
        {
            mapped = mapped.Replace("pick up", "take");
            emm.setpick(true);
            //Debug.Log(string.Format("pick: {0}", pick));

        }
        else if (mapped.StartsWith("pick") && (mapped.EndsWith("up")))
        {
            mapped = mapped.Replace("pick", "take").Replace("up", "");
            emm.setpick(true);
            //Debug.Log(string.Format("pick: {0}", pick));

        }
        else if (mapped.StartsWith("pick") && (!mapped.Contains("up")))
        {
            mapped = mapped.Replace("pick", "take");
            emm.setpick(true);
            //Debug.Log(string.Format("pick: {0}", pick));

        }
        else if (mapped.StartsWith("grab"))
        {
            mapped = mapped.Replace("grab", "take");
        }
        else if (mapped.StartsWith("grasp"))
        {
            mapped = mapped.Replace("grasp", "take");
        }
        else if (mapped.StartsWith("let go of"))
        {
            mapped = mapped.Replace("let go of", "ungrasp");
        }
        else if (mapped.StartsWith("let go"))
        {
            mapped = mapped.Replace("let go", "ungrasp");
        }
        else if (mapped.StartsWith("drop"))
        {
            mapped = mapped.Replace("drop", "ungrasp");
        }
        else if (mapped.StartsWith("release"))
        {
            mapped = mapped.Replace("release", "ungrasp");
        }
        else if (mapped.StartsWith("leave"))
        {
            mapped = mapped.Replace("leave", "ungrasp");
        }
        else if (mapped.StartsWith("move"))
        {
            mapped = mapped.Replace("move", /*"put"*/ "slide");
        }
        else if (mapped.StartsWith("push"))
        {
            mapped = mapped.Replace("push", /*"put"*/"slide");
        }
        else if (mapped.StartsWith("pull"))
        {
            mapped = mapped.Replace("pull", /*"put"*/"slide");
        }
        else if (mapped.StartsWith("servo"))
        {
            mapped = mapped.Replace("servo", "bring");
        }
        //Nada: the user might say the following command e.g., "behind the plate" after diana disamb "take the green block"
        else if (_relations.Contains(mapped.Split()[0]))
        {
            string intentobject = emm.getambigBlock();
            //DataStore.GetStringValue("user:intent:object");
            if (intentobject.Equals("BlueBlock1"))
            {
                //intentobject = intentobject.Replace("BlueBlock1", "blue block");
                mapped = String.Concat("take the blue block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("BlueBlock2"))
            {
                mapped = String.Concat("take the blue block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("RedBlock2"))
            {
                mapped = String.Concat("take the red block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("RedBlock1"))
            {
                mapped = String.Concat("take the red block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("GreenBlock1"))
            {
                mapped = String.Concat("take the green block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("GreenBlock2"))
            {
                mapped = String.Concat("take the green block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("YellowBlock1"))
            {
                mapped = String.Concat("take the yellow block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("YellowBlock2"))
            {
                mapped = String.Concat("take the yellow block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("PinkBlock1"))
            {
                mapped = String.Concat("take the pink block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }
            else if (intentobject.Equals("PinkBlock2"))
            {
                mapped = String.Concat("take the pink block ", mapped);
                Debug.Log("Nada logging: " + intentobject + " + " + mapped);
            }

        }
        else if (mapped.StartsWith("the") || mapped.StartsWith("The"))
        {
            Debug.Log(string.Format("start with the"));

            string nextWord = mapped.Split().ToList().IndexOf("the") == mapped.Split().Length - 1 ?
            string.Empty :
            mapped.Split().ToList()[mapped.Split().ToList().IndexOf("the") + 1];
            string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "bottle", "one",
                "pink", "blue", "yellow", "red","green", "gray","white", "purple", "black", "orange" };
            if (knownNominals.Contains(nextWord))
            {
                mapped = String.Concat("take ", mapped);
            }
        }
        //Nada: the user might say the command without action e.g., "red"

        else if (objects.Contains(mapped.Split().ToList()[0])

            && !mapped.Split().ToList()[0].Equals("block")
            && mapped.Split().ToList().Count.Equals(1))
        {
            //string mapped1 = "";
            Debug.Log("one word nada: {0}}" + mapped.Split().ToList().Count);

            mapped = string.Concat("take the ", mapped);
            mapped = string.Concat(mapped, " block");

        }
        // "red block"
        else if (objects.Contains(mapped.Split().ToList()[0]) && !mapped.Split().ToList()[0].Equals("block") && !mapped.Split().ToList()[0].Equals("object") &&
            (mapped.Contains("block") || mapped.Contains("object") || mapped.Contains("one")))
        {

            mapped = String.Concat("take the ", mapped);

        }

        //Nada: the user might say the command without action e.g., "that red block in front of the yellow block"
        else if (mapped.StartsWith("that") || mapped.StartsWith("That"))
        {
            string nextWord = mapped.Split().ToList().IndexOf("that") == mapped.Split().Length - 1 ?
            string.Empty :
            mapped.Split().ToList()[mapped.Split().ToList().IndexOf("that") + 1];
            string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "bottle", "one",
                "pink", "blue", "yellow", "red","green", "gray","white", "purple", "black", "orange" };
            if (knownNominals.Contains(nextWord))
            {
                mapped = "take " + mapped;

            }
        }

        //Nada: the user might say the command without action e.g., "this red block "
        else if (mapped.StartsWith("this") || mapped.StartsWith("This"))
        {
            string nextWord = mapped.Split().ToList().IndexOf("this") == mapped.Split().Length - 1 ?
            string.Empty :
            mapped.Split().ToList()[mapped.Split().ToList().IndexOf("this") + 1];
            string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "bottle", "one",
                "pink", "blue", "yellow", "red","green", "gray","white", "purple", "black", "orange" };
            if (knownNominals.Contains(nextWord))
            {
                mapped = "take " + mapped;
            }
        }


            // do noun mapping
            if (mapped.Split().Contains("box"))
        {
            mapped = mapped.Replace("box", "block");
        }
        if (mapped.Split().Contains("object"))
        {
            mapped = mapped.Replace("object", "block");
        }
        if (mapped.Split().Contains("boxes"))
        {
            mapped = mapped.Replace("boxes", "blocks");
        }
        if (mapped.Split().Contains("cylindrical block"))
        {
            mapped = mapped.Replace("cylindrical block", "cup");
        }
        if (mapped.Split().Contains("cylindrical object"))
        {
            mapped = mapped.Replace("cylindrical object", "cup");
        }
        if (mapped.Split().Contains("rounded object"))
        {
            mapped = mapped.Replace("rounded object", "cup");
        }
        if (mapped.Split().Contains("rounded block"))
        {
            mapped = mapped.Replace("rounded block", "cup");
        }
        if (mapped.Split().Contains("cylinder"))
        {
            mapped = mapped.Replace("cylinder", "cup");
        }
        if (mapped.Split().Contains("mug"))
        {
            mapped = mapped.Replace("mug", "cup");
        }
        if (mapped.Split().Contains("mugs"))
        {
            mapped = mapped.Replace("mugs", "cups");
        }
        if (mapped.Split().Contains("glass"))
        {
            mapped = mapped.Replace("glass", "cup");
        }
        if (mapped.Split().Contains("blog"))
        {
            mapped = mapped.Replace("blog", "block");
        }
        if (mapped.Split().Contains("one"))
        {
            mapped = mapped.Replace("one", "block");
        }
        if (mapped.Split().Contains("these"))
        {
            mapped = mapped.Replace("these", "this");
        }
        if (mapped.Split().Contains("those"))
        {
            mapped = mapped.Replace("those", "that");
        }
        if (mapped.Split().Contains("their"))
        {
            mapped = mapped.Replace("their", "there");
        }
        if (mapped.Split().Contains("deer"))
        {
            mapped = mapped.Replace("deer", "there");
        }
        if (mapped.Split().Contains("they’re"))
        {
            mapped = mapped.Replace("they’re", "there");
        }
        if (mapped.Split().Contains("deep"))
        {
            mapped = mapped.Replace("deep", "this");
        }
        if (mapped.Split().Contains("weight"))
        {
            mapped = mapped.Replace("weight", "wait");
        }
        if (mapped.Split().Contains("rate"))
        {
            mapped = mapped.Replace("rate", "wait");
        }
        if (mapped.Split().Contains("lock"))
        {
            mapped = mapped.Replace("lock", "block");
        }
        if (mapped.Split().Contains("rock"))
        {
            mapped = mapped.Replace("rock", "block");
        }
        if (mapped.Split().Contains("black"))
        {
            mapped = mapped.Replace("black", "block");
        }
        if (mapped.Split().Contains("blotch"))
        {
            mapped = mapped.Replace("blotch", "block");
        }
        if (mapped.Split().Contains("coke"))
        {
            mapped = mapped.Replace("coke", "block");
        }
        if (mapped.Split().Contains("dog"))
        {
            mapped = mapped.Replace("dog", "block");
        }
        if (mapped.Split().Contains("clock"))
        {
            mapped = mapped.Replace("clock", "block");
        }
        if (mapped.Split().Contains("nice"))
        {
            mapped = mapped.Replace("nice", "knife");
        }
        if (mapped.Split().Contains("about"))
        {
            mapped = mapped.Replace("about", "above");
        }
        if (mapped.Split().Contains("play sit"))
        {
            mapped = mapped.Replace("play sit", "place it");
        }
        if (mapped.Split().Contains("light"))
        {
            mapped = mapped.Replace("light", "right");
        }
        if (mapped.Split().Contains("site"))
        {
            mapped = mapped.Replace("site", "right");
        }
        if (mapped.Split().Contains("dead"))
        {
            mapped = mapped.Replace("dead", "that");
        }
        if (mapped.Split().Contains("limit"))
        {
            mapped = mapped.Replace("limit", "move it");
        }
        if (mapped.Split().Contains("your're"))
        {
            mapped = mapped.Replace("your're", "your");
        }
        if (mapped.Split().Contains("grave"))
        {
            mapped = mapped.Replace("grave", "gray");
        }
        //if (mapped.Split().Contains("your"))
        //{
        //    mapped = mapped.Replace("your", "");
        //}
        if (mapped.Split().Contains("hand"))
        {
            mapped = mapped.Replace("hand", "");
        }
        if (mapped.Split().Contains("paint"))
        {
            mapped = mapped.Replace("paint", "pink");
        }
        if (mapped.Split().Contains("spring"))
        {
            mapped = mapped.Replace("spring", "pink");
        }
        if (mapped.Split().Contains("eat"))
        {
            mapped = mapped.Replace("eat", "it");
        }
        //Rasp, grass, graph, craf
        if (mapped.Split().Contains("rasp"))
        {
            mapped = mapped.Replace("rasp", "grasp");
        }
        if (mapped.Split().Contains("grass"))
        {
            mapped = mapped.Replace("grass", "grasp");
        }
        if (mapped.Split().Contains("graph"))
        {
            mapped = mapped.Replace("graph", "grasp");
        }
        if (mapped.Split().Contains("craf"))
        {
            mapped = mapped.Replace("craf", "grasp");
        }
        if (mapped.Split().Contains("class"))
        {
            mapped = mapped.Replace("class", "cup");
        }
        if (mapped.Split().Contains("cream"))
        {
            mapped = mapped.Replace("cream", "green");
        }
        if (mapped.Split().Contains("but"))
        {
            mapped = mapped.Replace("but", "put");
        }
        if (mapped.Split().Contains("pillow"))
        {
            mapped = mapped.Replace("pillow", "below");
        }
        if (mapped.Split().Contains("problem"))
        {
            mapped = mapped.Replace("problem", "brown");
        }
        if (mapped.Split().Contains("bride"))
        {
            mapped = mapped.Replace("bride", "white");
        }
        if (mapped.Split().Contains("hold"))
        {
            mapped = mapped.Replace("hold", "grasp");
        }
        if (mapped.Split().Contains("place"))
        {
            mapped = mapped.Replace("place", "put");
        }
        if (mapped.Contains("to the back of") && mapped.Contains("of"))
        {
            mapped = mapped.Replace("to the back of", "behind");
        }
        if (mapped.Contains("to back of") && mapped.Contains("of"))
        {
            mapped = mapped.Replace("to back of", "behind");
        }
        if (mapped.Contains("in the back of") && mapped.Contains("of"))
        {
            mapped = mapped.Replace("in the back of", "behind");
        }
        if (mapped.Contains("in back of") && mapped.Contains("of"))
        {
            mapped = mapped.Replace("in back of", "behind");
        }
        if (mapped.Contains("back of") && mapped.Contains("of"))
        {
            mapped = mapped.Replace("back of", "behind");
        }

        if (mapped.Contains("to the back") && !mapped.Contains("of") && !mapped.Contains("backmost"))
        {
            mapped = mapped.Replace("to the back", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);

        }
        if (mapped.Contains("to the front") && !mapped.Contains("of") && !mapped.Contains("frontmost"))
        {
            mapped = mapped.Replace("to the front", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);

        }
        if (mapped.Contains("to back") && !mapped.Contains("of") && !mapped.Contains("backmost"))
        {
            mapped = mapped.Replace("to back", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);
        }
        if (mapped.Contains("back") && !mapped.Contains("of") && !mapped.Contains("in") && !mapped.Contains("to") && !mapped.Contains("backmost"))
        {
            mapped = mapped.Replace("back", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please! "), string.Empty);
        }

        if (mapped.Contains("to front") && !mapped.Contains("of") && !mapped.Contains("frontmost"))
        {
            mapped = mapped.Replace("to front", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);
        }
        if (mapped.Contains("front") && !mapped.Contains("of") && !mapped.Contains("in") && !mapped.Contains("to") && !mapped.Contains("frontmost"))
        {
            mapped = mapped.Replace("front", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please! "), string.Empty);
        }
        if (mapped.Contains("to the right") && !mapped.Contains("of") && !mapped.Contains("slide") && !mapped.Contains("rightmost"))
        {
            mapped = mapped.Replace("to the right", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);
        }
        if (mapped.Contains("to right") && !mapped.Contains("of") && !mapped.Contains("rightmost"))
        {
            mapped = mapped.Replace("to right", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);
        }
        if (mapped.Contains("right") && !mapped.Contains("of") && !mapped.Contains("in") && !mapped.Contains("to") && !mapped.Contains("rightmost"))
        {
            mapped = mapped.Replace("right", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please! "), string.Empty);
        }
        if (mapped.Contains("to the left") && !mapped.Contains("of") && !mapped.Contains("slide") && !mapped.Contains("leftmost"))
        {
            mapped = mapped.Replace("to the left", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);
            Debug.Log(string.Format(" to the left replacement "));

        }
        if (mapped.Contains("left") && !mapped.Contains("of") && !mapped.Contains("in") && !mapped.Contains("to"))
        {
            mapped = mapped.Replace("left", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please! "), string.Empty);
        }
        if (mapped.Contains("to left") && !mapped.Contains("of") && !mapped.Contains("leftmost"))
        {
            mapped = mapped.Replace("to left", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);
        }
        if (mapped.Contains("on top of"))
        {
            mapped = mapped.Replace("on top of", "on");
        }
        if (mapped.Contains("on the top of"))
        {
            mapped = mapped.Replace("on the top of", "on");
        }
        if (mapped.Contains("above"))
        {
            mapped = mapped.Replace("above", "on");
        }
        if (mapped.Contains("next to"))
        {
            mapped = mapped.Replace("next to", "beside");
        }

        // e.g., move it to the red block
        if (mapped.Split().Contains("to") && !mapped.Split().Contains("right") &&
            !mapped.Split().Contains("left") /*&& (mapped.Split().Contains("move")
            || mapped.Split().Contains("slide"))*/)
        {
            if (mapped.Contains("slide") ) {
                mapped = mapped.Replace("slide", "put").Replace("to", "beside");
            }
            else if (mapped.Contains("move"))
            {
                mapped = mapped.Replace("move", "put").Replace("to", "beside");
            }
            //else if (mapped.Contains("move") && mapped.Contains("back"))
            //{
            //    mapped = mapped.Replace("move", "put");
            //}
            else if (mapped.Contains("put") )
            {
                mapped = mapped.Replace("to", "beside");
            }
        }
        // e.g., take the red block here/there
        if (trans_no_goal.Contains(mapped.Split()[0]) && mapped.Split().Contains("here"))
        {
            mapped = mapped.Replace("here", "").Replace("the", "this");
        }
        if (trans_no_goal.Contains(mapped.Split()[0]) && mapped.Split().Contains("there"))
        {
            mapped = mapped.Replace("there", "").Replace("the", "that");
        }

        // get rid of "on/to the" before PP
        // on the top of, on top of -> on
        // to back of, to the back of -> behind
        //if ((mapped.Contains("to the left")) || (mapped.Contains("to the right"))) {
        //    mapped = mapped.Replace("to the", "");
        //}


        if (mapped.Contains("in the right") && !mapped.Contains("rightmost"))
        {
            mapped = mapped.Replace("in the right", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);
        }
        if (mapped.Contains("in the left") && !mapped.Contains("leftmost"))
        {
            mapped = mapped.Replace("in the left", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);

        }
        if (mapped.Contains("in the back") && !mapped.Contains("backmost"))
        {
            mapped = mapped.Replace("in the back", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);

        }

        if (mapped.Contains("in the front") && !mapped.Contains("frontmost"))
        {
            mapped = mapped.Replace("in the front", "");
            SetValue("me:speech:intent", string.Format("Locate the position you prefer please!"), string.Empty);

        }


        // insert "one" after "this"/"that" if not already followed by noun
        if (mapped.Split().Contains("this"))
        {
            string nextWord = mapped.Split().ToList().IndexOf("this") == mapped.Split().Length - 1 ?
                string.Empty :
                mapped.Split().ToList()[mapped.Split().ToList().IndexOf("this") + 1];
            // Nada: one dropped
            string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "bottle", "red", "is", "pink", "green", "blue", "yellow" };
            if (!knownNominals.Contains(nextWord) || string.IsNullOrEmpty(nextWord))
            {
                // mapped = mapped.Replace("this", "this one");
                // replaced with block; when there are duplicates of blocks the disambiguation function does not work with one word
                mapped = mapped.Replace("this", "this block");
                //    if (!string.IsNullOrEmpty(nextWord))
                //        mapped = mapped.Replace(nextWord, "");
            }
        }
        else if (mapped.Split().Contains("this"))
        {
            string nextWord = mapped.Split().Length == 1 ?
                string.Empty :
                mapped.Split().ToList()[mapped.Split().ToList().IndexOf("this") + 1];
            // Nada: one dropped
            string[] knownNominals = new string[] { "block", "cup", "knife", "plate", "bottle", "red", "is", "yellow", "pink", "blue", "white", "purple", "green", "black", "gray" };
            if (!knownNominals.Contains(nextWord))
            {
                mapped = mapped.Replace("this", "this block");
                //if (!string.IsNullOrEmpty(nextWord))
                //    mapped = mapped.Replace(nextWord, "");
            }
        }

        if (mapped.Split().Contains("that"))
        {
            string nextWord = mapped.Split().ToList().IndexOf("that") == mapped.Split().Length - 1 ?
                string.Empty :
                mapped.Split().ToList()[mapped.Split().ToList().IndexOf("that") + 1];
            if (!knownNominals.Contains(nextWord))
            {
                mapped = mapped.Replace("that", "that block");
            }
        }
        else if (mapped.Split().Contains("that"))
        {
            string nextWord = mapped.Split().Length == 1 ?
                string.Empty :
                mapped.Split().ToList()[mapped.Split().ToList().IndexOf("this") + 1];
            if (!knownNominals.Contains(nextWord))
            {
                mapped = mapped.Replace("that", "that block");
            }
        }

        Debug.Log("2 Nada logging: " + mapped);
        return mapped;
    }

    // This timer and its event were implemented to fix
    // an issue where if a command was given to Diana
    // prior to engaging and then repeated after engaging,
    // Diana would not act on it since the user:speech 
    // key hadn't changed.
    void SetTimer()
    {
        // Create a timer with a three second interval.
        timer = new System.Timers.Timer(1000);
        // Link the Elapsed event for the timer. 
        timer.Elapsed += new ElapsedEventHandler(UpdateUserSpeech);
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    void UpdateUserSpeech(object source, ElapsedEventArgs e)
    {
        // resets the user:speech key so commands to Diana can be repeated 
        // (after the three second window) as needed.
        SetValue("user:speech", String.Empty, "Clear user:speech for repeats");
    }

    void OnNewParse(string val)
    {
        Debug.Log(string.Format("New parse: {0}", val));

        if (!string.IsNullOrEmpty(val))
        {
            SetValue("user:intent:partialEvent", val, string.Empty);
        }
    }
}
