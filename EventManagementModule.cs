/*
This script interfaces with the VoxSim event manager

Reads:      user:isInteracting (BoolValue, whether or not the user is currently
                engaged in an interactive task with Diana)
            user:intent:event (StringValue, a predicate-argument representation
                of an event, with NO outstanding variables)
            user:intent:object (StringValue, the name of the object that the 
                user wants Diana to direct her attention or action to, i.e., the
                theme of any subsequent action)
            user:intent:action (StringValue, a predicate-argument representation
                of an events with no variables filled - may contain "hints",
                e.g., directions relative to which an argument should be
                interpreted when assigned)
            user:intent:isServoLeft (BoolValue, whether or not the user is
                gesturing "servo left")
            user:intent:isServoRight (BoolValue, whether or not the user is
                gesturing "servo right")
            user:intent:isServoFront (BoolValue, whether or not the user is
                gesturing "servo front" -- we don't really have this gesture)
            user:intent:isServoBack (BoolValue, whether or not the user is
                gesturing "servo back")               
            user:intent:location (Vector3Value, a location to which the user
                wants Diana to direct her attention or action, e.g., the space
                where she should move a theme object)
            user:intent:partialEvent (StringValue, a predicate-argument
                representation of an event, but with outstanding variables -
                variables are indicated by numbers in curly braces, e.g., {0},
                the number indicates typing information; {0} = object,
                {1} = location)
            user:intent:lastEvent (StringValue, a predicate-argument
                representation of the last event Diana completed)
            user:intent:append:event (StringValue, same as user:intent:event but
                to be added to the back of the event manager event list instead
                of the front)
            user:intent:append:action (StringValue, same as user:intent:action
                but to be added to the back of the event manager event list
                instead of the front)
            user:intent:append:partialEvent (StringValue, same as
                user:intent:partialEvent but to be added to the back of the
                event manager event list instead of the front)
            user:hands:left (StringValue, gesture being made by user's left hand
                - here used only in the context of one-shot learning new 
                gesture semantics)
            user:hands:right (StringValue, gesture being made by user's right
                hand - here used only in the context of one-shot learning new 
                gesture semantics)
            user:intent:replaceContent (StringValue, optional replacement
                content with an undo)                
                  
            me:lastTheme (StringValue, the theme of the most recent action
                Diana took)
            me:oneshot:newGesture (StringValue, the label of a newly-learned
                gesture)
            me:affordances:isLearning (BoolValue, whether or not Diana is
                learning a new affordance, e.g., a new gesture + object + grasp
                pose)               
            me:affordances:forget (BoolValue, whether or not Diana should forget
                the affordances she learned)
            me:actual:action (StringValue, Diana's completed action -- set to 
                value of me:intent:action when that action is complete, NOT to
                be confused with user:intent:action)               
            me:isCheckingServo (BoolValue, whether or not Diana is looking for
                another iteration on a servo gesture)
            me:isUndoing (BoolValue, whether or not Diana is currently undoing
                her last action)
                       
Writes:     user:intent:event (StringValue, a predicate-argument representation
                of an event, with NO outstanding variables)           
            user:intent:object (StringValue, the name of the object that the 
                user wants Diana to direct her attention or action to, i.e., the
                theme of any subsequent action)
            user:intent:isServoLeft (BoolValue, whether or not the user is
                gesturing "servo left")
            user:intent:isServoRight (BoolValue, whether or not the user is
                gesturing "servo right")
            user:intent:isServoFront (BoolValue, whether or not the user is
                gesturing "servo front" -- we don't really have this gesture)
            user:intent:isServoBack (BoolValue, whether or not the user is
                gesturing "servo back")
            user:intent:partialEvent (StringValue, a predicate-argument
                representation of an event, but with outstanding variables -
                variables are indicated by numbers in curly braces, e.g., {0},
                the number indicates typing information; {0} = object,
                {1} = location)
            user:intent:lastEvent (StringValue, a predicate-argument
                representation of the last event Diana completed)
            user:intent:append:action (StringValue, a predicate-argument
                representation of an events with no variables filled - may
                contain "hints", e.g., directions relative to which an argument
                should be interpreted when assigned; to be added to the back of
                the event manager event list instead of the front)
            user:intent:append:partialEvent (StringValue, same as
                user:intent:partialEvent but to be added to the back of the
                event manager event list instead of the front)
            user:intent:append:event (StringValue, same as user:intent:event but
                to be added to the back of the event manager event list instead
                of the front)
            user:intent:isQuestion (BoolValue, whether or not the user asked a
                question)

            me:actual:eventCompleted (StringValue, the predicate-argument
                representation of the last event Diana completed)
            me:isAttendingPointing (BoolValue, whether or not Diana is attending
                to the user's deixis)
            me:speech:intent (StringValue, Diana's speech output)
            me:intent:lookAt (StringValue, the object (or user) Diana looks at)
            me:intent:targetObj (StringValue, name of the object that is theme
                of Diana's attention/action)
            me:intent:target (Vector3Value, the location that is the target of
                Diana's attention/action)
            me:intent:action (StringValue, Diana's current intended action -- 
                NOT to be confused with user:intent:action; this is not a
                predicate-argument representation)               
            me:emotion (StringValue, Diana's emotion)
            me:lastTheme (StringValue, the theme of the most recent action
                Diana took)
            me:lastThemePos (Vector3Value, the previous location of
                me:lastTheme)
            me:isCheckingServo (BoolValue, whether or not Diana is looking for
                another iteration on a servo gesture)
            me:oneshot:newGesture (StringValue, the label of a newly-learned
                gesture)
            me:affordances:isLearning (BoolValue, whether or not Diana is
                learning a new affordance, e.g., a new gesture + object + grasp
                pose)               
            me:affordances:forget (BoolValue, whether or not Diana should forget
                the affordances she learned)
            me:question:isAnswering (BoolValue, whether or not Diana is
                currently answering a question)
*/

using UnityEngine;
using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Semantics.LocationSpec;
using RootMotion.FinalIK;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;
using UnitySDK.WebSocketSharp;
using ThirdParty.iOS4Unity;
using Newtonsoft.Json.Linq;
using Google.Protobuf.Collections;
using VoxSimPlatform.SpatialReasoning.QSR;
using Crosstales;
using VoxSimPlatform.Episteme;
//using UnityEngine.Monetization;
using static RootMotion.FinalIK.IKSolverVR;
using static UnityEngine.GraphicsBuffer;
using System.IO;
using Google.Api;
using UnityEngine.SceneManagement;

//using System.Drawing;
#if !UNITY_WEBGL
using VoxSimPlatform.Network;

using VoxSimPlatform.NLU;
using System.Text;
using VoxSimPlatform.Interaction;
//using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning;
//using VoxSimPlatform.Vox;
#endif

public class EventManagementModule : ModuleBase
{
    public EventManager eventManager;
    public DialogueHistory dh;
    public DianaParser dp;
    public Predicates pred;
    //public Logging log;

    public event EventHandler ObjectSelected;


    public void OnObjectSelected(object sender, EventArgs e)
    {
        if (ObjectSelected != null)
        {
            ObjectSelected(this, e);
        }
    }
    public VoxMLLibrary voxmlLibrary;
    // Nada: to track relations for relational REs
    GameObject behaviorController;
    RelationTracker relationTracker;

    Stack<string> ExecutedEvents;



    //Stack<string> objHist_stack;

    //public static List<string> objlist;
    //public List<string> getobjlist() { return objlist; }
    //public void setobjlist(List<string> value) { objlist = value; }


    //Stack<string> objtemp;
    //Stack<string> evtemp;
    /// <summary>
    /// Reference to the manipulable objects in the scene.
    /// Only these will be searched when an object is referred by name.
    /// </summary>
    public Transform grabbableBlocks;

    /// <summary>
    /// Reference to the default surface in the scene.
    /// (i.e., the table)
    /// </summary>
	public GameObject demoSurface;

    /// <summary>
    /// Reference to Diana's hand.
    /// </summary>
    //public Transform hand;

    public float servoSpeed = 0.05f;

    // Nada: used to delimit multiple events appended to the event list
    string multiAppendDelimiter = "+";
    static string RE_Category;
    public string getRE_Category() { return RE_Category; }
    public void setRE_Category(string value) { RE_Category = value; }

    bool relationalRE = false, LogrelationalRE;
    private bool HistoricalRE = false;

    static bool isHistorical;
    public bool getisHistorical() { return isHistorical; }
    public void setisHistorical(bool value) { isHistorical = value; }

    public bool pick;
    public bool Getpick() { return pick; }
    public void setpick(bool value) { pick = value; }

    static bool demdis;
    public bool getdemdis() { return demdis; }
    public void setdemdis(bool value) { demdis = value; }

    bool transit = false;
    bool exis;

    public string partialEvent1 { get; private set; }
    public string partialEvent2 { get; private set; }

    string relObj, relEvent;

    //string executedEvt = "";

    static bool isprompt = false;
    public bool getisprompt() { return isprompt; }
    public void setisprompt(bool value) { isprompt = value; }

    static bool compose;
    public bool getcompose() { return compose; }
    public void setcompose(bool value) { compose = value; }

    static bool dis;
    public bool getDisam() { return dis; }
    public void setDisam(bool value) { dis = value; }

    static bool cdis;
    public bool getCdisam() { return cdis; }
    public void setCdisam(bool value) { cdis = value; }

    static string ambigEvent;
    public string getambigEvent() { return ambigEvent; }
    public void setambigEvent(string value) { ambigEvent = value; }

    static string ambigBlock;
    public string getambigBlock() { return ambigBlock; }
    public void setambigBlock(string value) { ambigBlock = value; }

    //FileStream fs;
    public StreamWriter logFileStream;
    public StreamWriter logFileunexe;
    public StreamWriter logFileunexe2;

    public DialogueInteractionModule dim;
    public NLUModule nlu;
    MREG M;
    // corresponding predicate to each intent direction name
    //Dictionary<string, string> directionPreds = new Dictionary<string, string>()
    //{
    //    { "left", "left" },
    //    { "right", "right" },
    //    { "front", "in_front" },
    //    { "back", "behind" }
    //};

    // how each intent direction name is referred to in speech
    //Dictionary<string, string> directionLabels = new Dictionary<string, string>()
    //{
    //    { "left", "left of" },
    //    { "right", "right of" },
    //    { "front", "in front of" },
    //    { "back", "behind" }
    //};

    // corresponding world-space vector to each intent direction name
    Dictionary<string, Vector3> directionVectors = new Dictionary<string, Vector3>()
    {
        { "left", Vector3.left },
        { "right", Vector3.right },
        { "front", Vector3.forward },
        { "back", Vector3.back },
        { "up", Vector3.up },
        { "down", Vector3.down }
    };

    // the opposite of each direction
    Dictionary<string, string> oppositeDir = new Dictionary<string, string>()
    {
        { "left", "right" },
        { "right", "left" },
        { "front", "back" },
        { "back", "front" },
        { "up", "down" },
        { "down", "up" }
    };

    //Dictionary<string, string> relativeDir = new Dictionary<string, string>()
    //{
    //    { "left", "left" },
    //    { "right", "right" },
    //    { "front", "back" },
    //    { "back", "front" },
    //    { "up", "up" },
    //    { "down", "down" }
    //};

    Dictionary<string, string> learnableInstructions = new Dictionary<string, string>()
    {
        {"gesture 1", string.Empty},
        {"gesture 2", string.Empty},
        {"gesture 3", string.Empty},
        {"gesture 4", string.Empty},
        {"gesture 5", string.Empty},
        {"gesture 6", string.Empty}
    };

    // Nada: To discriminate relational REs from Historical REs
    List<string> _relations = new List<string>(new[] {
                "touching_adj","in_adj",
                "on_adj",
                "atop_adj",
                "port_adj",
                "starboard_adj",
                "afore_adj",
                "astern_adj",
                "at_adj",
                "behind_adj",
                "in_front_adj",
                "in_front",
                "beside_adj",
                "near_adj",
                "left_adj",
                "right_adj",
                "center_adj",
                "edge_adj",
                "under_adj",
                "underneath_adj",
                "below_adj",
                "against_adj",
                "support_adj",
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

    //Nada: to replace beside relation
    List<string> bes_synonyms = new List<string>(new[] {
                "touching",
                "support",
                "left",
                "right"
            });
    //Nada: to replace on relation
    List<string> on_synonyms = new List<string>(new[] {
                "touching",
                "support"
            });
    List<string> under_synonyms = new List<string>(new[] {
                "touching",
                "support",
                "under"
            });



    // Nada: actions that can be understood by Diana
    private static List<string> _actions = new List<string>(new[] {  "bring", "servo",
                "put", "slide", "push", "take", "lift", "pick up", "pick", "grab", "grasp",
                "pull", "move", "drop", "release", "ungrasp", "let go", "shift", "scoot","that", "this","the",
        "brought", "servoed","put", "slid", "pushed", "took", "lifted", "picked up", "picked", "grabed", "grasped",
                "pulled", "moved", "dropped", "released", "ungrasped", "let went", "shifted", "scooted","hi","stop" });

    // Nada: objects that can be understood by Diana
    private static List<string> objects = new List<string>(new[] { "orange_block","yellow_block", "green_block",
        "pink_block", "blue_block", "red_block", "purple_block", "black_block", "orange block","white block", "gray_block",
        "orange_block", "yellow block", "green block","pink block", "blue block", "red block", "purple block",
        "black block", "white block", "gray block",
        "orange block", "plate",  "cup", "table", "block", "one" });

    // Nada: colors that can be understood by Diana
    private static List<string> colors = new List<string>(new[] { "yellow", "green",
        "pink", "blue", "red", "purple", "black", "white", "gray","orange"});

    // Nada: objectVars that can be understood by Diana
    private List<string> _objectVars = new List<string>(new[] {
                "{0}","o"
            });
    // Nada: anaphorVars that can be understood by Diana
    private List<string> _anaphorVars = new List<string>(new[] {
                "{2}","a"
            });
    // Nada: demonstratives that can be understood by Diana
    private List<string> _demoVars = new List<string>(new[] {
                "{1}","l"
            });
    // Nada: dublicated blocks
    private static List<string> dublicated_blocks1 = new List<string>(new[] { "RedBlock2", "RedBlock1", "RedBlock", "BlueBlock", "BlueBlock1", "BlueBlock2",
        "YellowBlock", "YellowBlock1", "YellowBlock2", "GreenBlock", "GreenBlock1", "GreenBlock2", "PinkBlock1", "PinkBlock2","PinkBlock",
        "cup", "plate", "Cup", "Plate" });

    //private static List<string> dublicated_blocks = new List<string>(new[] {  "YellowBlock", "GreenBlock", "GrayBlock", "RedBlock", "BlackBlock", "WhiteBlock", "Cup", "Plate"});

    private static List<String> trans_no_goal = new List<string>(new[] { "bring", "select", "pick up", "lift", "grab", "grasp", "take", "let go of", "ungrasp", "drop", "release", "find", "go to" });
    private static List<String> trans_goal = new List<string>(new[] { "move", "put", "push", "pull", "slide", "place", "shift", "scoot", "servo", "bring" });
    //private object eventHist_stack;
    //private object objHist_stack;

    public static string logeventStr;
    public string getlogeventStr() { return logeventStr; }
    public void setlogeventStr(string value) { logeventStr = value; }

    public string spaceRelations { get; private set; }
    public bool relfound { get; private set; }
    public string Dianafocustime { get; private set; }
    public string speech { get; private set; }
    public string parsing { get; private set; }
    public string eventplf1 { get; private set; }
    public string eventplf2 { get; private set; }
    public string Focus1 { get; private set; }
    public string Focus2 { get; private set; }
    public string targetobject { get; private set; }
    public string promptCompletionTime { get; private set; }

    static bool isunknown;

    public bool getisunknown() { return isunknown; }
    public void setisunknown(bool value) { isunknown = value; }

    // Start is called before the first frame update
    void Start()
    {

        base.Start();
        // Nada: added for relational REs Understanding 
        behaviorController = GameObject.Find("BehaviorController");
        // Nada: added for relational REs Understanding 
        relationTracker = behaviorController.GetComponent<RelationTracker>();
        //DialogueHistory object
        dh = new DialogueHistory();
        dp = new DianaParser();
        pred = new Predicates();
        dim = new DialogueInteractionModule();
        nlu = new NLUModule();
        M = new MREG();
        DataStore.Subscribe("user:intent:event", PromptEvent);
        DataStore.Subscribe("user:intent:object", TryEventComposition);
        DataStore.Subscribe("user:intent:action", TryEventComposition);
        DataStore.Subscribe("user:intent:location", TryEventComposition);
        DataStore.Subscribe("user:intent:partialEvent", TryEventComposition);
        DataStore.Subscribe("user:intent:append:event", AppendEvent);
        DataStore.Subscribe("user:intent:append:action", TryEventComposition);
        DataStore.Subscribe("user:intent:append:partialEvent", TryEventComposition);
        DataStore.Subscribe("user:hands:left", CheckCustomGesture);
        DataStore.Subscribe("user:hands:right", CheckCustomGesture);
        DataStore.Subscribe("me:oneshot:newGesture", AssignNewGesture);
        DataStore.Subscribe("me:affordances:isLearning", AppendEvent);
        DataStore.Subscribe("me:affordances:forget", ForgetLearnedAffordances);
        DataStore.Subscribe("me:actual:action", CheckEventExecutionStatus);
        DataStore.Subscribe("me:isCheckingServo", CheckServoStatus);

        eventManager.EntityReferenced += EntityReferenced;
        eventManager.NonexistentEntityError += NonexistentReferent;
        eventManager.InvalidPositionError += InvalidPosition;
        eventManager.DisambiguationError += AskForDisambiguation;
        eventManager.EventComplete += EventCompleted;
        eventManager.ResolveDiscrepanciesComplete += PhysicsDiscrepanciesResolved;
        eventManager.QueueEmpty += QueueEmpty;

        SatisfactionTest.OnUnhandledArgument += TryPropWordHandling;
        eventManager.OnUnhandledArgument += TryPropWordHandling;
        SatisfactionTest.OnObjectMatchingConstraint += RemoveInaccessibleObjects;
        eventManager.OnObjectMatchingConstraint += RemoveInaccessibleObjects;
        demoSurface = GlobalHelper.GetMostImmediateParentVoxeme(demoSurface);

    }


    // Update is called once per frame
    void Update()
    {
        if (logFileStream != null)
        {
            try { logFileStream.FlushAsync(); }
            catch (System.InvalidOperationException)
            {
                Debug.Log("InvalidOperationException ");
            }
        }
        if (logFileunexe != null)
        {
            try { logFileunexe.FlushAsync(); }
            catch (System.InvalidOperationException)
            {
                Debug.Log("InvalidOperationException ");
            }
        }



    }
    // Nada: To log the user's related data to log file
    void Log(string msg)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        if (!sceneName.Equals("Scene0"))
        {
            if (logFileStream == null)
            {
                var fname = string.Format(sceneName + "_EventManagment.log");

                FileStream fs = new FileStream(fname, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                logFileStream = new StreamWriter(fs);
                //using (var file = File.Open(path, FileMode.Append, FileAccess.Write))
                //var fname = string.Format(sceneName + "_EventManagment.log");
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
            logFileStream.Flush();
        }
    }

    // log unexecuted prompts
    public void logTocsv(string UserSpeech, string DianaResponse, string PLF, string Relations, string Configurations)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (!sceneName.Equals("Scene0"))
        {
            // logging data to csv file
            if (logFileunexe == null)
            {
                var fname = string.Format(sceneName + "_UnexecutedPrompts.csv");

                FileStream fs = new FileStream(fname, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                logFileunexe = new StreamWriter(fs);
                logFileunexe.WriteLineAsync("Date, PromptTime, UserSpeech, DianaResponse, PLF, Relations, Configurations");
            }
            try
            {
                logFileunexe.WriteLineAsync(System.DateTime.Now.ToString("yyyy:MM:dd") + "," + System.DateTime.Now.ToString("HH:mm:ss") + "," + UserSpeech + "," + DianaResponse + "," + PLF.Replace(",", ";") + "," + Relations + "," + Configurations);
            }
            catch (System.InvalidOperationException) { }
            //logFileunexe.Flush();
        }
    }

    // prompt an event to execute immediately
    void PromptEvent(string key, DataStore.IValue value)
    {
        // only works in an interaction
        if (DataStore.GetBoolValue("user:isInteracting"))
        {
            string eventStr = value.ToString().Trim();

            if (string.IsNullOrEmpty(eventStr)) return;
            if (eventStr.Contains("grasp") && pick == true)
            {
                eventStr = eventStr.Replace("grasp", "lift");
                Debug.Log("++ replacing event: " + eventStr);
                pick = false;
            }


            if ((!trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && (eventStr.Contains("cup") || eventStr.Contains("plate"))) ||
                (trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && (eventStr.Split(',')[0].Contains("cup") || eventStr.Split(',')[0].Contains("plate")))
                || (trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && ((eventStr.Split(',')[1].Contains("on") && !eventStr.Split(',')[1].Contains("front")
                && eventStr.Split(',')[1].Contains("cup")) || (eventStr.Split(',')[1].Contains("on") && !eventStr.Split(',')[1].Contains("front") && eventStr.Split(',')[1].Contains("plate")))))
            {

                if (eventStr.Contains("on"))
                {

                    SetValue("me:speech:intent", string.Format("Sorry. I cannot put anything on landmarks. Please select a block only as a target!"), string.Empty);
                    //UserSpeech, DianaResponse, PLF, Relations, Configurations
                    //SetValue("me:intent:action", string.Empty, string.Empty);
                    //SetValue("me:intent:lookAt", string.Empty, string.Empty);
                    //SetValue("me:intent:targetObj", string.Empty, string.Empty);
                    //logTocsv(nlu.GetUserSpeech(), "Sorry. I cannot put anything on landmarks. Please select a block only as a target!", dp.getform(), RelationsLog(), dim.Configurations());


                    return;

                }
                else
                {
                    SetValue("me:speech:intent", string.Format("This is a landmark. Please select a block only!"), string.Empty);
                    //SetValue("me:intent:action", string.Empty, string.Empty);
                    //SetValue("me:intent:lookAt", string.Empty, string.Empty);
                    //SetValue("me:intent:targetObj", string.Empty, string.Empty);
                    //logTocsv(nlu.GetUserSpeech(), "This is a landmark. Please select a block only!", dp.getform(), RelationsLog(), dim.Configurations());


                    return;
                }

            }

            if (eventStr.Contains(","))
            {
                transit = true;
                RE_Category = "Transit_Attributive";
            }
            else
            {
                transit = false;
            }
            // clear the "completed event" key and made
            //  Diana not attend to pointing while the event is being executed

            SetValue("me:actual:eventCompleted", string.Empty, string.Empty);
            SetValue("me:isAttendingPointing", false, string.Empty);

            // if not undoing
            if (!DataStore.GetBoolValue("me:isUndoing"))
            {
                // and if the event predicate is a program
                // as opposed to an attribute since we can also use PromptEvent
                // to search for NPs
                if (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(eventStr), voxmlLibrary) == "programs")
                {
                    // store user:intent:event in user:intent:lastEvent in case Diana does something wrong
                    Debug.Log(string.Format("Setting last event {0}", DataStore.GetStringValue("user:intent:event")));
                    SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:event"),
                     string.Format("Store user:intent:event ({0}) in user:intent:lastEvent in case Diana did something wrong",
                      DataStore.GetStringValue("user:intent:event")));
                }
            }

            try
            {
                // if undoing, clear the event list
                if (DataStore.GetBoolValue("me:isUndoing"))
                {
                    Debug.Log("Clearing events");
                    eventManager.ClearEvents();
                }

                // insert the event string into the event manager's event list at the proper position
                Debug.Log(string.Format("Prompting event {0}", eventStr));
                if (DataStore.GetBoolValue("me:isUndoing") && dis && exis)
                {
                    SetValue("me:speech:intent", "OK.", string.Empty);
                    Debug.Log(string.Format("ok: undo:" + eventStr));
                }
                else if (!dis && !exis)
                {
                    SetValue("me:speech:intent", "OK.", string.Empty);
                    Debug.Log(string.Format("ok: dis && exis:" + eventStr));
                }

                eventManager.InsertEvent("", 0);
                eventManager.InsertEvent(eventStr, 1);
                // for logging purposes: to avoid logging the event more that once 
                //if (!executedEvt.Equals(eventStr))
                //{
                promptCompletionTime = System.DateTime.Now.ToString("HH:mm:ss");

                string last_event = GlobalHelper.GetTopPredicate(eventStr);
                string last_object = DataStore.GetStringValue("user:intent:object");
                isprompt = true;

                //promptCompletionTime = System.DateTime.Now.ToString("HH:mm:ss");

                logeventStr = eventStr;
                // or the condition is true, save all the scene relations and select the last one
                Debug.Log("Nada: isprompt: " + eventStr + " " + isprompt.ToString());
                if (!string.IsNullOrEmpty(dp.getColor())) { dp.setColor(null); }




                // Nada: reset Nonpointing 
                if (dim.getpointing()) { dim.setpointing(false); }
                //if (cdis == true) cdis=false;
                //if (dis == true) dis=false;

                if (ExecutedEvents == null || !ExecutedEvents.Peek().Equals(eventStr))
                {
                    Log("user:intent:event" + "  |  " + eventStr.ToString());
                    Log("-------------------------------------------------------------------------------");
                    if (eventManager)
                        spaceRelations = RelationsLog();
                    Log("Relations: " + spaceRelations);
                    Log("------------------------------------------------------------------------------");

                }
                Log("Configurations: " + dim.Configurations());
                Log("------------------------------------------------------------------------------");
                if (RE_Category.Equals("Historical") || RE_Category.Equals("Relational"))
                {
                    Log("focus obj:" + DataStore.GetStringValue("user:intent:object"));
                    Log("Distance from agent to focus obj:" + dim.Distance(DataStore.GetStringValue("user:intent:object")));
                    Log("------------------------------------------------------------------------------");
                }

            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException)
                {
                    Debug.LogWarning(string.Format("VoxSim event manager couldn't handle \"{0}\"", eventStr));
                }
            }

        }
    }
    public string RelationsLog()
    {

        // Nada: logging the inserted event
        string srelation, finalrel;
        //if ((eventStr.Contains(",") && eventStr.Split(',')[1].Contains("block") && !dis)
        //    || (!eventStr.Contains(","))|| (eventStr.Contains(",") && eventStr.Split(',')[1].Contains("plate"))
        //        || (eventStr.Contains(",") && eventStr.Split(',')[1].Contains("cup")))
        //    {

        List<String> SceneRelations = new List<String>();


        // Nada: logging relations for each executed command in editor console


        foreach (DictionaryEntry entry in relationTracker.relations)
        {
            string v = (string)entry.Value;
            List<GameObject> keys = (List<GameObject>)entry.Key;
            List<String> relkey = new List<String>();

            foreach (GameObject key1 in (List<GameObject>)entry.Key)
            {
                relkey.Add(key1.name);
            }
            Debug.Log(relkey[0] + "and" + relkey[1]);
            srelation = v + " | " + relkey[0] + " and " + relkey[1];
            if (srelation.Contains(","))
            {
                SceneRelations.Add(srelation.Replace(",", " & "));
            }
            else
            {
                SceneRelations.Add(srelation);
            }


        }
        finalrel = string.Join(" + ", SceneRelations);
        Debug.Log("finalrel: "+ finalrel);



        return finalrel;
    }
    // append an event string to the end of the event manager's event list
    void AppendEvent(string key, DataStore.IValue value)
    {
        // only works in an interaction
        if (DataStore.GetBoolValue("user:isInteracting"))
        {
            string eventStr = string.Empty;

            // if user:intent:append:event prompted this method,
            //  extract the event string directly
            if (key == "user:intent:append:event")
            {
                eventStr = value.ToString();
            }
            // otherwise it might have prompted affordance learning first
            //  so append:event should already be set
            //  append that instead
            else if (key == "me:affordances:isLearning")
            {
                eventStr = DataStore.GetStringValue("user:intent:append:event");
            }

            Debug.Log(string.Format("Appending event {0}", eventStr));

            // if there are no other events in the event manager list, this will
            //  be executed immediately
            if (eventManager.events.Count == 0)
            {
                Debug.Log(string.Format("eventManager.events.Count {0}", eventManager.events.Count));

                // find what the "lastTheme" value should be of this
                //  to-be-executed event
                // split in case multiple events were shoved in here
                string firstAppendedEvent = eventStr.Split(new string[] { multiAppendDelimiter }, StringSplitOptions.None)[0];
                // extract objects and take the first one
                object obj = eventManager.ExtractObjects(
                    GlobalHelper.GetTopPredicate(firstAppendedEvent),
                    (String)GlobalHelper.ParsePredicate(firstAppendedEvent)[
                                                GlobalHelper.GetTopPredicate(firstAppendedEvent)])[0];

                // if it exists, make it lastTheme
                if ((obj as GameObject) != null)
                {
                    SetValue("me:lastTheme", (obj as GameObject).name, string.Empty);
                }
            }

            eventStr = eventStr.Trim();

            // if there is replacement content on the blackboard
            // then the assumption is that lastEvent should be set to the appended event
            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
            {
                Debug.Log(string.Format("Setting last event {0}", DataStore.GetStringValue("user:intent:append:event")));
                SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:append:event"),
                    string.Format("Store user:intent:append:event ({0}) in user:intent:lastEvent in case Diana did something wrong",
                        DataStore.GetStringValue("user:intent:append:event")));
                if (!DataStore.GetBoolValue("me:isUndoing"))
                {
                    SetValue("me:isUndoing", true, string.Empty);
                }
            }

            // if not learning affordances
            if (!DataStore.GetBoolValue("me:affordances:isLearning"))
            {
                // insert each event in the full appended event string into
                //  the event manager's event list at the proper position
                try
                {
                    eventManager.InsertEvent("", eventManager.events.Count);
                    foreach (string ev in eventStr.Split(new string[] { multiAppendDelimiter }, StringSplitOptions.None))
                    {
                        //eventManager.InsertEvent(ev, eventManager.events.Count);
                        // changed by Nada
                        SetValue("user:intent:event", ev, string.Empty);

                    }
                    // Nada: set empty to appended event to be able to aske for more composed events 
                    //SetValue("user:intent:append:event", string.Empty, string.Empty);

                }
                catch (Exception ex)
                {
                    if (ex is NullReferenceException)
                    {
                        Debug.LogWarning(string.Format("VoxSim event manager couldn't handle \"{0}\"", eventStr));
                    }
                }
            }
        }
    }

    // try compose a full event string
    void TryEventComposition(string key, DataStore.IValue value)
    {
        // try compose a full event string from new information on one of the
        //  following keys (see below)

        // only works in an interaction
        if (DataStore.GetBoolValue("user:isInteracting"))
        {
            Debug.Log(string.Format("Trying event composition with new info: {0} ({1})",
                key, key == "user:intent:partialEvent" ? DataStore.GetStringValue("user:intent:partialEvent") :
                    key == "user:intent:action" ? DataStore.GetStringValue("user:intent:action") :
                    key == "user:intent:object" ? DataStore.GetStringValue("user:intent:object") :
                    key == "user:intent:location" ? GlobalHelper.VectorToParsable(DataStore.GetVector3Value("user:intent:location")) :
                    key == "user:intent:append:partialEvent" ? DataStore.GetStringValue("user:intent:append:partialEvent") :
                    key == "user:intent:append:action" ? DataStore.GetStringValue("user:intent:append:action") : "Null"));

            // store all the relevant key values for later reference
            string eventStr = DataStore.GetStringValue("user:intent:partialEvent");
            string actionStr = DataStore.GetStringValue("user:intent:action");

            string appendEventStr = DataStore.GetStringValue("user:intent:append:partialEvent");
            string appendActionStr = DataStore.GetStringValue("user:intent:append:action");

            string objectStr = DataStore.GetStringValue("user:intent:object");
            Vector3 locationPos = DataStore.GetVector3Value("user:intent:location");

            string lastTheme = DataStore.GetStringValue("me:lastTheme");
            //bool proceed = true;
            //string unknown = "";

            // Nada: to handle unknown events or objects
            isunknown = UnknownEvents(eventStr);
            //
            if (!isunknown)
            {
                if (DataStore.GetBoolValue("me:affordances:isLearning"))
                {
                    // put interactions during affordance learning here
                }
                else
                {
                    // if key that changed was user:intent:object
                    if (key == "user:intent:object")
                    {
                        // if its value is not null or empty
                        if (!string.IsNullOrEmpty(objectStr))
                        {
                            if (objectStr != lastTheme)
                            {
                                // focus switched to a new item
                                // empty lastEvent
                                Debug.Log(string.Format("Clearing lastEvent (was \"{0}\")", DataStore.GetStringValue("user:intent:lastEvent")));
                                SetValue("user:intent:lastEvent", string.Empty, string.Empty);
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(eventStr))
                                {
                                    // object changed and it is not empty
                                    // and not different from lastTheme
                                    // e.g., pointed at the same object again

                                    // empty lastEvent
                                    Debug.Log(string.Format("Clearing lastEvent (was \"{0}\")", DataStore.GetStringValue("user:intent:lastEvent")));
                                    SetValue("user:intent:lastEvent", string.Empty, string.Empty);

                                    // empty lastThemePos
                                    Debug.Log(string.Format("Clearing lastThemePos (was \"{0}\")", DataStore.GetStringValue("me:lastThemePos")));
                                    SetValue("me:lastThemePos", Vector3.zero, string.Empty);

                                }

                            }

                            // if there's an object variable in eventStr and eventStr is a program (action)
                            if ((!string.IsNullOrEmpty(eventStr)) && (eventStr.Contains("{0}")) &&
                                (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(eventStr), voxmlLibrary) == "programs"))
                            {
                                // replace the variable with the new object and put the result in partiaEvent
                                eventStr = eventStr.Replace("{0}", objectStr);
                                SetValue("user:intent:partialEvent", eventStr, string.Empty);
                            }
                            // if not, do the same for appendEventStr
                            else if ((!string.IsNullOrEmpty(appendEventStr)) && (appendEventStr.Contains("{0}")) &&
                                (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(eventStr), voxmlLibrary) == "programs"))
                            {
                                appendEventStr = appendEventStr.Replace("{0}", objectStr);
                                SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                            }
                            // if not, if there's an object variable in actionStr
                            else if (!string.IsNullOrEmpty(actionStr))
                            {
                                if (actionStr.Contains("{0}"))
                                {
                                    // replace the variable with the new object and put the result in partiaEvent
                                    eventStr = actionStr.Replace("{0}", objectStr);
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                            }
                            // otherwise try to do the same for appendActionStr
                            else if (!string.IsNullOrEmpty(appendActionStr))
                            {
                                if (appendActionStr.Contains("{0}"))
                                {
                                    appendEventStr = appendActionStr.Replace("{0}", objectStr);
                                    SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                                }
                            }
                        }
                    }
                    // if key that changed was user:intent:action
                    else if (key == "user:intent:action")
                    {
                        // if the action is a slide
                        if (actionStr.StartsWith("slide"))
                        {
                            // if eventStr isn't empty
                            if (!string.IsNullOrEmpty(eventStr))
                            {
                                // if it's a servo event
                                if (GlobalHelper.GetTopPredicate(eventStr) == "servo")
                                {
                                    // then a current servo has been overriden by a slide
                                    // clear the event manager in preparation to handle
                                    //  the slide instruction
                                    eventManager.ClearEvents();
                                    SetValue("user:intent:append:event", string.Empty, string.Empty);
                                }
                            }
                        }

                        // if actionStr isn't empty
                        if (!string.IsNullOrEmpty(actionStr))
                        {
                            // if there is an object indicated
                            if (!string.IsNullOrEmpty(objectStr))
                            {
                                // if there's an object variable in actionStr
                                if (actionStr.Contains("{0}"))
                                {
                                    // replace the variable with the object and put the result in partiaEvent
                                    eventStr = actionStr.Replace("{0}", objectStr);
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                            }
                            // if there's a previous theme that could be applied
                            else if (!string.IsNullOrEmpty(lastTheme))
                            {
                                // if there's an object variable in actionStr
                                if (actionStr.Contains("{0}"))
                                {
                                    // replace the variable with the object and put the result in partiaEvent
                                    eventStr = actionStr.Replace("{0}", lastTheme);
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                            }
                            // otherwise, if there's no object indicated
                            else
                            {
                                // and there's an object variable in actionStr
                                if (actionStr.Contains("{0}"))
                                {
                                    // Diana should ask questions based on the action predicate
                                    if (actionStr.StartsWith("put"))
                                    {
                                        SetValue("me:intent:lookAt", "user", string.Empty);
                                        SetValue("me:speech:intent", "What should I move?", string.Empty);
                                        SetValue("me:emotion", "confusion", string.Empty);
                                    }
                                    else if (actionStr.StartsWith("slide"))
                                    {
                                        SetValue("me:intent:lookAt", "user", string.Empty);
                                        SetValue("me:speech:intent", "What should I push?", string.Empty);
                                        SetValue("me:emotion", "confusion", string.Empty);
                                    }
                                    else if (actionStr.StartsWith("grasp"))
                                    {
                                        SetValue("me:intent:lookAt", "user", string.Empty);
                                        SetValue("me:speech:intent", "What should I grab?", string.Empty);
                                        SetValue("me:emotion", "confusion", string.Empty);
                                    }
                                    // the exception is servo, where if there's a previous object
                                    //  start/continue sliding that one
                                    else if (actionStr.StartsWith("servo"))
                                    {
                                        if (!string.IsNullOrEmpty(lastTheme))
                                        {
                                            eventStr = actionStr.Replace("{0}", lastTheme);
                                            SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                        }
                                        else
                                        {
                                            SetValue("me:intent:lookAt", "user", string.Empty);
                                            SetValue("me:speech:intent", "What should I slide?", string.Empty);
                                            SetValue("me:emotion", "confusion", string.Empty);
                                        }
                                    }
                                }
                            }

                            // if there's s valid candidate location already on the blackboard
                            if (locationPos != default)
                            {
                                // if actionStr contains a location variable
                                if (actionStr.Contains("{1}"))
                                {
                                    // replace the variable with the location and put the result in partiaEvent
                                    eventStr = actionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                            }
                        }
                    }
                    // if the key that changed was user:intent:location
                    else if (key == "user:intent:location")
                    {
                        Debug.Log(string.Format("EMM: Setting user:intent:location to {0} ({1}) object {2}", DataStore.GetVector3Value("user:lastPointedAt:position"), "user:lastPointedAt:position", objectStr));


                        // if it's valid
                        if (locationPos != default)
                        {
                            Debug.Log(string.Format("0 EMM: Setting user:intent:location"));
                            // if eventStr is not empty
                            if (!string.IsNullOrEmpty(objectStr))
                            {
                                //objectStr1 = objectStr;
                                Debug.Log(string.Format("5.0 EMM: Setting user:intent:location"));
                                Debug.Log(string.Format("555 EMM: Setting user:intent:location {0}", DataStore.GetStringValue("user:lastPointedAt:name")));

                                // here we assume that if you have and object on the board
                                //  and then indicate a location, then you want Diana to put
                                //  the object at that location
                                // find the object, calculate its Y-offset and get specific target coordinates
                                GameObject theme = GameObject.Find(objectStr);
                                Vector3 targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(theme).extents.y, locationPos.z);
                                var dis1 = Vector3.Distance(GameObject.Find("plate").transform.position, DataStore.GetVector3Value("user:lastPointedAt:position"));
                                var dis2 = Vector3.Distance(GameObject.Find("cup").transform.position, DataStore.GetVector3Value("user:lastPointedAt:position"));

                                if (!GlobalHelper.ContainingObjects(targetLoc).Contains(theme) &&
                                    !(dis1 < 0.1) && !(dis2 < 0.1))
                                {
                                    Debug.Log(string.Format("5 EMM: Setting user:intent:location"));

                                    // replace the object var {0} with the object
                                    //  replace the loc var {1} with the target location
                                    //  assume "put" verb
                                    //  put the result in partialEvent
                                    eventStr = "put({0},{1})".Replace("{0}", objectStr).Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                                else if (!GlobalHelper.ContainingObjects(targetLoc).Contains(theme) &&
                                 ((dis2 < 0.1) || (dis2 < 0.1)))
                                {
                                    Debug.Log(string.Format("5.1 EMM: Setting user:intent:location"));
                                    eventStr = "ungrasp({0})".Replace("{0}", objectStr);
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                                //else
                                //{
                                //    Debug.Log(string.Format("5.2 EMM: Setting user:intent:location"));
                                //    eventStr = "ungrasp({0})".Replace("{0}", objectStr);
                                //    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                //}
                            }
                            else if (!string.IsNullOrEmpty(eventStr))
                            {
                                Debug.Log(string.Format("0.1 EMM: Setting user:intent:location {0}", eventStr));
                                // if contains a location variable
                                if (eventStr.Contains("{1}"))
                                {
                                    Debug.Log(string.Format("1 EMM: Setting user:intent:location"));
                                    // replace the variable with the location and put the result in partiaEvent
                                    eventStr = eventStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                                //else
                                //{

                                //    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                //    Debug.Log(string.Format("1.1 EMM: Setting user:intent:location {0}", eventStr));


                                //}
                            }
                            // if not, try the same with appendEventStr
                            else if (!string.IsNullOrEmpty(appendEventStr))
                            {
                                if (appendEventStr.Contains("{1}"))
                                {
                                    Debug.Log(string.Format("2 EMM: Setting user:intent:location"));

                                    appendEventStr = appendEventStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                                    SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                                }
                            }
                            // if not if actionStr is not empty
                            else if (!string.IsNullOrEmpty(actionStr))
                            {
                                // if contains a location variable
                                if (actionStr.Contains("{1}"))
                                {
                                    Debug.Log(string.Format("3 EMM: Setting user:intent:location"));

                                    // replace the variable with the location and put the result in partiaEvent
                                    eventStr = actionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                                    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                }
                            }
                            // if not, try the same with appendActionStr
                            else if (!string.IsNullOrEmpty(appendActionStr))
                            {
                                if (appendActionStr.Contains("{1}"))
                                {
                                    Debug.Log(string.Format("4 EMM: Setting user:intent:location"));

                                    appendEventStr = appendActionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                                    SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                                }
                            }
                            // otherwise (now, the only things available to put together are the location and an object, if available)
                            //else if (!string.IsNullOrEmpty(objectStr))
                            //{
                            //    Debug.Log(string.Format("5.0 EMM: Setting user:intent:location"));
                            //    // here we assume that if you have and object on the board
                            //    //  and then indicate a location, then you want Diana to put
                            //    //  the object at that location
                            //    // find the object, calculate its Y-offset and get specific target coordinates
                            //    GameObject theme = GameObject.Find(objectStr);
                            //    Vector3 targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(theme).extents.y, locationPos.z);
                            //    if (!GlobalHelper.ContainingObjects(targetLoc).Contains(theme))
                            //    {
                            //        Debug.Log(string.Format("5 EMM: Setting user:intent:location"));

                            //        // replace the object var {0} with the object
                            //        //  replace the loc var {1} with the target location
                            //        //  assume "put" verb
                            //        //  put the result in partialEvent
                            //        eventStr = "put({0},{1})".Replace("{0}", objectStr).Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                            //        SetValue("user:intent:partialEvent", eventStr, string.Empty);
                            //    }
                            //    //else
                            //    //{
                            //    //    Debug.Log(string.Format("5.1 EMM: Setting user:intent:location"));
                            //    //    eventStr = "put({0},{1})".Replace("{0}", objectStr).Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                            //    //    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                            //    //}
                            //}
                            Debug.Log(string.Format("6 EMM: Setting user:intent:location"));

                        }
                        else
                        {
                            //SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                            //SetValue("me:speech:intent", string.Format("invalid location"), string.Empty);

                            Debug.Log(string.Format("loc is default:"));

                            //return;
                        }
                    }
                    // if the key that changed was user:intent:partialEvent
                    //  (this is where the magic happens)
                    else if (key == "user:intent:partialEvent")
                    {
                        // if it's valid
                        if (!string.IsNullOrEmpty(eventStr))
                        {
                            // Nada: no historical or relational REs get inside here until it has been simplified
                            if (!eventStr.Contains("_adj"))
                            {
                                // Nada: print if enters this condition 
                                //Debug.Log(string.Format("enters no adj condition"));
                                if (eventStr.Contains(","))
                                {
                                    RE_Category = "Transit_Attributive";
                                }
                                else
                                {
                                    RE_Category = "Attributive";
                                }                            // if no variables left in the composed event string, no empty parens, and / Nada: no more than one event
                                if (!Regex.IsMatch(eventStr, @"\{[0-1]+\}") && !Regex.IsMatch(eventStr, @"\(\)") && !eventStr.Contains("+"))
                                {
                                    //Log("user:intent:partialEvent _ Attributive RE" + "  |  " + eventStr);
                                    // if parens match and count > 0
                                    if (eventStr.Count(f => f == '(') == eventStr.Count(f => f == ')') &&
                                        (eventStr.Count(f => f == '(') + eventStr.Count(f => f == ')') > 0))
                                    {
                                        // log what we're doing
                                        Debug.Log(string.Format("Composed object {0}, action {1}, and location {2} into event {3}",
                                            objectStr, actionStr, GlobalHelper.VectorToParsable(locationPos), eventStr));
                                        //Log("target object" + "  |  " + objectStr);
                                        //Log("target position" + "  |  " + GlobalHelper.VectorToParsable(locationPos));
                                        //Log("-------------------------------------------------------------------------------");

                                        // if there's a valid object
                                        if (!string.IsNullOrEmpty(objectStr))
                                        {
                                            // store this object as lastTheme
                                            //  store its position as well in case we undo this
                                            Debug.Log(string.Format("Setting lastTheme to {0}, lastThemePos to {1}", objectStr,
                                                GlobalHelper.VectorToParsable(GameObject.Find(objectStr).transform.position)));
                                            SetValue("me:lastTheme", objectStr, string.Empty);
                                            SetValue("me:lastThemePos", GameObject.Find(objectStr).transform.position, string.Empty);
                                        }

                                        // set user:intent:event to the completed partialEvent
                                        //  this key change will prompt PromptEvent
                                        //  which sends the event string to the VoxSim event manager
                                        SetValue("user:intent:event", eventStr, string.Empty);

                                        //SetValue("user:intent:partialEvent", string.Empty, string.Empty);

                                    }

                                }
                                //  here there are still variables in partialEvent and /Nada: command does not have more than one event 
                                //else if (Regex.IsMatch(eventStr, @"\{[0-1]+\}") && Regex.IsMatch(eventStr, @"\(\)") /*&& !eventStr.Contains("+")*/)
                                //Nada: it was not working, the previous scripts is commented and the following is considered
                                else if (((eventStr.Contains("{0}") || eventStr.Contains("{1}")) || (Regex.IsMatch(eventStr, @"\{[0-1]+\}") && Regex.IsMatch(eventStr, @"\(\)"))) && (!eventStr.Contains("+")))
                                {
                                    //if (Regex.IsMatch(appendEventStr, @"\{1\}\(.+\)(?=\))"))

                                    // Log the current state of user:intent:partialEvent
                                    Debug.Log(string.Format("Partial event is now {0}", eventStr));

                                    // special handling for servo events
                                    if (GlobalHelper.GetTopPredicate(eventStr) == "servo")
                                    {
                                        // if it's a servo left
                                        if (Regex.IsMatch(eventStr, @"\{1\}\(left\)(?=\))"))
                                        {
                                            // find objects in the partial event eventStr
                                            List<object> objs = eventManager.ExtractObjects(GlobalHelper.GetTopPredicate(eventStr),
                                                    (String)GlobalHelper.ParsePredicate(eventStr)[
                                                        GlobalHelper.GetTopPredicate(eventStr)]);
                                            if (objs.Count > 0)
                                            {
                                                // if there is a theme object
                                                //  clear partialEvent
                                                //  set object
                                                //  set servo w/ direction
                                                //  this triggers an update that will properly fill in partialEvent
                                                if (objs[0] is GameObject)
                                                {
                                                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                    SetValue("user:intent:object", (objs[0] as GameObject).name, string.Empty);
                                                    SetValue("user:intent:isServoLeft", true, string.Empty);
                                                }
                                            }
                                            // otherwise, if there's a lastTheme available
                                            //  try to fill in partialEvent with it
                                            else if (!string.IsNullOrEmpty(DataStore.GetStringValue("me:lastTheme")))
                                            {
                                                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                SetValue("user:intent:object", DataStore.GetStringValue("me:lastTheme"), string.Empty);
                                                SetValue("user:intent:isServoLeft", true, string.Empty);
                                            }
                                        }
                                        // if it's a servo right
                                        else if (Regex.IsMatch(eventStr, @"\{1\}\(right\)(?=\))"))
                                        {
                                            // find objects in the partial event eventStr
                                            List<object> objs = eventManager.ExtractObjects(GlobalHelper.GetTopPredicate(eventStr),
                                                    (String)GlobalHelper.ParsePredicate(eventStr)[
                                                        GlobalHelper.GetTopPredicate(eventStr)]);
                                            if (objs.Count > 0)
                                            {
                                                // if there is a theme object
                                                //  clear partialEvent
                                                //  set object
                                                //  set servo w/ direction
                                                //  this triggers an update that will properly fill in partialEvent
                                                if (objs[0] is GameObject)
                                                {
                                                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                    SetValue("user:intent:object", (objs[0] as GameObject).name, string.Empty);
                                                    SetValue("user:intent:isServoRight", true, string.Empty);
                                                }
                                            }
                                            // otherwise, if there's a lastTheme available
                                            //  try to fill in partialEvent with it
                                            else if (!string.IsNullOrEmpty(DataStore.GetStringValue("me:lastTheme")))
                                            {
                                                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                SetValue("user:intent:object", DataStore.GetStringValue("me:lastTheme"), string.Empty);
                                                SetValue("user:intent:isServoRight", true, string.Empty);
                                            }
                                        }
                                        // if it's a servo front
                                        else if (Regex.IsMatch(eventStr, @"\{1\}\(front\)(?=\))"))
                                        {
                                            // find objects in the partial event eventStr
                                            List<object> objs = eventManager.ExtractObjects(GlobalHelper.GetTopPredicate(eventStr),
                                                    (String)GlobalHelper.ParsePredicate(eventStr)[
                                                        GlobalHelper.GetTopPredicate(eventStr)]);
                                            if (objs.Count > 0)
                                            {
                                                // if there is a theme object
                                                //  clear partialEvent
                                                //  set object
                                                //  set servo w/ direction
                                                //  this triggers an update that will properly fill in partialEvent
                                                if (objs[0] is GameObject)
                                                {
                                                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                    SetValue("user:intent:object", (objs[0] as GameObject).name, string.Empty);
                                                    SetValue("user:intent:isServoFront", true, string.Empty);
                                                }
                                            }
                                            // otherwise, if there's a lastTheme available
                                            //  try to fill in partialEvent with it
                                            else if (!string.IsNullOrEmpty(DataStore.GetStringValue("me:lastTheme")))
                                            {
                                                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                SetValue("user:intent:object", DataStore.GetStringValue("me:lastTheme"), string.Empty);
                                                SetValue("user:intent:isServoFront", true, string.Empty);
                                            }
                                        }
                                        // if it's a servo back
                                        else if (Regex.IsMatch(eventStr, @"\{1\}\(back\)(?=\))"))
                                        {
                                            // find objects in the partial event eventStr
                                            List<object> objs = eventManager.ExtractObjects(GlobalHelper.GetTopPredicate(eventStr),
                                                    (String)GlobalHelper.ParsePredicate(eventStr)[
                                                        GlobalHelper.GetTopPredicate(eventStr)]);
                                            if (objs.Count > 0)
                                            {
                                                // if there is a theme object
                                                //  clear partialEvent
                                                //  set object
                                                //  set servo w/ direction
                                                //  this triggers an update that will properly fill in partialEvent
                                                if (objs[0] is GameObject)
                                                {
                                                    SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                    SetValue("user:intent:object", (objs[0] as GameObject).name, string.Empty);
                                                    SetValue("user:intent:isServoBack", true, string.Empty);
                                                }
                                            }
                                            // otherwise, if there's a lastTheme available
                                            //  try to fill in partialEvent with it
                                            else if (!string.IsNullOrEmpty(DataStore.GetStringValue("me:lastTheme")))
                                            {
                                                SetValue("user:intent:partialEvent", string.Empty, string.Empty);
                                                SetValue("user:intent:object", DataStore.GetStringValue("me:lastTheme"), string.Empty);
                                                SetValue("user:intent:isServoBack", true, string.Empty);
                                            }
                                        }
                                    }


                                    // if we encounter an focus object variable
                                    else if (eventStr.Contains("{0}"))
                                    {

                                        Debug.Log(string.Format("00 encounter an object variable: {0}", eventStr));
                                        // fill in partial event with the current object of focus
                                        //  if one exists
                                        if (!string.IsNullOrEmpty(objectStr) /*|| eventStr.Contains("plate") || eventStr.Contains("cup")*/)
                                        {
                                            GameObject target = GameObject.Find(objectStr);
                                            List<object> PointingTargetAttr = null;
                                            Debug.Log(string.Format(" 11 encounter an object variable"));

                                            // Nada: if the user did not point to the ambiguate block: if pointing value is null
                                            // or the pointed at obj is not the same as the intented object

                                            if (!string.IsNullOrEmpty(dp.getColor()))
                                            {
                                                Debug.Log(string.Format(" 22 encounter an object variable"));
                                                PointingTargetAttr = target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList();

                                                //cdis = true;
                                                //if (!PointingTargetAttr[0].Equals(dp.getColor()))
                                                //{

                                                //    SetValue("me:speech:intent", "This is not a " + dp.getColor() + " block. " + "Which " + dp.getColor() + " block you are referring to?", string.Empty);
                                                //    logTocsv(nlu.GetUserSpeech(), "This is not a " + dp.getColor() + " block. " + "Which " + dp.getColor() + " block you are referring to?", dp.getform(), RelationsLog(), dim.Configurations());

                                                //    //SetValue("me:intent:action", string.Empty, string.Empty);
                                                //    SetValue("me:intent:lookAt", "user", string.Empty);
                                                //    SetValue("me:emotion", "confusion", string.Empty);
                                                //    SetValue("user:intent:object", string.Empty, string.Empty);

                                                //    Debug.Log(string.Format(" 222 encounter an object variable"));

                                                //    //return;
                                                //}
                                                //// no color for the block
                                                //else
                                                //{
                                                Debug.Log(string.Format(" 33 encounter an object variable"));
                                                eventStr = eventStr.Replace("{0}", objectStr);
                                                SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                                //}
                                            }
                                            else
                                            {
                                                Debug.Log(string.Format(" 44 encounter an object variable"));
                                                eventStr = eventStr.Replace("{0}", objectStr);
                                                SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            // nada: Disambiguate {0} if object is embty, no pointing, put this red block to the plate 
                                            if (!string.IsNullOrEmpty(dp.getColor()) /*&& (eventStr.Contains("plate") || eventStr.Contains("cup"))*/)
                                            {
                                                // flag for the color of the block
                                                cdis = true;
                                                Debug.Log(string.Format("55 encounter an object variable"));
                                                SetValue("me:speech:intent", "Which " + dp.getColor() + " block?", string.Empty);
                                                // SetValue("me:intent:action", string.Empty, string.Empty);
                                                SetValue("me:intent:lookAt", "user", string.Empty);
                                                SetValue("me:emotion", "confusion", string.Empty);
                                                // return;
                                            }
                                            //else if (!string.IsNullOrEmpty(lastTheme) && !transit)
                                            //{
                                            //    Debug.Log(string.Format("66 encounter an object variable"));

                                            //    eventStr = eventStr.Replace("{0}", lastTheme);
                                            //    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                            //    SetValue("me:speech:intent", "I assume the block you mean is this " + lastTheme.Substring(0, lastTheme.Length - 1) + ". Is it still your focus?", string.Empty);
                                            //}

                                            // 
                                            else if (string.IsNullOrEmpty(dp.getColor()) /*&& !eventStr.Contains("plate")
                                                && !eventStr.Contains("cup")*/)
                                            {
                                                demdis = true;
                                                cdis = false;
                                                Debug.Log(string.Format("77 encounter an object variable"));
                                                SetValue("me:speech:intent", "Which block?", string.Empty);
                                            }
                                        }
                                    }
                                    // if it not a servo, handle variables
                                    // if we encounter a direction variable (form "{1}(DIR)")
                                    if (Regex.IsMatch(eventStr, @"\{1\}\(.+\)(?=\))"))
                                    {
                                        string match = Regex.Match(eventStr, @"\{1\}\(.+\)(?=\))").Value;
                                        // extract the direction
                                        string dir = match.Replace("{1}(", "").Replace(")", "");

                                        try
                                        {
                                            // try the infer the target location given the event, theme, and direction
                                            Vector3 targetPos = InferTargetLocation(GlobalHelper.GetTopPredicate(eventStr),
                                                string.IsNullOrEmpty(objectStr) ? GameObject.Find(lastTheme) : GameObject.Find(objectStr), dir);

                                            // fill in partialEvent with the inferred location
                                            Debug.Log(string.Format("Replacing \"{0}\" with \"{1}\"", match, GlobalHelper.VectorToParsable(targetPos)));
                                            SetValue("user:intent:partialEvent",
                                                eventStr.Replace(match, GlobalHelper.VectorToParsable(targetPos)), string.Empty);
                                        }
                                        catch (Exception ex)
                                        {
                                            if (ex is NullReferenceException)   // no theme found!
                                            {
                                                // try to extract the theme from the event string
                                                object obj = eventManager.ExtractObjects(GlobalHelper.GetTopPredicate(eventStr),
                                                    (String)GlobalHelper.ParsePredicate(eventStr)[
                                                        GlobalHelper.GetTopPredicate(eventStr)])[0];
                                                if (obj is GameObject)
                                                {
                                                    // try the infer the target location given the event, theme, and direction
                                                    Vector3 targetPos = InferTargetLocation(GlobalHelper.GetTopPredicate(eventStr),
                                                        obj as GameObject, dir);

                                                    // fill in partialEvent with the inferred location
                                                    Debug.Log(string.Format("Replacing \"{0}\" with \"{1}\"", match, GlobalHelper.VectorToParsable(targetPos)));
                                                    SetValue("user:intent:partialEvent",
                                                        eventStr.Replace(match, GlobalHelper.VectorToParsable(targetPos)), string.Empty);
                                                }
                                            }
                                        }
                                    }
                                    // if we encounter a location variable
                                    else if (eventStr.Contains("{1}"))
                                    {
                                        // if it's a valid loation
                                        if (locationPos != default)
                                        {
                                            Vector3 targetLoc = locationPos;

                                            if (!string.IsNullOrEmpty(objectStr))
                                            {
                                                // adjust the target location for the bounds of the theme object if applicable
                                                targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(GameObject.Find(objectStr)).extents.y, locationPos.z);
                                            }

                                            // fill in partial event with the location
                                            eventStr = eventStr.Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                                            SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                        }
                                        // otherwise if there's an indicated object
                                        else if (!string.IsNullOrEmpty(objectStr))
                                        {
                                            // and that object isn't already part of the event (i.e., it's not the theme)
                                            if (!eventStr.Contains(objectStr))
                                            {
                                                // replace the location var with "on(that)"
                                                //  and fill in partialEvent
                                                eventStr = eventStr.Replace("{1}", string.Format("on({0})", objectStr));
                                                SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                            }
                                        }
                                    }
                                }
                                // Nada: this is for "and" composition in events
                                // "+" used between two events instead of "and"
                                else if (eventStr.Contains("+"))
                                {
                                    compose = true;

                                    if (!string.IsNullOrEmpty(objectStr))
                                    {
                                        GameObject target = GameObject.Find(objectStr);
                                        List<object> PointingTargetAttr = null;
                                        PointingTargetAttr = target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList();
                                        Debug.Log(string.Format(" 11 encounter an object variable"));

                                        // Nada: if the user did not point to the ambiguate block: if pointing value is null
                                        // or the pointed at obj is not the same as the intented object

                                        //if (!string.IsNullOrEmpty(dp.getColor()))
                                        //{
                                        Debug.Log(string.Format(" 22 encounter an object variable"));

                                        //cdis = true;
                                        //if (!PointingTargetAttr[0].Equals(dp.getColor()))
                                        //{

                                        //    SetValue("me:speech:intent", "This is not a " + dp.getColor() + " block. " + "Which " + dp.getColor() + " block you are referring to?", string.Empty);
                                        //    logTocsv(nlu.GetUserSpeech(), "This is not a " + dp.getColor() + " block. " + "Which " + dp.getColor() + " block you are referring to?", dp.getform(), RelationsLog(), dim.Configurations());

                                        //    //SetValue("me:intent:action", string.Empty, string.Empty);
                                        //    SetValue("me:intent:lookAt", "user", string.Empty);
                                        //    SetValue("me:emotion", "confusion", string.Empty);
                                        //    SetValue("user:intent:object", string.Empty, string.Empty);

                                        //    Debug.Log(string.Format(" 222 encounter an object variable"));

                                        //    //return;
                                        //}
                                        //else
                                        //{
                                        Debug.Log(string.Format(" 33 encounter an object variable"));

                                        // SetValue("user:intent:append:partialEvent", eventStr, string.Empty);
                                        // }
                                        //}
                                        //else
                                        //{

                                        Debug.Log(string.Format(" 44 encounter an object variable"));
                                        SetValue("user:intent:append:partialEvent", eventStr, string.Empty);
                                        //}
                                    }
                                    else
                                    {
                                        // nada: Disambiguate {0} if object is embty
                                        if (!string.IsNullOrEmpty(dp.getColor()))
                                        {
                                            cdis = true;
                                            Debug.Log(string.Format("55 encounter an object variable"));

                                            SetValue("me:speech:intent", "Which " + dp.getColor() + " block?", string.Empty);
                                            // SetValue("me:intent:action", string.Empty, string.Empty);
                                            SetValue("me:intent:lookAt", "user", string.Empty);
                                            SetValue("me:emotion", "confusion", string.Empty);
                                            // return;
                                        }
                                        //else if (!string.IsNullOrEmpty(lastTheme) && !transit)
                                        //{
                                        //    Debug.Log(string.Format("66 encounter an object variable"));

                                        //    eventStr = eventStr.Replace("{0}", lastTheme);
                                        //    SetValue("user:intent:partialEvent", eventStr, string.Empty);
                                        //    SetValue("me:speech:intent", "I assume the block you mean is this " + lastTheme.Substring(0, lastTheme.Length - 1) + ". Is it still your focus?", string.Empty);

                                        //}
                                        else
                                        {
                                            if (eventStr.Contains("cup") || eventStr.Contains("plate"))
                                            {
                                                Debug.Log(string.Format("++ encounter an object variable lm"));
                                                if (!trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && (eventStr.Split('+')[0].Contains("cup") || eventStr.Split('+')[0].Contains("plate")) ||
                                                    !trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && (eventStr.Split('+')[1].Contains("cup") || eventStr.Split('+')[1].Contains("plate")))
                                                {
                                                    SetValue("me:speech:intent", string.Format("This is a landmark. Please select a block only!"), string.Empty);
                                                    return;
                                                }
                                            }

                                            else
                                            {
                                                Debug.Log(string.Format("++ encounter an object variable"));

                                                SetValue("me:speech:intent", "Which block?", string.Empty);
                                            }
                                            //SetValue("user:intent:append:partialEvent", eventStr, string.Empty);

                                            // return;
                                        }
                                    }
                                    //if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
                                    //{

                                    //    if (!string.IsNullOrEmpty(dp.getColor()))
                                    //    {
                                    //        SetValue("me:speech:intent", "Which " + dp.getColor() + " block?", string.Empty);
                                    //        cdis = true;
                                    //        return;
                                    //    }
                                    //    else
                                    //    {
                                    //        SetValue("me:speech:intent", "Which block?", string.Empty);
                                    //        return;
                                    //    }

                                    //}
                                    //else
                                    //{
                                    //    SetValue("user:intent:append:partialEvent", eventStr, string.Empty);
                                    //    Debug.Log(string.Format("Nada: Append two composed events to Partial event is now {0}", eventStr));
                                    //}
                                }
                                Debug.Log(string.Format(" nothing applied in partial event operations"));
                                //SetValue("me:speech:intent", "This is not clear! What do you mean?", string.Empty);


                            }
                            // Nada: Find intersection between _relations list and eventString list
                            // If there is intersection, it is relational REs
                            // else, it is historical REs
                            else if (eventStr.Contains("_adj"))
                            {
                                string[] eventStrings = eventStr.Split('(');

                                if (eventStrings.Intersect(_relations).Any())
                                {
                                    // relational REs
                                    Log("user:intent:partialEvent _ Relational RE" + "  |  " + eventStr);

                                    Debug.Log(string.Format("Nada: Inside relational condition"));
                                    relational_RE(eventStrings);
                                    relationalRE = true;
                                    LogrelationalRE = true;
                                    relEvent = eventStr;

                                }
                                else
                                {
                                    // historical REs
                                    Log("user:intent:partialEvent _ Historical RE" + "  |  " + eventStr);

                                    Debug.Log(string.Format("Nada: Inside historical condition"));
                                    historical_REs(eventStrings);
                                    HistoricalRE = true;

                                }
                            }
                        }



                    }
                    // handling for append:action
                    else if (key == "user:intent:append:action")
                    {
                        if (!string.IsNullOrEmpty(appendActionStr))
                        {
                            if (!string.IsNullOrEmpty(objectStr))
                            {
                                // fill in object variable with indicated object if possible
                                if (appendActionStr.Contains("{0}"))
                                {
                                    appendEventStr = appendActionStr.Replace("{0}", objectStr);
                                }
                            }

                            if (locationPos != default)
                            {
                                // fill in location variable with indicated location if possible
                                if (appendActionStr.Contains("{1}"))
                                {
                                    appendEventStr = appendActionStr.Replace("{1}", GlobalHelper.VectorToParsable(locationPos));
                                }
                            }

                            // fill in partialEvent
                            SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                        }
                    }
                    else if (key == "user:intent:append:partialEvent")
                    {
                        // if no variables left in the composed event string
                        if (!string.IsNullOrEmpty(appendEventStr))
                        {
                            if (!Regex.IsMatch(appendEventStr, @"\{[0-1]+\}"))
                            {
                                Log("user:intent:append:partialEvent _ Attributive RE" + "  |  " + appendEventStr);

                                Debug.Log(string.Format("Composed object {0}, action {1}, and location {2} into event {3}",
                                    objectStr, appendActionStr, GlobalHelper.VectorToParsable(locationPos), appendEventStr));
                                SetValue("user:intent:append:event", appendEventStr, string.Empty);
                                Log("target object" + "  |  " + objectStr);
                                //Log("target position" + "  |  " + GlobalHelper.VectorToParsable(locationPos));
                                //Log("-------------------------------------------------------------------------------");

                            }
                            else
                            {
                                Debug.Log(string.Format("Partial event is now {0}", appendEventStr));

                                if (Regex.IsMatch(appendEventStr, @"\{1\}\(.+\)(?=\))"))
                                {
                                    string match = Regex.Match(appendEventStr, @"\{1\}\(.+\)(?=\))").Value;
                                    string dir = match.Replace("{1}(", "").Replace(")", "");
                                    Vector3 targetPos = InferTargetLocation(GlobalHelper.GetTopPredicate(appendEventStr), GameObject.Find(objectStr), dir);
                                    Debug.Log(GlobalHelper.VectorToParsable(targetPos));
                                    Debug.Log(match);

                                    SetValue("user:intent:append:partialEvent",
                                        appendEventStr.Replace(match, GlobalHelper.VectorToParsable(targetPos)), string.Empty);
                                }
                                // if we encounter an object variable
                                else if (appendEventStr.Contains("{0}"))
                                {
                                    // fill in partial event with the current object of focus
                                    //  if one exists
                                    if (!string.IsNullOrEmpty(objectStr))
                                    {
                                        appendEventStr = appendEventStr.Replace("{0}", objectStr);
                                        SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                                    }
                                    // or with lastTheme otherwise
                                    else if (!string.IsNullOrEmpty(lastTheme))
                                    {
                                        eventStr = actionStr.Replace("{0}", lastTheme);
                                        SetValue("user:intent:append:partialEvent", eventStr, string.Empty);
                                    }
                                }
                                // if we encounter a location variable
                                else if (appendEventStr.Contains("{1}"))
                                {
                                    // if it's a valid loation
                                    if (locationPos != default)
                                    {
                                        Vector3 targetLoc = locationPos;

                                        if (!string.IsNullOrEmpty(objectStr))
                                        {
                                            // adjust the target location for the bounds of the theme object if applicable
                                            targetLoc = new Vector3(locationPos.x, locationPos.y + GlobalHelper.GetObjectWorldSize(GameObject.Find(objectStr)).extents.y, locationPos.z);
                                        }
                                        // fill in partial event with the location
                                        appendEventStr = eventStr.Replace("{1}", GlobalHelper.VectorToParsable(targetLoc));
                                        SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                                    }
                                    // otherwise if there's an indicated object
                                    else if (!string.IsNullOrEmpty(objectStr))
                                    {
                                        // and that object isn't already part of the event (i.e., it's not the theme)
                                        if (!eventStr.Contains(objectStr))
                                        {
                                            // replace the location var with "on(that)"
                                            //  and fill in partialEvent
                                            eventStr = eventStr.Replace("{1}", string.Format("on({0})", objectStr));
                                            SetValue("user:intent:append:partialEvent", eventStr, string.Empty);
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }
    }


    //----------------------------------RELATIONAL_REs--------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------

    // Nada: find a target from relational description like:
    // "bring the green block that is beside the blue block" > bring(green(beside_adj(blue_block(block)))
    void relational_RE(string[] eventStrings)
    {
        relationalRE = false;
        RE_Category = "Relational";
        List<GameObject> gobj = new List<GameObject>();
        /* Find intersection between _relations list and eventString list
        /* If there is intersection, it is relational REs
        /* else, it is historical REs*/
        for (int i = 0; i < grabbableBlocks.childCount; i++)
        {
            gobj.Add(grabbableBlocks.GetChild(i).gameObject);
        }
        string targetName = "";
        string relationalObjName2;
        string relationalObjName1 = "";
        string[] robj;
        //bool isEqual, isEqual2;
        // bool relfound = false;
        bool tarobjfound = false;
        bool relobjfound = false;
        IEnumerable<string> intersection = eventStrings.Intersect(_relations);
        string relation = "";
        GameObject target = null;
        GameObject relationalObj = null;
        foreach (string a in intersection)
        {
            Debug.Log(string.Format("Nada : Intersection of relations list and received event is..." + a));
            relation = a;
        }

        //> retrieve element of index 0 =event
        string _event = eventStrings[0];
        //> retrieve the target object= last index and remove ))) from it
        string targeObj = eventStrings[eventStrings.Length - 1];
        targeObj = targeObj.Replace(")", "");

        if (targeObj.Equals("cup") || targeObj.Equals("plate"))
        {
            logTocsv(nlu.GetUserSpeech(), "This is a landmark. Please select a block only!", dp.getform(), RelationsLog(), dim.Configurations());
            Debug.Log(string.Format("Nada : logtocsv"));
            SetValue("me:speech:intent", string.Format("This is a landmark. Please select a block only!"), string.Empty);
            return;
        }


        //if relation's index is 2: E.G.,right in: bring(red(right(blue_block(block))))
        //----------------------------------------------------------------------------

        if (Array.IndexOf(eventStrings, relation) == 2)
        {
            Debug.Log(string.Format("Nada : The index of relation is 2..."));
            //relation = relation.Replace("_adj", "");
            //> retrieve element of index 1 =targetColor
            string targetColor = eventStrings[1];
            //> retrieve element of index 3 =relationalObject
            relationalObjName1 = eventStrings[3];
            //> concat targetColor+TargetObj=target
            targetName = targetColor.Substring(0, 1).ToUpper() + targetColor.Substring(1, targetColor.Length - 1) + targeObj.Substring(0, 1).ToUpper() + targeObj.Substring(1, targeObj.Length - 1);
            Debug.Log(string.Format("Nada : targetName..." + targetName));
        }

        //if relation's index is 1: E.G., bring(righ_adj(blue_block(block))))
        //----------------------------------------------------------------------------

        else if (Array.IndexOf(eventStrings, relation) == 1)
        {
            Debug.Log(string.Format("Nada : The index of relation is 1..."));
            relationalObjName1 = eventStrings[2];
            targetName = targeObj.Substring(0, 1).ToUpper() + targeObj.Substring(1, targeObj.Length - 1);



            Debug.Log(string.Format("Nada : targetName..." + targetName));
        }

        //Capitalize the objects' names
        if (relationalObjName1.Contains("_"))
        {
            robj = relationalObjName1.Split('_');
            relationalObjName2 = robj[0].Substring(0, 1).ToUpper() + robj[0].Substring(1, robj[0].Length - 1) + robj[1].Substring(0, 1).ToUpper() + robj[1].Substring(1, robj[1].Length - 1);
        }
        else
        {
            relationalObjName2 = relationalObjName1.Substring(0, 1).ToLower() + relationalObjName1.Substring(1, relationalObjName1.Length - 1);
        }
        Debug.Log(string.Format("Nada : relationalObjName2..." + relationalObjName2));

        relation = relation.Replace("_adj", "");

        // relationsTracker trackes relations in human's frame, Diana resolves relations based on her frame
        // therefore, humans should refer to objects based on her frame, then we flip relations (right & left) to match
        // relations generated by relationsTracker

        if (relation.Equals("left")) relation = "right";
        else if (relation.Equals("right")) relation = "left";

        string[] values = new String[10];
        // to handle if focus or target objects are not exist
        if (dublicated_blocks1.Contains(targetName + "1"))
        {
            target = GameObject.Find(targetName + "1");
            tarobjfound = true;
        }
        else if (!dublicated_blocks1.Contains(targetName + "1") && gobj.Contains(GameObject.Find(targetName + "1")))
        {
            target = GameObject.Find(targetName);
            tarobjfound = true;
        }
        else if (!dublicated_blocks1.Contains(targetName + "1") && !gobj.Contains(GameObject.Find(targetName + "1")))
        {
            target = null;
            tarobjfound = false;
        }

        if (dublicated_blocks1.Contains(relationalObjName2))
        {
            relationalObj = GameObject.Find(relationalObjName2);

            if (relationalObj == null)
            {
                relationalObj = GameObject.Find(relationalObjName2 + "1");
            }
            relobjfound = true;
        }
        else if (!dublicated_blocks1.Contains(relationalObjName2) && gobj.Contains(GameObject.Find(relationalObjName2)))
        {
            relationalObj = GameObject.Find(relationalObjName2);
            relobjfound = true;
        }
        else if (!dublicated_blocks1.Contains(relationalObjName2 + "1") && gobj.Contains(GameObject.Find(relationalObjName2 + "1")))
        {
            relationalObj = GameObject.Find(relationalObjName2 + "1");
            relobjfound = true;
        }

        else if (!dublicated_blocks1.Contains(relationalObjName2) && !gobj.Contains(GameObject.Find(relationalObjName2)))
        {
            relationalObj = null;
            relobjfound = false;
        }
        else if (!dublicated_blocks1.Contains(relationalObjName2 + "1") && !gobj.Contains(GameObject.Find(relationalObjName2 + "1")))
        {
            relationalObj = null;
            relobjfound = false;
        }
        if (relobjfound && tarobjfound)
        {
            List<GameObject> gameobjs = new List<GameObject>(new[] { target, relationalObj });
            Debug.Log(string.Format("Nada : gameobjs[0]..." + gameobjs[0].name.Substring(0, gameobjs[0].name.Length - 1)));
            Debug.Log(string.Format("Nada : gameobjs[1]..." + gameobjs[1].name.Substring(0, gameobjs[1].name.Length - 1)));

            foreach (DictionaryEntry entry in relationTracker.relations)
            {
                List<GameObject> keys = (List<GameObject>)entry.Key;
                if (keys[0].name.Contains(gameobjs[0].name.Substring(0, gameobjs[0].name.Length - 1)) && keys[1].name.Contains(gameobjs[1].name.Substring(0, gameobjs[1].name.Length - 1)))
                {
                    //GameObject on_relobj = GameObject.Find(keys[1].name);
                    //bool on_surfaceClear = DialogueUtility.SurfaceClear(on_relobj, out GameObject blocker2);
                    //check if the relation exists between these objs
                    string v = (string)entry.Value;
                    if (v.Contains(','))
                    {
                        values = v.Split(',');
                    }
                    else
                    {
                        values[0] = v;
                    }
                    // because relation tracker does not track on and beside relations,
                    // we find the closest synonyms from the implemented relations
                    if (relation.Equals("on") || relation.Equals("beside") || relation.Equals("under") || relation.Equals("below") || relation.Equals("underneath") || relation.Equals("above"))
                    {
                        Debug.Log(string.Format("Nada : relation.Equals under: " + relation.Equals("under")));
                        Debug.Log(string.Format("Nada : values.Intersect(on_synonyms).Any(): " + values.Intersect(on_synonyms).Any()));

                        if (relation.Equals("beside") && values.Intersect(bes_synonyms).Any())
                        {
                            IEnumerable<string> shared = values.Intersect(bes_synonyms);
                            relation = shared.ToArray()[0];
                            Debug.Log(string.Format("Nada : Intersection of values list and Beside Synonyms..." + relation));
                            Debug.Log(string.Format("Nada : the relation: " + relation + " exists"));

                        }
                        else if (relation.Equals("on") && !values.Contains("under") && !values.Contains("in_front") && values.Intersect(on_synonyms).Any())
                        {
                            IEnumerable<string> shared = values.Intersect(on_synonyms);
                            relation = shared.ToArray()[0];
                            Debug.Log(string.Format("Nada : Intersection of values list and On Synonyms..." + relation));
                            Debug.Log(string.Format("Nada : key 1: " + keys[1].name));

                        }
                        else if ((relation.Equals("under") || relation.Equals("below") || relation.Equals("underneath")) && values.Intersect(under_synonyms).Any())
                        {
                            IEnumerable<string> shared = values.Intersect(under_synonyms);
                            relation = shared.ToArray()[0];
                            Debug.Log(string.Format("Nada : Intersection of values list and under Synonyms..." + relation));
                            Debug.Log(string.Format("Nada : key 1: " + keys[1].name));

                        }
                    }
                    if (values.Contains(relation))
                    {
                        GameObject blocker = null;
                        relObj = keys[0].name;
                        Debug.Log(string.Format("Nada : The relation " + relation + " between these objects " + keys[0].name + " and " + keys[1].name + " is tracked"));
                        string partialEvent = _event + "(" + relObj + ")";
                        // stored for logging purposes

                        Debug.Log(string.Format("Nada : partialEvent: " + partialEvent));
                        SetValue("user:intent:object", relObj, string.Empty);
                        if (DialogueUtility.SurfaceClear(keys[0], out blocker) /*&& emm.getDisam()*/)
                        {
                            SetValue("user:intent:event", partialEvent, string.Empty);
                            relfound = true;
                            Dianafocustime = System.DateTime.Now.ToString("HH:mm:ss");
                            Focus1 = relObj;
                            break;
                        }

                        else
                        {
                            SetValue("me:speech:intent", string.Format("I can't. It is blocked by another block."), string.Empty);
                            logTocsv(nlu.GetUserSpeech(), "I can't. It is blocked by another block.", dp.getform(), RelationsLog(), dim.Configurations());

                            relfound = true;
                            Dianafocustime = System.DateTime.Now.ToString("HH:mm:ss");
                            Focus1 = relObj;
                            SetValue("me:intent:action", string.Empty, string.Empty);
                            SetValue("me:intent:lookAt", string.Empty, string.Empty);
                            break;
                        }
                    }

                }

            }
            if (!relfound)
            {
                Focus1 = targetName + 1;
                Focus2 = targetName + 2;
                partialEvent1 = _event + "(" + Focus1 + ")";
                partialEvent2 = _event + "(" + Focus2 + ")";
                Dianafocustime = System.DateTime.Now.ToString("HH:mm:ss");
                speech = nlu.GetUserSpeech();
                parsing = dp.getform();
                eventplf1 = partialEvent1;
                eventplf2 = partialEvent2;
                targetobject = "None";
                spaceRelations = RelationsLog();

                //if yes: take these and add to history
                //if no: try it  

                Debug.Log(string.Format("Nada : no object with this description here "));
                //SetValue("me:speech:intent", string.Format("Which one? I can't see an object with this description here"), string.Empty);
                SetValue("me:speech:intent", string.Format("Do you mean this one?"), string.Empty);
                SetValue("me:intent:action", "point", string.Empty);
                //SetValue("me:intent:pointAt","YellowBlock1", string.Empty);
                //SetValue("me:intent:target", GameObject.Find("YellowBlock1").transform.position, string.Empty);
                SetValue("me:intent:lookAt", Focus1, string.Empty);
                SetValue("me:intent:targetObj", Focus1, string.Empty);
                SetValue("user:intent:object", Focus1, string.Empty);

                //if (!DataStore.GetBoolValue("user:intent:isNevermind"))
                //{
                //    string partialEvent = _event + "(YellowBlock1)";
                //    SetValue("user:intent:event", partialEvent, string.Empty);
                //}
            }
        }
        else if (!relobjfound && !tarobjfound)
        {
            SetValue("me:speech:intent", string.Format("There are no " + targetName + " and " + relationalObjName2 + " here!"), string.Empty);
            SetValue("user:intent:partialEvent", string.Empty, string.Empty);
            Debug.Log(string.Format("Nada : relation1 "));

        }
        else if (relobjfound && !tarobjfound && !targeObj.ToLower().Equals("block"))
        {
            Debug.Log(string.Format("Nada : relation2 "));
            SetValue("me:speech:intent", string.Format("There is no " + targetName + " here!"), string.Empty);
            logTocsv(nlu.GetUserSpeech(), "There is no " + targetName + " here!", dp.getform(), RelationsLog(), dim.Configurations());
            SetValue("user:intent:partialEvent", string.Empty, string.Empty);

        }
        else if (!relobjfound && tarobjfound)
        {
            Debug.Log(string.Format("Nada : relation3 "));
            SetValue("me:speech:intent", string.Format("There is no " + relationalObjName2 + " here!"), string.Empty);
            logTocsv(nlu.GetUserSpeech(), "There is no " + relationalObjName2 + " here!", dp.getform(), RelationsLog(), dim.Configurations());
            SetValue("user:intent:partialEvent", string.Empty, string.Empty);

        }

        //e.g., take the block beside the cup , which block?
        if (!colors.Contains(eventStrings[1]) && (targeObj.ToLower().Equals("block")) && relobjfound)
        {
            Debug.Log(string.Format("Nada : relation4 "));

            SetValue("me:speech:intent", string.Format("I can see several objects in relation with " + relationalObjName2 + ". Locate the block you meant?"), string.Empty);
            Debug.Log(string.Format("Nada : target block not specified "));
            return;
        }

        if (!colors.Contains(eventStrings[1]) && targeObj.Equals("block") && !relobjfound)
        {
            Debug.Log(string.Format("Nada : relation5 "));
            SetValue("me:speech:intent", string.Format("There is no " + relationalObjName2 + " here!"), string.Empty);
            logTocsv(nlu.GetUserSpeech(), "There is no " + relationalObjName2 + " here!", dp.getform(), RelationsLog(), dim.Configurations());

            Debug.Log(string.Format("Nada : target block not specified and relational object not found "));
            return;
        }
    }

    //----------------------------------HISTORICAL_REs--------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------------------


    //Nada: find a target from historical description like:
    //"bring the block you just moved" > bring(green(moved_adj(block)))
    void historical_REs(string[] eventStrings)
    {
        GameObject blocker = null;
        Stack<string> popedEventstack = new Stack<string>();
        Stack<string> popedObjHiststack = new Stack<string>();
        HistoricalRE = false;
        isHistorical = true;
        RE_Category = "Historical";
        string hist_event = "", targetName = "", lastEvent = "",
               lastObj = "", stro = "Objects History: ", stre = "Events History: ";
        bool found = false;
        int hist_event_index;

        //------------------------------------------------

        if (dim.getEventHist_stack() != null)
        {
            // TEST: print dialogue history in the debug log
            foreach (var o in dim.getobjHist_stack()) { stro = stro + " " + o; }
            Debug.Log("Nada:" + stro);

            foreach (var e in dim.getEventHist_stack()) { stre = stre + " " + e; }
            Debug.Log("Nada:" + stre);
            //------------------------------------------------
        }
        else
        {
            SetValue("me:speech:intent", string.Format("Sorry, we did not do anything yet, so I cannot remember this block!"), string.Empty);
            logTocsv(nlu.GetUserSpeech(), "Sorry, we did not do anything yet, so I cannot remember this block!", dp.getform(), RelationsLog(), dim.Configurations());
            return;
        }

        hist_event_index = Array.FindIndex(eventStrings, x => x.Contains("_adj"));
        //> retrieve element of index 0 =event
        string _event = eventStrings[0];
        //> retrieve the target object= last index and remove ))) from it
        string targeObj = eventStrings[eventStrings.Length - 1];
        targeObj = targeObj.Replace(")", "");

        if (hist_event_index == 2)
        {
            Debug.Log(string.Format("Nada : The index of historical event is 2..."));
            hist_event = eventStrings[2];
            //> retrieve element of index 1 =targetColor
            string targetColor = eventStrings[1];
            //> concat targetColor+TargetObj=target
            targetName = targetColor.Substring(0, 1).ToUpper() + targetColor.Substring(1, targetColor.Length - 1) + targeObj.Substring(0, 1).ToUpper() + targeObj.Substring(1, targeObj.Length - 1);
        }
        else if (hist_event_index == 1)
        {
            Debug.Log(string.Format("Nada : The index of historical event is 1..."));
            hist_event = eventStrings[1];
            targetName = targeObj.Substring(0, 1).ToUpper() + targeObj.Substring(1, targeObj.Length - 1);
        }

        hist_event = hist_event.Replace("_adj", "");
        Debug.Log(string.Format("Nada : targetObjName..." + targetName));
        GameObject target = GameObject.Find(targetName);


        //// Navigate the Dialogue history 
        while (dim.getEventHist_stack().Count > 0)
        {
            lastEvent = dim.getEventHist_stack().Pop();
            lastObj = dim.getobjHist_stack().Pop();

            Debug.Log("Nada: eventHist_stack.Count:  " + dim.getEventHist_stack().Count + " lastEvent: " + lastEvent + " lastObj: " + lastObj);


            if (hist_event.Contains(lastEvent) || lastEvent.Contains(hist_event))
            {
                Debug.Log(string.Format("Nada : enters eventHist_stack: lastEvent:  " + lastEvent + " and hist_event: " + hist_event));
                Debug.Log(string.Format("Nada : --outside--enters objHist_stack: lastObj " + lastObj + "and targetName: " + targetName));


                if (lastObj.Contains(targetName))
                {
                    Debug.Log(string.Format("Nada : --inside-- enters objHist_stack: lastObj " + lastObj + "and targetName: " + targetName));
                    Debug.Log(string.Format("Nada : The previous event and its object is tracked"));
                    // save the poped element to push them again to the main history stack
                    dh.getevtemp().Push(lastEvent);
                    dh.getobjtemp().Push(lastObj);
                    // compose the event and set it to the partialEvent
                    string partialEvent = _event + "(" + lastObj + ")";
                    Debug.Log(string.Format("Nada : partialEvent: " + partialEvent));


                    if (DialogueUtility.SurfaceClear(GameObject.Find(lastObj), out blocker) /*&& emm.getDisam()*/)
                    {
                        SetValue("user:intent:object", lastObj, string.Empty);
                        SetValue("user:intent:event", partialEvent, string.Empty);
                        found = true;
                        Dianafocustime = System.DateTime.Now.ToString("HH:mm:ss");
                        break;
                    }

                    else
                    {
                        SetValue("me:speech:intent", string.Format("I can't. It is blocked by another block."), string.Empty);
                        logTocsv(nlu.GetUserSpeech(), "I can't. It is blocked by another block.", dp.getform(), RelationsLog(), dim.Configurations());
                        found = true;
                        Dianafocustime = System.DateTime.Now.ToString("HH:mm:ss");
                        SetValue("me:intent:action", string.Empty, string.Empty);
                        SetValue("me:intent:lookAt", string.Empty, string.Empty);
                        break;
                    }
                }
            }
            else
            {
                // save the poped element to push them again to the main history stack
                dh.getevtemp().Push(lastEvent);
                dh.getobjtemp().Push(lastObj);
            }

        }


        if (found != true)
        {
            Debug.Log(string.Format("Nada : This previous event is not tracked in the dialogue history "));
            SetValue("me:speech:intent", string.Format("I can not remember this object"), string.Empty);
            logTocsv(nlu.GetUserSpeech(), "I can not remember this object", dp.getform(), RelationsLog(), dim.Configurations());
        }

    }

    //Nada: handle any unknown events
    public bool UnknownEvents(string eventStr)
    {
        bool isink = false;
        if (!string.IsNullOrEmpty(eventStr))
        {
            string checkevent = eventStr.Replace(")", "");
            if (checkevent.Contains("(("))
            {
                checkevent = checkevent.Replace("((", "(");
            }
            if (checkevent.Contains(","))
            {
                checkevent = checkevent.Replace(",", "(");
            }
            if (checkevent.Contains("{0}"))
            {
                checkevent = checkevent.Replace("{0}", "o");
            }
            if (checkevent.Contains("{1}"))
            {
                checkevent = checkevent.Replace("{1}", "l");
            }
            if (checkevent.Contains("{2}"))
            {
                checkevent = checkevent.Replace("{2}", "a");
            }
            if (checkevent.Contains("_adj"))
            {
                checkevent = checkevent.Replace("_adj", "");
            }
            if (checkevent.Contains("+"))
            {
                checkevent = checkevent.Replace("+", "(");
            }
            if (checkevent.Contains(";"))
            {
                checkevent = checkevent.Replace(";", "").Replace("<", "").Replace(">", "");
            }
            string[] checkevent1 = checkevent.Split('(');
            for (int entries = 0; entries < checkevent1.Length; entries++)
            {
                Debug.Log("checkevent1: " + checkevent1[entries]);
                //Debug.Log(string.Format(checkevent1[entries]));
                if (!checkevent1[entries].Equals(""))
                {
                    if ((!_actions.Contains(checkevent1[entries])) && (!objects.Contains(checkevent1[entries]))
                        && (!dublicated_blocks1.Contains(checkevent1[entries])) && (!colors.Contains(checkevent1[entries]))
                        && (!_relations.Contains(checkevent1[entries])) && (!_objectVars.Contains(checkevent1[entries]))
                        && (!_demoVars.Contains(checkevent1[entries])) && (!_anaphorVars.Contains(checkevent1[entries])))
                    {
                        if (!checkevent1[entries].Any(c => char.IsDigit(c)))
                        {
                            string intentObject = "";
                            //proceed = false;
                            //unknown = checkevent1[entries];
                            //if (!string.IsNullOrEmpty(unknown))
                            //{
                            //SetValue("me:speech:intent", "I am not sure what " + unknown + " means!", string.Empty);
                            // if (colors.Contains(checkevent1[entries])) {
                            if (nlu.GetUserSpeech().Contains("close") || nlu.GetUserSpeech().Contains("closest") || nlu.GetUserSpeech().Contains("to your left")
                                || nlu.GetUserSpeech().Contains("to your right") || nlu.GetUserSpeech().Contains("in front of you") || nlu.GetUserSpeech().Contains("to your front") ||
                                nlu.GetUserSpeech().Contains("frontmost") || nlu.GetUserSpeech().Contains("backmost") || nlu.GetUserSpeech().Contains("rightmost") || nlu.GetUserSpeech().Contains("leftmost"))
                            {
                                SetValue("me:speech:intent", "Can you Point to the object?   and please do not refer to objects in relation to me or the scene.", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "relation to Diana: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }

                            else if (nlu.GetUserSpeech().Contains("small") || nlu.GetUserSpeech().Contains("large") || nlu.GetUserSpeech().Contains("big"))
                            {
                                SetValue("me:speech:intent", "Blocks have the same size.Use pointing or relations for description.", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "relation to Diana: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }
                            else if (nlu.GetUserSpeech().Contains("red"))
                            {
                                intentObject = "red";
                                SetValue("me:speech:intent", "It seems you need a " + intentObject + " block. Which " + intentObject + " block you meant?", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "Known Color: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }
                            else if (nlu.GetUserSpeech().Contains("green"))
                            {
                                intentObject = "green";
                                SetValue("me:speech:intent", "This is still complex to understand. But I think you need " + intentObject + " block. Which " + intentObject + " block you meant?", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "Known Color: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }
                            else if (nlu.GetUserSpeech().Contains("blue"))
                            {
                                intentObject = "blue";
                                SetValue("me:speech:intent", "Sorry! I could not get this prompt. But if I am correct, you want the " + intentObject + " block. Which " + intentObject + " block you meant?", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "Only Known Color: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }
                            else if (nlu.GetUserSpeech().Contains("yellow"))
                            {
                                intentObject = "yellow";
                                SetValue("me:speech:intent", "I cannot do this, sorry. I am still learning from you. But you said " + intentObject + " block. Which " + intentObject + " block you meant?", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "Known Color: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }
                            else if (nlu.GetUserSpeech().Contains("pink"))
                            {
                                intentObject = "pink";
                                SetValue("me:speech:intent", "I am not able to interpret this yet. However, you said " + intentObject + " block. Which " + intentObject + " block you meant?", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "Known Color: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }
                            else if (nlu.GetUserSpeech().Contains("block"))
                            {
                                SetValue("me:speech:intent", "I could not find this block here. You might have spilling mistakes. If not, point to this block please!", string.Empty);
                            }
                            else if ((!trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && (eventStr.Contains("cup") || eventStr.Contains("plate"))) ||
                            (trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && (eventStr.Split(',')[0].Contains("cup") || eventStr.Split(',')[0].Contains("plate")))
                            || (trans_goal.Contains(GlobalHelper.GetTopPredicate(eventStr)) && ((eventStr.Split(',')[1].Contains("on") && !eventStr.Split(',')[1].Contains("front")
                            && eventStr.Split(',')[1].Contains("cup")) || (eventStr.Split(',')[1].Contains("on") && !eventStr.Split(',')[1].Contains("front") && eventStr.Split(',')[1].Contains("plate")))))
                            {
                                SetValue("me:speech:intent", string.Format("This is a landmark. Please select a block only!"), string.Empty);
                            }
                            else
                            {
                                SetValue("me:speech:intent", "I am not sure what you are referring to. Say that in different way please! ", string.Empty);
                                logTocsv(nlu.GetUserSpeech(), "UnKnown Color: I am not sure what you are referring to. Say that in different way please!", dp.getform(), RelationsLog(), dim.Configurations());
                            }



                            isink = true;

                        }
                        else
                        {
                            isink = false;
                        }

                    }
                }
            }
        }
        return isink;
    }

    // Diana checks to see if the user is still servoing
    void CheckServoStatus(string key, DataStore.IValue value)
    {
        bool val = (value as DataStore.BoolValue).val;

        // if me:checkServo is true, and me:actual:eventCompleted is move (E2 of servo)
        if ((val) &&
            (!string.IsNullOrEmpty(DataStore.GetStringValue("me:actual:eventCompleted")) &&
            (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("me:actual:eventCompleted")) == "move")))
        {
            // if there are no other events to be executed
            if (eventManager.events.Count == 0)
            {
                // clear append:partialEvent
                SetValue("user:intent:append:partialEvent", string.Empty, string.Empty);

                // clear and set the appropriate value of append:action
                if (DataStore.GetBoolValue("user:intent:isServoLeft"))
                {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(left))", string.Empty);
                }
                else if (DataStore.GetBoolValue("user:intent:isServoRight"))
                {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(right))", string.Empty);
                }
                else if (DataStore.GetBoolValue("user:intent:isServoFront"))
                {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(front))", string.Empty);
                }
                else if (DataStore.GetBoolValue("user:intent:isServoBack"))
                {
                    SetValue("user:intent:append:action", string.Empty, string.Empty);
                    SetValue("user:intent:append:action", "servo({0},{1}(back))", string.Empty);
                }
            }
        }

        // no longer checking servo
        SetValue("me:isCheckingServo", false, string.Empty);
    }

    // infer a target location based on an object, motion, and directional "hint"
    Vector3 InferTargetLocation(string pred, GameObject theme, string dir)
    {
        Vector3 loc = theme.transform.position;
        // different methods for slide and servo
        switch (pred)
        {
            case "slide":
                loc = CalcSlideTarget(theme, dir);
                Debug.Log(string.Format("Slide target calculated as {0}", GlobalHelper.VectorToParsable(loc)));
                break;

            case "servo":
                loc = CalcServoTarget(theme, dir);
                break;

            default:
                break;
        }

        return loc;
    }

    // calculates a target for a slide event given a theme and direction
    Vector3 CalcSlideTarget(GameObject theme, string dir)
    {
        Vector3 loc = theme.transform.position;

        List<GameObject> options = new List<GameObject>();
        GameObject choice = null;

        // every manipulable object could be an option
        foreach (Transform child in grabbableBlocks)
        {
            if (child.gameObject.activeInHierarchy)
            {
                options.Add(child.gameObject);
            }
        }

        switch (dir)
        {
            // for each direction
            //  remove options where the dot product of the direction vector between that object and theme and the directon vector of the supplied
            //      direction is not > 0.5, and where the theme object would not fit next to that object in the supplied direction
            //  then order the remaining options in descending order, first by the dot product calc'ed above, then by the distance between the theme
            //      and the object in question, then choose the first one
            //  if an object was chosen, slide the theme agains that object
            //  if no object was chosen (i.e., no object meets the selection criteria
            //      slide the theme to a randomly chosen point in that direction
            case "left":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.4f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();

                choice = options.OrderByDescending(o =>
                    Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    Debug.Log(string.Format("Sliding {0} against {1}", theme.name, choice.name));
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice, true).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme, true).extents,
                        directionVectors[dir]);
                    loc = new Vector3(loc.x, loc.y + (GlobalHelper.GetObjectWorldSize(theme, true).extents.y - GlobalHelper.GetObjectWorldSize(choice, true).extents.y),
                        loc.z);
                }
                else
                {
                    Debug.Log(string.Format("Sliding {0} to {1}", theme.name, dir));
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    Region targetRegion = new Region(new Vector3(theme.transform.position.x, surfaceBounds.max.y, themeBounds.max.z),
                        new Vector3(surfaceBounds.max.x, surfaceBounds.max.y, themeBounds.min.z));
                    Region clearRegion = GlobalHelper.FindClearRegion(demoSurface, targetRegion, theme);
                    loc = new Vector3(clearRegion.center.x, theme.transform.position.y, clearRegion.center.z);
                }
                break;

            case "right":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.4f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();

                choice = options.OrderByDescending(o =>
                    Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    Debug.Log(string.Format("Sliding {0} against {1}", theme.name, choice.name));
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice, true).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme, true).extents,
                        directionVectors[dir]);
                    loc = new Vector3(loc.x, loc.y + (GlobalHelper.GetObjectWorldSize(theme, true).extents.y - GlobalHelper.GetObjectWorldSize(choice, true).extents.y),
                        loc.z);
                }
                else
                {
                    Debug.Log(string.Format("Sliding {0} to {1}", theme.name, dir));
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    Region targetRegion = new Region(new Vector3(surfaceBounds.min.x, surfaceBounds.max.y, themeBounds.min.z),
                        new Vector3(theme.transform.position.x, surfaceBounds.max.y, themeBounds.max.z));
                    Region clearRegion = GlobalHelper.FindClearRegion(demoSurface, targetRegion, theme);
                    loc = new Vector3(clearRegion.center.x, theme.transform.position.y, clearRegion.center.z);
                }
                break;

            case "front":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.4f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();

                choice = options.OrderByDescending(o =>
                    Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    Debug.Log(string.Format("Sliding {0} against {1}", theme.name, choice.name));
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice, true).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme, true).extents,
                        directionVectors[dir]);
                    loc = new Vector3(loc.x, loc.y + (GlobalHelper.GetObjectWorldSize(theme, true).extents.y - GlobalHelper.GetObjectWorldSize(choice, true).extents.y),
                        loc.z);
                }
                else
                {
                    Debug.Log(string.Format("Sliding {0} to {1}", theme.name, dir));
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    Region targetRegion = new Region(new Vector3(themeBounds.min.x, surfaceBounds.max.y, surfaceBounds.min.z),
                        new Vector3(themeBounds.max.x, surfaceBounds.max.y, theme.transform.position.z));
                    Region clearRegion = GlobalHelper.FindClearRegion(demoSurface, targetRegion, theme);
                    loc = new Vector3(clearRegion.center.x, theme.transform.position.y, clearRegion.center.z);
                }
                break;

            case "back":
                options = options.Where(o =>
                    ((Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]]) > 0.4f) &&
                    (DialogueUtility.FitsTouching(theme, grabbableBlocks, o, dir)))).ToList();

                choice = options.OrderByDescending(o =>
                    Vector3.Dot(Vector3.Normalize(o.transform.position - theme.transform.position), directionVectors[oppositeDir[dir]])).
                    ThenBy(o => (o.transform.position - theme.transform.position).magnitude).FirstOrDefault();

                if (choice != null)
                {
                    Debug.Log(string.Format("Sliding {0} against {1}", theme.name, choice.name));
                    // slide against the side of chosen block
                    loc = choice.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(choice, true).extents,
                        directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme, true).extents,
                        directionVectors[dir]);
                    loc = new Vector3(loc.x, loc.y + (GlobalHelper.GetObjectWorldSize(theme, true).extents.y - GlobalHelper.GetObjectWorldSize(choice, true).extents.y),
                        loc.z);
                }
                else
                {
                    Debug.Log(string.Format("Sliding {0} to {1}", theme.name, dir));
                    // choose location in that direction on table
                    Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(demoSurface);
                    Region targetRegion = new Region(new Vector3(themeBounds.min.x, surfaceBounds.max.y, theme.transform.position.z),
                        new Vector3(themeBounds.max.x, surfaceBounds.max.y, surfaceBounds.max.z));
                    Region clearRegion = GlobalHelper.FindClearRegion(demoSurface, targetRegion, theme);
                    loc = new Vector3(clearRegion.center.x, theme.transform.position.y, clearRegion.center.z);
                }
                break;

            default:
                break;
        }

        return loc;
    }

    // calculates a target for a servo event given a theme and direction
    Vector3 CalcServoTarget(GameObject theme, string dir)
    {
        // calc the projected next location
        Vector3 loc = theme.transform.position + (directionVectors[oppositeDir[dir]] * servoSpeed);

        // calc the projected bounds of the theme object at that location
        Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
        Bounds projectedBounds = new Bounds(loc, themeBounds.size);

        // now, for each manipulable object
        foreach (Transform test in grabbableBlocks)
        {
            // if it's active
            if (test.gameObject.activeInHierarchy)
            {
                // and not the same as the theme
                if (test.gameObject != theme.gameObject)
                {
                    // if the projected bounds are not DisConnected or Externally Connected
                    //  from the test object (i.e., they interpenetrate somehow
                    //  stop the servo and place the theme object flush with that object in the
                    //  appropriate direction
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject)))
                    {
                        if ((dir == "left") || (dir == "right"))
                        {
                            Debug.Log(string.Format("Stopping servo-{0} ({1}): Running up against {2}", dir, theme.name, test.gameObject.name));
                            loc = new Vector3(
                                (test.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(test.gameObject).extents,
                                directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme.gameObject).extents,
                                directionVectors[dir])).x, loc.y, loc.z);
                        }
                        else if ((dir == "front") || (dir == "back"))
                        {
                            Debug.Log(string.Format("Stopping servo-{0} ({1}): Running up against {2}", dir, theme.name, test.gameObject.name));
                            loc = new Vector3(loc.x, loc.y,
                                (test.transform.position + Vector3.Scale(GlobalHelper.GetObjectWorldSize(test.gameObject).extents,
                                directionVectors[dir]) + Vector3.Scale(GlobalHelper.GetObjectWorldSize(theme.gameObject).extents,
                                directionVectors[dir])).z);
                        }
                    }
                }
            }
        }

        // return the calculated direction
        return loc;
    }

    // on change to me:actual:action, what are the consequences of having completed that action?
    void CheckEventExecutionStatus(string key, DataStore.IValue value)
    {
        // if the value is empty, we effectively reset
        // it the value is "grasp", reset but keep the indicated object on the blackboard
        if (((value as DataStore.StringValue).val == string.Empty) ||
            ((value as DataStore.StringValue).val == "grasp"))
        {
            SetValue("user:intent:event", DataStore.StringValue.Empty, string.Empty);
            SetValue("user:intent:partialEvent", DataStore.StringValue.Empty, string.Empty);

            if ((value as DataStore.StringValue).val == string.Empty)
            {
                SetValue("user:intent:object", DataStore.StringValue.Empty, string.Empty);
            }
            SetValue("user:intent:action", DataStore.StringValue.Empty, string.Empty);
            SetValue("user:intent:location", DataStore.Vector3Value.Zero, string.Empty);

            SetValue("user:intent:append:partialEvent", DataStore.StringValue.Empty, string.Empty);
            SetValue("user:intent:append:action", DataStore.StringValue.Empty, string.Empty);

            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:lastEvent")))
            {
                if (DataStore.GetStringValue("user:intent:lastEvent").StartsWith("servo"))
                {
                    SetValue("me:intent:target", "user", string.Empty);
                }
            }

            if (DataStore.GetBoolValue("me:isUndoing"))
            {
                SetValue("user:intent:lastEvent", DataStore.StringValue.Empty, string.Empty);
                SetValue("me:isUndoing", false, string.Empty);
            }
            else
            {
                SetValue("user:intent:replaceContent", string.Empty, string.Empty);
            }
        }
    }

    // assign a newly-learned gesture to an instrution
    void AssignNewGesture(string key, DataStore.IValue value)
    {
        string gesture = (value as DataStore.StringValue).val;
        Debug.Log(string.Format("AssignNewGesture: received new custom gesture string \"{0}\"", gesture));

        if (gesture != string.Empty)
        {
            GameObject graspedObj = GameObject.Find(DataStore.GetStringValue("user:intent:object"));

            // if this gesture is a learnable instruction
            if (learnableInstructions.ContainsKey(gesture))
            {
                Debug.Log(string.Format("AssignNewGesture: \"{0}\" is a learnable instruction", gesture));
                Debug.Log(string.Format("AssignNewGesture: learnableInstructions[\"{0}\"] = {1}", gesture, learnableInstructions[gesture]));
                // if it's not already in use
                if (learnableInstructions[gesture] == string.Empty)
                {
                    // assign this gesture to grasp the current object in future
                    Debug.Log(string.Format("AssignNewGesture: assigning custom gesture \"{0}\" to instruction {1}", gesture, string.Format("grasp({0})", graspedObj.name)));
                    learnableInstructions[gesture] = string.Format("grasp({0})", graspedObj.name);
                    // if there's no follow-on action (i.e., affordance learning wasn't triggered as part of a
                    //  longer event sequence, exit learning
                    if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:append:event")))
                    {
                        SetValue("me:affordances:isLearning", false, "Learning completed");
                    }
                }
                // if not, complain about it
                else
                {
                    SetValue("me:intent:lookAt", "user", string.Empty);
                    SetValue("me:speech:intent", "That gesture already means something else.",
                        string.Format("{0} is already used for {1}", gesture, learnableInstructions[gesture]));
                }

                SetValue("me:speech:intent", string.Format("Got it!  Do that and I'll grasp the {0} like this.", graspedObj.name.ToLower()), "Learning succeeded.");

                // clear oneshot:newGesture
                SetValue("me:oneshot:newGesture", string.Empty, string.Empty);
                SetValue("me:affordances:isLearning", false, string.Empty);
            }
            else
            {
                Debug.Log(string.Format("AssignNewGesture: \"{0}\" is not a learnable instruction!", gesture));
            }
        }
    }

    // check to see if the user made a learned gesture
    //  triggered on changes to user:hands:[left,right]
    void CheckCustomGesture(string key, DataStore.IValue value)
    {
        string gesture = (value as DataStore.StringValue).val;

        // if you see it, and it's a learnable instruction with an event associated
        //  stick that event in partialEvent and go
        if (learnableInstructions.ContainsKey(gesture))
        {
            Debug.Log(string.Format("CheckCustomGesture: Saw custom gesture {0} on {1}", gesture, key));
            if (learnableInstructions[gesture] != string.Empty)
            {
                Debug.Log(string.Format("CheckCustomGesture: setting user:intent:partialEvent to {0}", learnableInstructions[gesture]));
                SetValue("user:intent:partialEvent", learnableInstructions[gesture], string.Empty);
            }
        }
    }

    // forget learned affordances
    void ForgetLearnedAffordances(string key, DataStore.IValue value)
    {
        if ((value as DataStore.BoolValue).val)
        {
            learnableInstructions.Keys.ToList().ForEach(x => learnableInstructions[x] = string.Empty);
            SetValue("me:affordances:forget", false, string.Empty);
        }
    }

    // this is called when an entity name is extracted from an event formula
    //  callback from VoxSim EntityReferenced event
    public void EntityReferenced(object sender, EventArgs e)
    {
        if (((EventReferentArgs)e).Referent is string)
        {
            // if currently executing an event
            //  (e.g., if user:intent:event is not empty
            //  and is the same as the first (current) event in event manager)
            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:event")) && (eventManager.events.Count > 0) &&
                (GlobalHelper.GetTopPredicate(eventManager.events[0]) ==
                    GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:event"))))
            {
                // if this object falls directly under the scope of the event's main predicate
                if ((((EventReferentArgs)e).Predicate as string) ==
                    GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:event")))
                {
                    // set lastTheme and its position
                    string referentStr = ((EventReferentArgs)e).Referent as string;
                    Debug.Log(string.Format("Setting lastTheme to {0}, lastThemePos to {1}", referentStr,
                        GlobalHelper.VectorToParsable(GameObject.Find(referentStr).transform.position)));
                    SetValue("me:lastTheme", referentStr, string.Empty);
                    SetValue("me:lastThemePos", GameObject.Find(referentStr).transform.position, string.Empty);

                    // if no object is currently indicated
                    //  set user:intent:object
                    if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
                    {
                        GameObject target = GameObject.Find(referentStr);
                        GameObject blocker;
                        bool surfaceClear = DialogueUtility.SurfaceClear(target, out blocker);

                        if (surfaceClear)
                        {
                            SetValue("user:intent:object", referentStr, string.Empty);
                        }
                        else
                        {
                            List<object> targetUniqueAttrs =
                                GlobalHelper.DiffLists(blocker.GetComponent<AttributeSet>().attributes.Cast<object>().ToList(),
                                target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList());
                            List<object> blockerUniqueAttrs =
                                GlobalHelper.DiffLists(target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList(),
                                blocker.GetComponent<AttributeSet>().attributes.Cast<object>().ToList());

                            SetValue("me:speech:intent", string.Format("I can't access the {0} {1}.  The {2} {3} is on top of it.",
                                targetUniqueAttrs[0], target.GetComponent<Voxeme>().voxml.Lex.Pred,
                                blockerUniqueAttrs[0], blocker.GetComponent<Voxeme>().voxml.Lex.Pred),
                                string.Empty);
                            eventManager.AbortEvent();
                        }
                    }
                }
            }
            // if currently executing an event
            //  (e.g., if user:intent:append:event is not empty
            //  and is the same as the first (current) event in event manager)
            else if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:append:event")) && (eventManager.events.Count > 0) &&
                (GlobalHelper.GetTopPredicate(eventManager.events[0]) ==
                    GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:append:event"))))
            {
                // if this object falls directly under the scope of the event's main predicate
                if ((((EventReferentArgs)e).Predicate as string) ==
                    GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:event")))
                {
                    if (!string.IsNullOrEmpty("user:intent:replaceContent"))
                    {
                        string referentStr = ((EventReferentArgs)e).Referent as string;
                        Debug.Log(string.Format("Setting lastTheme to {0}, lastThemePos to {1}", referentStr,
                            GlobalHelper.VectorToParsable(GameObject.Find(referentStr).transform.position)));
                        SetValue("me:lastTheme", referentStr, string.Empty);
                        SetValue("me:lastThemePos", GameObject.Find(referentStr).transform.position, string.Empty);

                        // if no object is currently indicated
                        //  set user:intent:object
                        if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
                        {
                            GameObject target = GameObject.Find(referentStr);
                            GameObject blocker;
                            bool surfaceClear = DialogueUtility.SurfaceClear(target, out blocker);

                            if (surfaceClear)
                            {
                                SetValue("user:intent:object", referentStr, string.Empty);
                            }
                            else
                            {
                                List<object> targetUniqueAttrs =
                                    GlobalHelper.DiffLists(blocker.GetComponent<AttributeSet>().attributes.Cast<object>().ToList(),
                                    target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList());
                                List<object> blockerUniqueAttrs =
                                    GlobalHelper.DiffLists(target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList(),
                                    blocker.GetComponent<AttributeSet>().attributes.Cast<object>().ToList());

                                SetValue("me:speech:intent", string.Format("I can't access the {0} {1}.  The {2} {3} is on top of it.",
                                    targetUniqueAttrs[0], target.GetComponent<Voxeme>().voxml.Lex.Pred,
                                    blockerUniqueAttrs[0], blocker.GetComponent<Voxeme>().voxml.Lex.Pred),
                                    string.Empty);
                            }
                        }
                    }
                }
            }
        }
    }

    // call back from VoxSim UnhandledArgment error
    //  in this case, the unhandled argment might be a prop-word variable ("{2}")
    public string TryPropWordHandling(string predStr)
    {
        Debug.Log(string.Format("VoxSim hit an UnhandledArgument error with {0}!", predStr));

        string fillerList = string.Empty;

        // it might contain a prop-word
        if (predStr.Contains("{2}"))
        {
            // populate the list of potential fillers with the names of active grabbable blocks
            fillerList = string.Join(",", grabbableBlocks.GetComponentsInChildren<Rigging>().Where(r => r.isActiveAndEnabled && r.gameObject.tag == "Block").Select(
                o => o.gameObject.name));
        }

        return fillerList;
    }

    // call back from VoxSim ObjectMatchingConctraint error
    //  remove all objects that are not accessible
    public List<GameObject> RemoveInaccessibleObjects(List<GameObject> matches, MethodInfo referringMethod)
    {
        Debug.Log(string.Format("VoxSim hit an ObjectMatchingConstraint event with {0}!", string.Format("[{0}]", string.Join(",", matches))));

        List<GameObject> prunedMatches = new List<GameObject>(matches);

        // TODO: DeferredEvaluation is basically VoxML-ish for indefinite predicates
        if (referringMethod.GetCustomAttributes(typeof(DeferredEvaluation), false).ToList().Count > 0)
        {
            // for each object, if surface is not clear, remove it
            List<GameObject> toRemove = new List<GameObject>();
            foreach (GameObject match in prunedMatches)
            {
                GameObject blocker = null;

                if (!DialogueUtility.SurfaceClear(match, out blocker))
                {
                    toRemove.Add(match);
                }
            }

            if (eventManager.referents.stack.Count > 0)
            {
                toRemove.Add(GameObject.Find((string)eventManager.referents.stack.Peek()));
            }

            prunedMatches = prunedMatches.Except(toRemove).ToList();
        }

        return prunedMatches;
    }

    // this is called when the user talks about some entity that doesn't exist in this scene
    //  callback from VoxSim NonexistentEntityError event
    public void NonexistentReferent(object sender, EventArgs e)
    {
        //if (((EventReferentArgs)e).Referent.ToString().Any(c => char.IsDigit(c)))
        //{
        //((EventReferentArgs)e).Referent.ToString().Replace(")","");

        Debug.Log(string.Format("Nada: ref: {0}", ((EventReferentArgs)e).Referent.ToString().Replace(")", "")));

        Debug.Log(string.Format("Nada:{0}", ((EventReferentArgs)e).Referent is Pair<string, List<object>>));
        if (((EventReferentArgs)e).Referent is Pair<string, List<object>>)
        {
            // pair of predicate and object list 
            // (if the referent is of a present type - this is the common type of object list,
            //  if the referent has an attrbute that is absent attribute - this is the predicate)
            string pred = ((Pair<string, List<object>>)((EventReferentArgs)e).Referent).Item1;
            List<object> objs = ((Pair<string, List<object>>)((EventReferentArgs)e).Referent).Item2;
            // foreach (object o in objs) {
            Debug.Log(string.Format("Nada: objs[0] {0} .GetComponent<Voxeme> {1}", objs[0], (objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred.ToString()));
            Debug.Log(string.Format("Nada: objs.count {0} .GetComponent<Voxeme> {1}", objs.Count, (objs[1] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred.ToString()));

            //}

            if (objs.Count > 0)
            {
                Debug.Log(string.Format("Nada: objs.count inside2 {0} .GetComponent<Voxeme> {1}", objs.Count, (objs[1] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred.ToString()));

                // if objs not null or objs with the type of GameObject
                if (!objs.Any(o => (o == null) || (o.GetType() != typeof(GameObject))))
                {
                    Debug.Log(string.Format("Nada inside3: objs.count {0} .GetComponent<Voxeme> {1}", objs.Count, (objs[1] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred.ToString()));

                    // if all objects are game objects
                    string responseStr = string.Empty;
                    //Nada: add relationalRE 
                    if ((pred == "this" || pred == "that")
                        && (relationalRE == false))
                    //&& (String.IsNullOrEmpty(DataStore.GetStringValue("user:lastPointedAt:name")))
                    //&& (String.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object"))))
                    {
                        responseStr = string.Format("Which {0}?", (objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred);
                        //Debug.Log(string.Format("Nada: objs[0] {0} .GetComponent<Voxeme> {1}", objs[0], (objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred.ToString()));
                    }
                    else
                    {
                        // an object of that description does not exist in the scene
                        Debug.Log(string.Format("Nada:object not exist: {0} {1} does not exist!", pred,
                            (objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred));
                        responseStr = string.Format("There is no {0} {1} here.", pred,
                            (objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred);
                    }

                    // look at the user and be confused
                    SetValue("me:intent:lookAt", "user", string.Empty);
                    SetValue("me:speech:intent", responseStr, string.Empty);
                    logTocsv(nlu.GetUserSpeech(), responseStr, dp.getform(), RelationsLog(), dim.Configurations());

                    SetValue("me:emotion", "confusion", string.Empty);
                }
            }
        }
        // }
        // absent object type - string
        else if (((EventReferentArgs)e).Referent is string)
        {

            // if the referent is a variable - skip
            if (Regex.IsMatch(((EventReferentArgs)e).Referent as string, @"\{.+\}"))
            {
                //if ((((EventReferentArgs)e).Referent as string).Contains(")"))
                //{
                //    (((EventReferentArgs)e).Referent as string).Replace(")", "");
                //}

                return;
            }

            // if it's a vector - skip
            if (Regex.IsMatch(((EventReferentArgs)e).Referent as string, GlobalHelper.vec.ToString()))
            {
                return;
            }
            // no object of that type exists in the scene
            string responseStr = string.Format("There is no {0} here.", ((EventReferentArgs)e).Referent as string);

            // look at the user and be confused
            SetValue("me:intent:lookAt", "user", string.Empty);
            SetValue("me:speech:intent", responseStr, string.Empty);
            logTocsv(nlu.GetUserSpeech(), responseStr, dp.getform(), RelationsLog(), dim.Configurations());
            SetValue("me:emotion", "confusion", string.Empty);

        }
        exis = true;
    }

    // a position that is inavlid for this action was indicated
    //  callback from VoxSim InvalidPositionError event
    public void InvalidPosition(object sender, EventArgs e)
    {
        if (e != null)
        {

        }
        else
        {
            string responseStr = string.Format("That's not a valid place.");
            SetValue("me:intent:lookAt", "user", string.Empty);
            SetValue("me:speech:intent", responseStr, string.Empty);
            logTocsv(nlu.GetUserSpeech(), responseStr, dp.getform(), RelationsLog(), dim.Configurations());
            SetValue("me:emotion", "confusion", string.Empty);
            //TODO: expand on why
        }
    }

    // Nada: callback from VoxSim DisambiguationError event

    public void AskForDisambiguation(object sender, EventArgs e)
    {
        List<object> SpeechTargetAttr = null;
        List<object> PointingTargetAttr = null;
        dis = true;
        GameObject AmbigBlock = null;
        GameObject target = GameObject.Find(DataStore.GetStringValue("user:intent:object"));

        Debug.Log(string.Format("DISAMB: Nada: DIM: ENTERED: {0}", dis.ToString()));

        string responseStr = "";

        // attributes of the target mention in the speech 

        Debug.Log(string.Format("Nada:disambiguation 2: event: {0}, AmbiguityStr: {1}, AmbiguityVar: {2}, Candidates: {3}",
                ((EventDisambiguationArgs)e).Event, ((EventDisambiguationArgs)e).AmbiguityStr, ((EventDisambiguationArgs)e).AmbiguityVar, string.Join(",", ((EventDisambiguationArgs)e).Candidates)));
        string Ambigstr = (((EventDisambiguationArgs)e).AmbiguityStr).Replace("the", "").Replace("(", "").Replace(")", "");
        Ambigstr = Ambigstr.Split(',')[0];
        ambigBlock = Ambigstr;
        AmbigBlock = GameObject.Find(ambigBlock);
        SpeechTargetAttr = AmbigBlock.GetComponent<AttributeSet>().attributes.Cast<object>().ToList();
        ambigEvent = ((EventDisambiguationArgs)e).Event;
        bool isDigitPresent = Ambigstr.Any(c => char.IsDigit(c));

        if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")) && !transit)
        {
            PointingTargetAttr = target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList();
            Debug.Log(string.Format("Nada:disambiguation 1"));

            // if user did not point to the ambiguate block: if pointing value is null
            // or the pointed at obj is not the same as the intented object

            //if (!PointingTargetAttr[0].Equals(SpeechTargetAttr[0]))
            //{
            Debug.Log(string.Format("Nada:disambiguation 2"));
            //if (isDigitPresent)
            //{
            //    Debug.Log(string.Format("Nada:disambiguation 3"));
            //    responseStr = string.Format("This is not a {0}. "/* Which {1} you are referring to?"*/, Ambigstr.Substring(0, Ambigstr.Length - 1)/*, Ambigstr.Substring(0, Ambigstr.Length - 1)*/);
            //}
            //else
            //{
            //    Debug.Log(string.Format("Nada:disambiguation 4"));
            //    responseStr = string.Format("This is not a {0}. "/*Which {1} you are referring to?"*/, Ambigstr.Substring(0, Ambigstr.Length - 1)/*, Ambigstr.Substring(0, Ambigstr.Length - 1)*/);
            //}
            SetValue("me:intent:lookAt", "user", string.Empty);
            SetValue("me:speech:intent", responseStr, string.Empty);
            SetValue("me:emotion", "confusion", string.Empty);
            SetValue("user:intent:object", string.Empty, string.Empty);
            //}
            //else
            //{
            //    //Debug.Log(string.Format("Nada:disambiguation 5"));
            //    SetValue("me:speech:intent", "OK!", string.Empty);
            //    SetValue("user:intent:object", DataStore.GetStringValue("user:lastPointedAt:name"), string.Empty);
            //    dim.setfocuspointing("After Ask for Disambiguation");
            //    dim.settargetpointing("None");
            //    // to save data in csv file 
            //    dim.setfocusObject(DataStore.GetStringValue("user:intent:object"));
            //    dim.settargetObject("None");
            //    dim.AddToDialogueHist();
            //    Debug.Log(string.Format("Nada: emm: focusObject: {0} ", dim.getfocusObject() ));

            //}
        }
        else
        {
            //Debug.Log(string.Format("Nada:disambiguation 6"));
            Disambiguate(isDigitPresent, Ambigstr);
            //dis = true;
        }
    }

    //public void AskForDisambiguation(object sender, EventArgs e)
    //{
    //    string responseStr = "";
    //    //if (!HistoricalRE && !relationalRE)
    //    //{

    //    Debug.Log(string.Format("Nada:disambiguation 2: event: {0}, AmbiguityStr: {1}, AmbiguityVar: {2}, Candidates: {3}",
    //        ((EventDisambiguationArgs)e).Event, ((EventDisambiguationArgs)e).AmbiguityStr, ((EventDisambiguationArgs)e).AmbiguityVar, string.Join(",", ((EventDisambiguationArgs)e).Candidates)));

    //    string Ambigstr = (((EventDisambiguationArgs)e).AmbiguityStr).Replace("the", "").Replace("(", "").Replace(")", "");
    //    ambigEvent = ((EventDisambiguationArgs)e).Event;
    //    bool isDigitPresent = Ambigstr.Any(c => char.IsDigit(c));
    //    Ambigstr = Ambigstr.Split(',')[0];
    //    ambigBlock = Ambigstr;


    //    //if (string.IsNullOrEmpty(DataStore.GetStringValue("user:lastPointedAt:name")) || (!DataStore.GetStringValue("user:intent:object").Equals(Ambigstr)))
    //    //{
    //        if (isDigitPresent)
    //        {

    //            responseStr = string.Format("Which {0}?", Ambigstr.Substring(0, Ambigstr.Length - 1));
    //        }
    //        else
    //        {
    //            responseStr = string.Format("Which {0}?", Ambigstr);
    //        }

    //        SetValue("me:intent:lookAt", "user", string.Empty);
    //        SetValue("me:speech:intent", responseStr, string.Empty);
    //        SetValue("me:emotion", "confusion", string.Empty);
    //        dis = true;
    //   // }
    //}

    public void Disambiguate(bool isdigit, string Ambigstr)
    {
        string responseStr = "";
        if (isdigit)
        {
            responseStr = string.Format("Which {0}?", Ambigstr.Substring(0, Ambigstr.Length - 1), Ambigstr.Substring(0, Ambigstr.Length - 1));
        }
        else
        {
            responseStr = string.Format("Which {0}?", Ambigstr.Substring(0, Ambigstr.Length - 1), Ambigstr.Substring(0, Ambigstr.Length - 1));
        }
        //SetValue("me:intent:action", string.Empty, string.Empty);
        SetValue("me:intent:lookAt", "user", string.Empty);
        SetValue("me:speech:intent", responseStr, string.Empty);
        SetValue("me:emotion", "confusion", string.Empty);

    }

    // callback from VoxSim EventCompleted event
    public void EventCompleted(object sender, EventArgs e)
    {
        SetValue("me:actual:eventCompleted", ((EventManagerArgs)e).EventString, string.Empty);
    }

    // called when the event is complete and any discrepancies between the
    //  physics bodies/geometries and the voxeme have been resolved
    //  callback from VoxSim ResolveDiscrepanciesComplete event
    public void PhysicsDiscrepanciesResolved(object sender, EventArgs e)
    {
        // if there's an event that was just completed
        if (!string.IsNullOrEmpty(DataStore.GetStringValue("me:actual:eventCompleted")))
        {
            // start attending to pointing again
            SetValue("me:isAttendingPointing", true, string.Empty);
        }
    }

    // callback from VoxSim QueueEmpty event
    public void QueueEmpty(object sender, EventArgs e)
    {
        // start attending to pointing again
        SetValue("me:isAttendingPointing", true, string.Empty);
    }

    // Diana-specific GRASP implementation
    // overrides Predicates.GRASP
    // IN: Objects
    // OUT: none
    public void GRASP(object[] args)
    {
        if (args[args.Length - 1] is bool)
        {
            if ((bool)args[args.Length - 1] == true)
            {
                if (args[0] is GameObject)
                {
                    GameObject obj = (args[0] as GameObject);
                    Voxeme objVox = obj.GetComponent<Voxeme>();

                    // grasp events might trigger one-shot learning
                    // first, see if an existing grasp convention is assigned to this object
                    if (objVox.graspConvention != null)
                    {
                        // get all interaction targets
                        List<InteractionObject> graspPoses = obj.GetComponentsInChildren<Transform>(true)
                            .Where(t => t.gameObject.GetComponent<InteractionObject>() != null).Select(g => g.GetComponent<InteractionObject>()).ToList();

                        // deactivate all interaction objects except the grasp convention
                        foreach (InteractionObject graspPose in graspPoses)
                        {
                            if (graspPose.gameObject == objVox.graspConvention)
                            {
                                graspPose.gameObject.SetActive(true);
                            }
                            else
                            {
                                graspPose.gameObject.SetActive(false);
                            }
                        }

                        // grasp like this
                        RiggingHelper.UnRig(obj, obj.transform.parent.gameObject);
                        SetValue("me:intent:lookAt", obj.name, string.Empty);
                        SetValue("me:intent:targetObj", obj.name, string.Format("Starting interaction with {0}", obj.name));
                        SetValue("me:intent:action", "grasp", string.Empty);

                        // deactivate the object's physics
                        Rigging rigging = obj.GetComponent<Rigging>();
                        if (rigging != null)
                        {
                            rigging.ActivatePhysics(false);
                        }
                    }
                    // no existing grasp convention exists
                    // see how many interaction objects the theme has
                    else
                    {
                        InteractionTarget[] interactionTargets = obj.GetComponentsInChildren<InteractionTarget>();
                        Debug.Log(string.Format("{0} has {1} InteractionTarget components", obj.name, interactionTargets.Length));
                        // more than one possible grasp convention
                        if (interactionTargets.Length > 1)
                        {
                            // unrig the object from any parent
                            RiggingHelper.UnRig(obj, obj.transform.parent.gameObject);

                            // don't start affordance learning if we're already learning about affordances
                            if (!DataStore.GetBoolValue("me:affordances:isLearning"))
                            {
                                SetValue("me:affordances:isLearning", true, string.Empty);

                                // if there's a valid intended event
                                if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:event")))
                                {
                                    // if its top predicate == the top predicate of the most recently executed ebent
                                    if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:event")) ==
                                        GlobalHelper.GetTopPredicate(eventManager.eventHistory.Last()))
                                    {
                                        // then append the remainder of the event list so subsquent events
                                        //  will get executed after affordance learning is complete
                                        string toAppend = string.Join(multiAppendDelimiter,
                                            Enumerable.Reverse(eventManager.events).Take(eventManager.events.Count - 1).Reverse().ToList());

                                        // remove those events from the event list until affordance learning is done
                                        for (int i = eventManager.events.Count - 1; i > 0; i--)
                                        {
                                            eventManager.RemoveEvent(i);
                                        }
                                        SetValue("user:intent:append:event", toAppend, string.Empty);
                                        SetValue("user:intent:event", string.Empty, string.Empty);
                                    }
                                }
                            }

                            // suggest a grasp
                            SetValue("me:speech:intent", "Should I grasp it like this?", string.Empty);
                            SetValue("me:intent:lookAt", "user", string.Empty);
                            SetValue("me:intent:targetObj", obj.name, string.Format("Starting interaction with {0}", obj.name));
                            SetValue("me:intent:action", "grasp", string.Empty);

                            // because we're grasping, deactivate the object's physics
                            Rigging rigging = obj.GetComponent<Rigging>();
                            if (rigging != null)
                            {
                                rigging.ActivatePhysics(false);
                            }
                        }
                        // only one interaction object: default to this
                        else
                        {
                            // unrig the object from any parent
                            RiggingHelper.UnRig(obj, obj.transform.parent.gameObject);

                            // look at the object and grasp it
                            SetValue("me:intent:lookAt", obj.name, string.Empty);
                            SetValue("me:intent:targetObj", obj.name, string.Format("Starting interaction with {0}", obj.name));
                            SetValue("me:intent:action", "grasp", string.Empty);

                            // deactivate the object's physics
                            Rigging rigging = obj.GetComponent<Rigging>();
                            if (rigging != null)
                            {
                                rigging.ActivatePhysics(false);
                            }
                        }
                    }
                }
            }
        }
    }

    // Diana-specific UNGRASP implementation
    //  overrides Predicates.UNGRASP
    // IN: Objects
    // OUT: none
    public void UNGRASP(object[] args)
    {
        if (args[args.Length - 1] is bool)
        {
            if ((bool)args[args.Length - 1] == true)
            {
                if (args[0] is GameObject)
                {
                    // end the interaction and look at the user
                    GameObject obj = (args[0] as GameObject);
                    SetValue("me:intent:action", string.Empty, string.Format("Ending interaction with {0}", obj.name));
                    SetValue("me:intent:lookAt", "user", string.Empty);
                    SetValue("me:intent:targetObj", string.Empty, string.Empty);
                }
            }
        }
    }

    // Diana-specific THAT implementation
    //  overrides Predicates.THAT
    // IN: Objects
    // OUT: String
    public String THAT(object[] args)
    {
        String objName = string.Empty;

        // assume all inputs are of same type - if a GameObject
        if (args[0] is GameObject)
        {
            // find target by location
            GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
                .1f, args.Select(a => a as GameObject).ToList(), LayerMask.GetMask("Blocks"));
            if (target != null)
            {
                objName = target.name;
            }
        }
        // assume all inputs are of same type - if a String
        else if (args[0] is String)
        {
            // find target by location
            GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
                .1f, args.Select(a => GameObject.Find(a as String)).ToList(), LayerMask.GetMask("Blocks"));
            if (target != null)
            {
                objName = target.name;
            }
        }

        // still didn't find one
        if (objName == string.Empty)
        {
            // it might be the same as the current object intent
            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
            {
                objName = DataStore.GetStringValue("user:intent:object");
            }
        }

        return objName;
    }

    // Diana-specific THIS implementation
    //  overrides Predicates.THIS
    // IN: Objects
    // OUT: String
    public String THIS(object[] args)
    {
        String objName = string.Empty;

        // assume all inputs are of same type - if a GameObject
        if (args[0] is GameObject)
        {
            // find target by location
            GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
                .1f, args.Select(a => a as GameObject).ToList(), LayerMask.GetMask("Blocks"));
            if (target != null)
            {
                objName = target.name;
            }
        }
        // assume all inputs are of same type - if a String
        else if (args[0] is String)
        {
            // find target by location
            GameObject target = GlobalHelper.FindTargetByLocation(DataStore.GetVector3Value("user:pointPos"),
                .1f, args.Select(a => GameObject.Find(a as String)).ToList(), LayerMask.GetMask("Blocks"));
            if (target != null)
            {
                objName = target.name;
            }
        }

        // still didn't find one
        if (objName == string.Empty)
        {
            // it might be the same as the current object intent
            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
            {
                objName = DataStore.GetStringValue("user:intent:object");
            }
        }

        return objName;
    }

    // IN: Objects
    // OUT: none
    public void WHAT(object[] args)
    {
        if (args[0] is GameObject)
        {
            // assume all inputs are of same type
            GameObject obj = args[0] as GameObject;
            Voxeme objVox = obj.GetComponent<Voxeme>();

            if (objVox != null)
            {
                // if we know what the name of the object is
                if (objVox.voxml.Lex.Pred != string.Empty)
                {
                    SetValue("me:speech:intent", string.Format("That's a {0}.", objVox.voxml.Lex.Pred), string.Empty);
                }
                // otherwise try to analogize
                else
                {
                    // look for similarity with other objects
                    // stick to affordances for now
                    // get InteractionTarget objects in children of objVox
                    InteractionTarget[] affordances = objVox.GetComponentsInChildren<InteractionTarget>();

                    Voxeme compareVox = null;
                    // find and object to compare to 
                    foreach (Voxeme voxeme in eventManager.objSelector.allVoxemes)
                    {
                        // can't be the same as the theme object
                        if (voxeme != objVox)
                        {
                            // get InteractionTarget objects in children
                            InteractionTarget[] comparisons = voxeme.GetComponentsInChildren<InteractionTarget>();

                            // pick a comparison
                            InteractionTarget common = comparisons.Where(c => affordances.Any(a => a.gameObject.name == c.gameObject.name)).FirstOrDefault();
                            if (common != null)
                            {
                                compareVox = voxeme;
                                break;
                            }
                        }
                    }

                    // if we found one
                    if (compareVox != null)
                    {
                        // currently we compare via grasping only
                        Debug.Log(string.Format("Comparing unpredicated object {0} to {1}", obj.name, compareVox.gameObject.name));
                        SetValue("me:speech:intent", string.Format("I don't know what it's called, but I can grasp it like a {0}.", compareVox.voxml.Lex.Pred), string.Empty);
                        // TODO: actually grasp the object
                        // TODO: break this out into a separate method that can handle multiple types of analogy
                    }
                    // otherwise
                    else
                    {
                        SetValue("me:speech:intent", "I don't know what it's called.", string.Empty);
                    }
                }
            }
            else
            {
                SetValue("me:speech:intent", "idk lol", string.Empty);
            }

            // done answering question
            SetValue("user:intent:isQuestion", false, string.Empty);
            SetValue("me:question:isAnswering", false, string.Empty);
        }
    }

    // Diana-specific IsSatisfies implementation
    //  overrides SatisfactionTest.IsSatisfied
    public bool IsSatisfied(string test)
    {
        bool satisfied = false;

        // parse the predicate and extract the arguments
        Hashtable predArgs = GlobalHelper.ParsePredicate(test);
        string predString = "";
        string[] argsStrings = null;

        foreach (DictionaryEntry entry in predArgs)
        {
            predString = (string)entry.Key;
            argsStrings = ((string)entry.Value).Split(',');
        }

        // satisfy grasp
        if (predString == "grasp")
        {
            // grasp action must be complete
            if (DataStore.GetStringValue("me:actual:action") == "grasp")
            {
                GameObject theme = GameObject.Find(argsStrings[0] as string);

                if (theme != null)
                {
                    // satisfied if the theme object is Diana's target object
                    if (DataStore.GetStringValue("me:intent:targetObj") == theme.name)
                    {
                        satisfied = true;
                    }
                }
            }
        }
        else if (predString == "ungrasp")
        {
            // no me:actual:action = nothing is grasped/pointed at
            if (DataStore.GetStringValue("me:actual:action") == string.Empty)
            {
                GameObject theme = GameObject.Find(argsStrings[0] as string);

                if (theme != null)
                {
                    // satisfied if the theme object is NOT Diana's target object (b/c targetObject should be empty)
                    if (DataStore.GetStringValue("me:intent:targetObj") != theme.name)
                    {
                        satisfied = true;
                    }
                }
            }
        }

        return satisfied;
    }

    // IN: Object (single element array)
    // OUT: Location
    public Vector3 NEAR(object[] args)
    {
        Debug.Log(args[0].GetType());
        Vector3 outValue = Vector3.zero;
        if (args[0] is GameObject)
        {
            // near an object
            GameObject obj = ((GameObject)args[0]);

            Voxeme voxComponent = obj.GetComponent<Voxeme>();

            if (voxComponent != null)
            {
                Region region = new Region();
                Vector3 closestSurfaceBoundary = Vector3.zero;
                do
                {
                    region = GlobalHelper.FindClearRegion(voxComponent.supportingSurface.transform.root.gameObject, obj);
                    closestSurfaceBoundary =
                        GlobalHelper.ClosestExteriorPoint(voxComponent.supportingSurface.transform.root.gameObject,
                            region.center);
                    //				Debug.Log (Vector3.Distance (obj.transform.position, region.center));
                    //				Debug.Log (Vector3.Distance(closestSurfaceBoundary,region.center));
                } while (Vector3.Distance(obj.transform.position, region.center) >
                         Vector3.Distance(closestSurfaceBoundary, region.center));

                outValue = region.center;
            }
        }
        else if (args[0] is Vector3)
        {
            // near a location
            outValue = (Vector3)args[0];
        }

        return outValue;
    }
}
