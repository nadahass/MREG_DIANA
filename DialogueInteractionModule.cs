/*
This script governs the dialogue and interaction between Diana and the user

Reads:  Everything.  This module listens for all key changes.  It doesn't
            necessarily act on every key change but ValueChanged is
            called each time and acts depending on the particular
            key and its new value.  Particular keys this module acts on are:
                       
        user:isInteracting (BoolValue, whether or not the user is currently
            engaged in an interactive task with Diana)
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
        user:intent:lastEvent (StringValue, a predicate-argument
            representation of the last event Diana completed)       
        user:intent:replaceContent (StringValue, optional replacement
            content with an undo) 
        user:lastPointedAt:name (StringValue, the name of the object the user
            last rested deixis on -- empty if last focus of deixis was not an
            object)           
        user:lastPointedAt:position (Vector3Value, the position the user last
            rested deixis on -- <0,0,0> if not pointing)
        user:pointPos (Vector3Value, current position of user's deixis)
       
        me:question:isAnswering (BoolValue, whether or not Diana is
            currently answering a question)
        me:affordances:isLearning (BoolValue, whether or not Diana is
            learning a new affordance, e.g., a new gesture + object + grasp
            pose)               
        me:oneshot:isLearning (BoolValue, whether or not the hand client is
            currently attempting to learn a new gesture)
        me:isAttendingPointing (BoolValue, whether or not Diana is attending
            to the user's deixis)
        me:intent:action (StringValue, Diana's current intended action -- 
            NOT to be confused with user:intent:action; this is not a
            predicate-argument representation)        
        me:lastTheme (StringValue, the theme of the most recent action
            Diana took)
        me:lastThemePos (Vector3Value, the previous location of
            me:lastTheme)      
       
Write:  user:intent:object (StringValue, the name of the object that the 
            user wants Diana to direct her attention or action to, i.e., the
            theme of any subsequent action)
        user:intent:action (StringValue, a predicate-argument representation
            of an events with no variables filled - may contain "hints",
            e.g., directions relative to which an argument should be
            interpreted when assigned)
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
        user:intent:isQuestion (BoolValue, whether or not the user asked a
            question)
        user:intent:posack (BoolValue, whether or not the user is making posack
            -- either modality)
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
               
        me:isAttendingPointing (BoolValue, whether or not Diana is attending
            to the user's deixis)
        me:isCheckingServo (BoolValue, whether or not Diana is looking for
            another iteration on a servo gesture)
        me:isInServoLoop (BoolValue, whether or not Diana is executing a servo
            action, and therefore keeps doing it until she receives a cue to
            stop it)           
        me:speech:intent (StringValue, Diana's speech output)
        me:intent:lookAt (StringValue, the object (or user) Diana looks at)
        me:intent:targetObj (StringValue, name of the object that is theme
            of Diana's attention/action)
        me:intent:action (StringValue, Diana's current intended action -- 
            NOT to be confused with user:intent:action; this is not a
            predicate-argument representation) 
        me:affordances:forget (BoolValue, whether or not Diana should forget
            the affordances she learned)
        me:affordances:isLearning (BoolValue, whether or not Diana is
            learning a new affordance, e.g., a new gesture + object + grasp
            pose) 
        me:isUndoing (BoolValue, whether or not Diana is currently undoing
            her last action)     
        me:question:isAnswering (BoolValue, whether or not Diana is
            currently answering a question)      
        me:emotion (StringValue, Diana's emotion)      
*/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using RootMotion.FinalIK;
using VoxSimPlatform.Agent.CharacterLogic;
using VoxSimPlatform.Global;
using VoxSimPlatform.Interaction;
using VoxSimPlatform.Vox;
using System.IO;
using System;
using System.Collections;
using VoxSimPlatform.SpatialReasoning.QSR;
using VoxSimPlatform.SpatialReasoning;
using UnityEngine.SceneManagement;
using VoxSimPlatform.NLU;
using GoogleCloudStreamingSpeechToText;
using VoxSimPlatform.Core;

public class DialogueInteractionModule : ModuleBase
{
    public GameObject agent;
    public DialogueStateMachine stateMachine;

    public double servoLoopTimerTime = 10;
    public double learningUpdateTimerTime = 3000;

    public Transform grabbableBlocks;
    public GameObject demoSurface;

    public VoxMLLibrary voxmlLibrary;

    System.Timers.Timer learningUpdateTimer;
    bool provideLearningStatusUpdate = false;

    System.Timers.Timer servoLoopTimer;
    bool checkServoStatus = false;
    bool logged = false;
    bool SetLocation = false;
    //Stack<string> eventHist_stack;
    //Stack<string> objHist_stack;

    public static Stack<string> eventHist_stack;
    public Stack<string> getEventHist_stack() { return eventHist_stack; }
    public void setEventHist_stack(Stack<string> value) { eventHist_stack = value; }

    public static Stack<string> objHist_stack;
    public Stack<string> getobjHist_stack() { return objHist_stack; }
    public void setobjHist_stack(Stack<string> value) { objHist_stack = value; }

    public static List<string> objlist;
    public List<string> getobjlist() { return objlist; }
    public void setobjlist(List<string> value) { objlist = value; }

    public DialogueHistory dh;
    static bool nonpointing;
    public bool getpointing() { return nonpointing; }
    public void setpointing(bool value) { nonpointing = value; }

    //GameObject behaviorController;
    //RelationTracker relationTracker;

    EventManagementModule emm;
    DianaParser dp;
    //EventManager eventmanager;
    //UserPointMarker PointingMarkerObj;
    public NLUModule nlu;
    StreamingRecognizer stream;

    // TODO: migrate over to GenericLogger
    public StreamWriter logFileStream;
    public StreamWriter logFile;

    public string eveHist { get; private set; }
    public string objHist { get; private set; }

    //static string pointingPurpose;
    //public string getpointingPurpose() { return pointingPurpose; }
    //public void setpointingPurpose(string value) { pointingPurpose = value; }

    static string focuspointing, dianafocuspointTime,posRelcompTime;
    string DianaFocusPointing, IsDianaDis, HumanModality, dianaMpdality, DianaTargetPointing;
    public string humanfocuspointTime { get; private set; }

    public string getdianafocuspointTime() { return dianafocuspointTime; }
    public void setdianafocuspointTime(string value) { dianafocuspointTime = value; }

    public string StartTime { get; private set; }
    public string getfocuspointing() { return focuspointing; }
    public void setfocuspointing(string value) { focuspointing = value; }

    static string targetpointing;

    public string targetpointTime { get; private set; }

    public string gettargetpointing() { return targetpointing; }
    public void settargetpointing(string value) { targetpointing = value; }

    static string focusObject;
    public string getfocusObject() { return focusObject; }
    public void setfocusObject(string value) { focusObject = value; }

    static string targetObject;
    //private string relationalEvent;

    public string gettargetObject() { return targetObject; }
    public void settargetObject(string value) { targetObject = value; }

    public string focusPosition { get; private set; }
    public bool befdis { get; private set; }
    public bool issetobj { get; private set; }
    public bool setloc { get; private set; }
    public static bool visited;
    //string executedevent = null;
    //string speech;
    public static string relations { get; private set; }
    public static bool pointonly { get; private set; }
    public bool hisSetLocation { get; private set; }

    static bool speechonly;
    //Vector3 targetPos,startPos;

    public bool getspeechonly() { return speechonly; }
    public void setspeechonly(bool value) { speechonly = value; }


    // Use this for initialization
    void Start()
    {
        base.Start();
       
        // get the active avatar and appropriate components
        for (int i = 0; i < GameObject.Find("Avatars (Pick One)").transform.childCount; i++)
        {
            if (GameObject.Find("Avatars (Pick One)").transform.transform.GetChild(i).gameObject.activeSelf)
            {
                agent = GameObject.Find("Avatars (Pick One)").transform.GetChild(i).gameObject;
                if (stateMachine == null) stateMachine = agent.GetComponent<DialogueStateMachine>();
                stateMachine.scenarioController = gameObject.GetComponent<SingleAgentInteraction>();
            }
        }

        // here we listen for all changes
        DataStore.instance.onValueChanged.AddListener(ValueChanged);
        // setup servo loop timer
        if (servoLoopTimerTime > 0)
        {
            servoLoopTimer = new System.Timers.Timer(servoLoopTimerTime);
            servoLoopTimer.Enabled = false;
            servoLoopTimer.Elapsed += CheckServoStatus;
        }

        // setup learning update timer
        if (learningUpdateTimerTime > 0)
        {
            learningUpdateTimer = new System.Timers.Timer(learningUpdateTimerTime);
            learningUpdateTimer.Enabled = false;
            learningUpdateTimer.Elapsed += ProvideLearningStatusUpdate;
        }
    }

    // Update is called once per frame
    void Update()
    {

        // check servo status - timer-triggered flag
        if (checkServoStatus)
        {
            SetValue("me:isCheckingServo", true, string.Empty);
            SetValue("me:isInServoLoop", true, string.Empty);
            checkServoStatus = false;
        }
        // check learning status - timer-triggered flag
        if (provideLearningStatusUpdate)
        {
            SetValue("me:speech:intent", "Please hold your new gesture still and let me commit it to memory.", string.Empty);
            provideLearningStatusUpdate = false;
        }
        if (logFileStream != null)
        {
            try { logFileStream.FlushAsync(); }
            catch (System.InvalidOperationException) { }
        }
        if (logFile != null)
        {
            try { logFile.FlushAsync(); }
            catch (System.InvalidOperationException)
            {
                Debug.Log("InvalidOperationException ");
            }
        }

        // Debug.Log("isprompt: "+ emm.getisprompt().ToString());
        // Debug.Log("pointonly: " + pointonly.ToString());
        //if (!string.IsNullOrEmpty(nlu.GetUserSpeech()) && nlu.GetUserSpeech().Equals("stop"))
        //{
        logTocsv();
        //}
    }

    public void logTocsv()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (!sceneName.Equals("Scene0")) {

            if (emm.getisprompt() == true  /*&& !nlu.GetUserSpeech().Equals("stop") && (issetobj==true || /*string.IsNullOrEmpty(nlu.GetUserSpeech())||*/ /*pointonly == true || !nlu.parsed.Contains(";") || !emm.getlogeventStr().Contains(";") || emm.getRE_Category().Equals("Historical") ||
            emm.getRE_Category().Equals("Relational") || nlu.parsed.Contains(",") || emm.getlogeventStr().Contains(",") || SetLocation==true)*/)
            {
                Debug.Log("Nada: DIM: isprompt: " + emm.getisprompt().ToString() + SetLocation);

                //    // logging data to csv file
                if (logFile == null)
            {
                var fname = string.Format(sceneName + "_ExecutedPrompts.csv");
                logFile = File.AppendText(fname);
                logFile.WriteLine("Date, PromptStartTime, PromptCompletionTime, ExecutionLength, UserSpeech, HumanUsedModality, DianaUsedModality, SpeechRecognition, PLF, RE_Category, Event, FocusPosition, " +
                    "HumanFocusPointingTime,DisambigRecognitionLength, FocusObj, TargetObj,TargetObjPosition, Demonstrative, " +
                    "HumanFocusPointing, HumanTargetPointing, DianaFocusPointingTime, DianaFocusPointing, DianaTargetPointing, IsDianaDis, Distance(Agent->Focus), Relations, Configurations, " + "EventsHistory," +
                    " ObjectsHistory");
            }

                if (!emm.getRE_Category().Equals("Historical") && !emm.getRE_Category().Equals("Relational"))
                {
                    Debug.Log("Nada: DIM: not-hist-or-rel: ");

                    //logFile.WriteLine((pointonly == true).ToString() + (SetLocation == false).ToString() + targetpointing.Contains("move").ToString() + (speechonly == false).ToString());
                    //pointing only: target is object
                    if (pointonly == true && /*SetLocation == false &&*/ targetpointing.Contains("move") && /*string.IsNullOrEmpty(nlu.GetUserSpeech())*/ speechonly == false /*&& !targetObject.Equals("None")*/ && !emm.getlogeventStr().Contains(";") && emm.getlogeventStr().Contains(",") /*&& !emm.logeventStr.Contains("plate")*/)
                    {
                        //logFile.WriteLine("pointing only: target is object");
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: pointonly{0}", issetobj));

                        try
                        {
                            dianafocuspointTime = humanfocuspointTime;
                            string speech = "none";
                            //if (!string.IsNullOrEmpty(nlu.GetspeechTime()))
                            //StartTime = startTime(speech, focuspointing, null, humanfocuspointTime);
                            //else
                            StartTime = humanfocuspointTime;

                            if (string.IsNullOrEmpty(emm.promptCompletionTime))
                            {
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + posRelcompTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + "No speech" + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + "None" + "," + "No parsed speech" + "," +
                                emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                humanfocuspointTime + "," + "None" + "," + focusObject + "," + targetObject + "," + GameObject.Find(targetObject).transform.position.ToString("f9").Replace(",", ":") + "," + "None" + "," + focuspointing + "," + targetpointing + "," + dianafocuspointTime + "," +
                                DianaFocusPointing + "," + DianaTargetPointing + "," + "No" + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                eveHist + "," + objHist);
                            }
                            else
                            {
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + "No speech" + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + "None" + "," + "No parsed speech" + "," +
                                emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                humanfocuspointTime + "," + "None" + "," + focusObject + "," + targetObject + "," + GameObject.Find(targetObject).transform.position.ToString("f9").Replace(",", ":") + "," + "None" + "," + focuspointing + "," + targetpointing + "," + dianafocuspointTime + "," +
                                DianaFocusPointing + "," + DianaTargetPointing + "," + "No" + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                eveHist + "," + objHist);
                            }
                        }
                        catch (System.InvalidOperationException) { }
                        emm.setisprompt(false);
                        pointonly = false;
                        issetobj = false;
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: point and target is a block: after false{0}", issetobj));
                    }
                    //pointing only: target is empty location
                    else if (speechonly == false && SetLocation == true && emm.getlogeventStr().Contains(";"))
                    {

                        //logFile.WriteLine("point only: target is location");
                        SetLocation = false;
                        try
                        {
                            dianafocuspointTime = humanfocuspointTime;
                            string speech = "none";
                            StartTime = startTime(speech, focuspointing, null, humanfocuspointTime);

                            if (string.IsNullOrEmpty(emm.promptCompletionTime))
                            {
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + posRelcompTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + "No speech" + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + "None" + "," + "No parsed speech" + "," +
                            emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                            GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                            humanfocuspointTime + "," + "None" + "," + focusObject + "," + "None" + "," + "( " + emm.getlogeventStr().Split(',')[1].Replace(">", "").Replace("<", "").Replace(";", ":") + " )" + "," + "None" + "," + focuspointing + "," + "To empty Location" + "," +
                            dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.RelationsLog() + "," + Configurations() + "," +
                            eveHist + "," + objHist);
                            }
                            else
                            {
                               logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + "No speech" + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + "None" + "," + "No parsed speech" + "," +
                            emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                            GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                            humanfocuspointTime + "," + "None" + "," + focusObject + "," + "None" + "," + "( " + emm.getlogeventStr().Split(',')[1].Replace(">", "").Replace("<", "").Replace(";", ":") + " )" + "," + "None" + "," + focuspointing + "," + "To empty Location" + "," +
                            dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.RelationsLog() + "," + Configurations() + "," +
                            eveHist + "," + objHist);
                            }
                        }
                        catch (System.InvalidOperationException) { }
                        emm.setisprompt(false);
                        SetLocation = false;
                        issetobj = false;
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: point to location: after false{0}", issetobj));
                    }
                    // Transit_Attributive, e.g., put it on the green block by speacking only without pointing.. use pointing for disambiguation
                    else if (nlu.parsed.Contains(";").Equals(false) /*|| emm.logeventStr.Contains(";").Equals(false) && SetLocation.Equals(false)*/
                    && nlu.parsed.Contains(",").Equals(true) /*|| emm.logeventStr.Contains(",").Equals(true)*/ && issetobj.Equals(true) && !targetObject.Equals("None")
                    && nlu.parsed.Contains("plate").Equals(false) && nlu.parsed.Contains("cup").Equals(false) /*&& pointingPurpose.Equals("None").Equals(false) */&& speechonly.Equals(true))
                    {
                        //logFile.WriteLine("Speech only: target is object");

                        try
                        {
                            dianafocuspointTime = humanfocuspointTime;
                            string speech = nlu.GetUserSpeech();
                            StartTime = startTime(speech, focuspointing, nlu.GetspeechTime(), humanfocuspointTime);

                            if (befdis == true)
                            {
                                AddToDialogueHist();

                                befdis = false;
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," +  StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":")
                                + "," + humanfocuspointTime + "," + "None" + "," + focusObject + "," + targetObject + "," + GameObject.Find(targetObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                Demonstrative(nlu.GetUserSpeech()) + "," + focuspointing + "," + targetpointing + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.RelationsLog() + "," + Configurations() + "," + eveHist + "," + objHist);
                            }
                            else
                            {
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + targetObject + "," + GameObject.Find(targetObject).transform.position.ToString("f9").Replace(",", ":") + "," + Demonstrative(nlu.GetUserSpeech()) + "," +
                                focuspointing + "," + targetpointing + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.RelationsLog() + "," + Configurations() + "," + eveHist + "," + objHist);
                            }

                        }
                        catch (System.InvalidOperationException) { }
                        emm.setisprompt(false);
                        issetobj = false;
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: target is a block: after false{0}", issetobj));
                        speechonly = false;
                    }

                    // Attributivee.g., take the/this/that green block
                    else if (!nlu.parsed.Contains(",") && !nlu.GetUserSpeech().Contains("no") && !nlu.GetUserSpeech().Contains("nevermind") && issetobj == true /*&& SetLocation == false && pointonly==false|| !emm.logeventStr.Contains(",")*/)
                    {
                        //logFile.WriteLine("Attributive");
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: attributive {0}", focusObject));
                        dianafocuspointTime = humanfocuspointTime;

                        try
                        {
                            string speech = nlu.GetUserSpeech();

                            if (nlu.GetUserSpeech().Contains("pick"))
                                speech = dp.getform().Replace("grasp", "lift");
                            else speech = dp.getform();
                            // to add the event to the history
                            StartTime = startTime(speech, focuspointing, nlu.GetspeechTime(), humanfocuspointTime);

                            if (befdis == true)
                            {
                                AddToDialogueHist();
                                befdis = false;
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," +  SpeechRecognition() + "," + speech.Replace(",", ";") + "," +
                                 emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + "None" + "," + focusObject + "," + "None" + "," + "None" +"," +
                                 Demonstrative(nlu.GetUserSpeech()) + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," + eveHist + "," + objHist);
                            }
                            else
                            {
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + speech.Replace(",", ";") + "," +
                                emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + "None" + "," + "None" +
                                "," + Demonstrative(nlu.GetUserSpeech()) + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," + eveHist + "," + objHist);
                            }

                        }
                        catch (System.InvalidOperationException) { }
                        emm.setisprompt(false);
                        issetobj = false;
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: attributive: : after false {0}", focusObject));

                    }
                    // Attributivee.g., take green block
                    else if (!nlu.parsed.Contains(",") && issetobj == false && (!nlu.GetUserSpeech().Contains("the") && !nlu.GetUserSpeech().Contains("this") && !nlu.GetUserSpeech().Contains("that") )/*&& SetLocation == false && pointonly==false|| !emm.logeventStr.Contains(",")*/)
                    {
                        //logFile.WriteLine("Attributive");
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: attributive {0}", focusObject));
                        focusObject = DataStore.GetStringValue("user:intent:object");
                        dianafocuspointTime = emm.promptCompletionTime;
                        humanfocuspointTime= "None";
                        DianaFocusPointing = "pointing without disambiguation";
                        string speech = nlu.GetUserSpeech();
                        focuspointing = "none";
                        StartTime = System.DateTime.Now.ToString("HH:mm:ss");
                           // startTime(speech, focuspointing, nlu.GetspeechTime(), humanfocuspointTime);

                        try
                        {
                            if (nlu.GetUserSpeech().Contains("pick"))
                                speech = dp.getform().Replace("grasp", "lift");
                            else speech = dp.getform();
                            // to add the event to the history

                            if (nlu.GetUserSpeech().Contains("no") || nlu.GetUserSpeech().Contains("nevermind")) {

                                //befdis = false;
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + System.DateTime.Now.ToString("HH:mm:ss") + ", " + "None" + "," + "None" + "," + nlu.GetUserSpeech() + "," + "speech" + "," + "multimodal" + "," + SpeechRecognition() + "," + "No parsed speech" + "," +
                                 "None" + "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + focusObject + "," + "None" + "," + "None" + "," + "None" +
                                 "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + "No" + "," + "None" + "," + emm.spaceRelations + "," + Configurations() + "," + eveHist + "," + objHist);
                            }
                            else
                            {
                                AddToDialogueHist();
                                befdis = false;
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + "multimodal" + "," + SpeechRecognition() + "," + speech.Replace(",", ";") + "," +
                                 emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + "None" + "," + focusObject + "," + "None" + "," + "None" + "," + "None" +
                                 "," + "None" + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + "None" + "," + "No" + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," + eveHist + "," + objHist);
                            }

                        }
                        catch (System.InvalidOperationException) { }
                        emm.setisprompt(false);
                        issetobj = false;
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: attributive: : after false {0}", focusObject));

                    }
                    // Speech only: target is landmark or location e.g., move it to the plate, to the cup or slide it to the left
                    else if (nlu.parsed.Contains(",") == true && /*SetLocation == false && targetObject.Equals("None") == false &&*/ (nlu.parsed.Contains("plate") == true || nlu.parsed.Contains("cup") == true || nlu.parsed.Contains("slide") == true /*|| emm.logeventStr.Contains("put") == true*/))
                    {
                        //logFile.WriteLine("Speech only: target is landmark or location ");

                        try
                        {
                            dianafocuspointTime = humanfocuspointTime;
                            string speech = nlu.GetUserSpeech();
                            StartTime = startTime(speech, focuspointing, nlu.GetspeechTime(), humanfocuspointTime);

                            // put it/this red block/the red block to the plate
                            if (nlu.parsed.Contains("plate") || emm.getlogeventStr().Contains("plate"))
                            {
                                AddToDialogueHist();
                                //if the object type did not be mentioned explicitly; e.g., put it to the plate 
                                if (nlu.parsed.Contains("{0}") && nlu.GetUserSpeech().Contains("it"))
                                {
                                    // if the object selected by pointing before user speaking and diana disambiguation "it"
                                    if (befdis == true)
                                    {
                                        //AddToDialogueHist();
                                        befdis = false;
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + "None" + "," + focusObject + "," + "plate" + "," + GameObject.Find("plate").transform.position.ToString("f9").Replace(",", ":") +
                                        "," + "None" + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);
                                    }
                                    //// object selected by user after diana disambiguating "it"
                                    else
                                    {
                                        focuspointing = "Before Ask for Disambiguation";
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + "plate" + "," + GameObject.Find("plate").transform.position.ToString("f9").Replace(",", ":") +
                                        "," + "None" + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);

                                    }
                                }
                                //if the object type mentioned explicitly; e.g., put the green block to the plate 
                                else if (nlu.parsed.Contains("{0}") && !nlu.GetUserSpeech().Contains("it"))
                                {
                                    if (befdis == true)
                                    {
                                        // AddToDialogueHist();
                                        befdis = false;
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + "None" + "," + focusObject + "," + "plate" + "," + GameObject.Find("plate").transform.position.ToString("f9").Replace(",", ":") +
                                        "," + Demonstrative(nlu.GetUserSpeech()) + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);
                                    }
                                    else
                                    {
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + "plate" + "," + GameObject.Find("plate").transform.position.ToString("f9").Replace(",", ":") +
                                        "," + Demonstrative(nlu.GetUserSpeech()) + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);
                                    }
                                }
                            }
                            // put it/this red block/the red block to the cup
                            if (nlu.parsed.Contains("cup") || emm.getlogeventStr().Contains("cup"))
                            {
                                AddToDialogueHist();

                                //logFile.WriteLine("cup reference");

                                //if the object type did not be mentioned explicitly; e.g., put it to the cup 
                                if (nlu.parsed.Contains("{0}") && nlu.GetUserSpeech().Contains("it"))
                                {
                                    // if the object selected by pointing before user speaking and diana disambiguation "it"
                                    if (befdis == true)
                                    {
                                        //AddToDialogueHist();
                                        befdis = false;
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + "None" + "," + focusObject + "," + "cup" + "," + GameObject.Find("cup").transform.position.ToString("f9").Replace(",", ":") +
                                        "," + "None" + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);
                                    }
                                    //// object selected by user after diana disambiguating "it"
                                    else
                                    {
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + "cup" + "," + GameObject.Find("cup").transform.position.ToString("f9").Replace(",", ":") +
                                        "," + "None" + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);
                                    }
                                }
                                //if the object type mentioned explicitly; e.g., put the red block to the plate 
                                else if (nlu.parsed.Contains("{0}") && !nlu.GetUserSpeech().Contains("it"))
                                {
                                    if (befdis == true)
                                    {
                                        //logFile.WriteLine("before/ object with cup ref");
                                        //AddToDialogueHist();
                                        befdis = false;
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + "None" + "," + focusObject + "," + "cup" + "," + GameObject.Find("cup").transform.position.ToString("f9").Replace(",", ":") +
                                         "," + Demonstrative(nlu.GetUserSpeech()) + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);
                                    }
                                    else
                                    {
                                        //logFile.WriteLine("after/ object with cup ref");
                                        //AddToDialogueHist();
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + "cup" + "," + GameObject.Find("cup").transform.position.ToString("f9").Replace(",", ":") +
                                        "," + Demonstrative(nlu.GetUserSpeech()) + "," + focuspointing + "," + "None" + "," + dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations
                                        + "," + Configurations() + "," + eveHist + "," + objHist);
                                    }
                                }
                            }
                            // slide it/the red block to the left , move it/the red block here
                            else if (nlu.parsed.Contains("slide") || emm.getlogeventStr().Contains("slide") /*|| emm.logeventStr.Contains("put")*/)
                            {
                                AddToDialogueHist();
                                if (nlu.parsed.Contains("{0}") && nlu.GetUserSpeech().Contains("it"))
                                {
                                    // if "it" specified by pointing before disamb
                                    if (befdis == true)
                                    {
                                        //AddToDialogueHist();
                                        befdis = false;

                                        // object mentioned before the speaking by pointing 
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                        humanfocuspointTime + "," + "None" + "," + focusObject + "," + "None" + "," + "( " + emm.getlogeventStr().Split(',')[1].Replace(">", "").Replace("<", "").Replace(";", ":") + " )" + "," + "None" + ","
                                        + focuspointing + "," + "None" + "," +
                                        dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                        eveHist + "," + objHist);
                                    }
                                    else
                                    {
                                        //object mentioned in the previous event or after disambigaution:slide it to the left
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss") + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                        humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + "None" + "," + "( " + emm.getlogeventStr().Split(',')[1].Replace(">", "").Replace("<", "").Replace(";", ":") + " )" + "," + "None" + ","
                                        + focuspointing + "," + "None" + "," +
                                        dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                        eveHist + "," + objHist);
                                    }
                                }
                                else if (nlu.parsed.Contains("{0}") && !nlu.GetUserSpeech().Contains("it"))
                                {
                                    if (befdis == true)
                                    {
                                        //AddToDialogueHist();
                                        befdis = false;
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                        humanfocuspointTime + "," + "None" + "," + focusObject + "," + "None" + "," + "(" + emm.getlogeventStr().Split(',')[1].Replace(">", "").Replace("<", "").Replace(";", ":") + " )" + "," + Demonstrative(nlu.GetUserSpeech()) + ","
                                        + focuspointing + "," + "None" + "," +
                                        dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                        eveHist + "," + objHist);
                                    }
                                    else
                                    {
                                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")
                                            + "," + ExecutionLength(StartTime, DateTime.Parse(emm.promptCompletionTime).AddSeconds(3).ToString("HH:mm:ss")) + "," + nlu.GetUserSpeech() + "," + HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," +
                                        GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                                        humanfocuspointTime + "," + DisRecognitionTime(StartTime, humanfocuspointTime).ToString() + "," + focusObject + "," + "None" + "," + "(" + emm.getlogeventStr().Split(',')[1].Replace(">", "").Replace("<", "").Replace(";", ":") + " )" + "," + Demonstrative(nlu.GetUserSpeech()) + ","
                                        + focuspointing + "," + "None" + "," +
                                        dianafocuspointTime + "," + DianaFocusPointing + "," + DianaTargetPointing + "," + IsDianaDis + "," + Distance(focusObject) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                        eveHist + "," + objHist);
                                    }
                                }
                            }
                        }
                        catch (System.InvalidOperationException) { }
                        emm.setisprompt(false);
                        issetobj = false;
                        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj: target is LM: : after false {0}", focusObject));

                    }
                }
                else if (emm.getRE_Category().Equals("Historical") && !nlu.GetUserSpeech().Contains("no") && !nlu.GetUserSpeech().Contains("nevermind"))
                {
                    try
                    {
                        dianafocuspointTime = emm.Dianafocustime;
                        string speech = nlu.GetUserSpeech();
                        StartTime = startTime(speech, "none", nlu.GetspeechTime(), "none");

                        //logFile.WriteLine("historical");
                        AddToDialogueHist();
                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," +
                        ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + "speech" + "," +
                        "multimodal" + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                        emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + GameObject.Find(focusObject).transform.position.ToString("f9").Replace(",", ":") + "," +
                        "None" + "," + "None" + "," + focusObject + "," + "None" + "," + "None" +
                        "," + Demonstrative(nlu.GetUserSpeech()) + "," + "None" + "," + "None" + "," + dianafocuspointTime + "," + "Acting without disambiguation" + "," + "None" + "," + "No" + "," + Distance(focusObject) + "," +
                        emm.spaceRelations + "," + Configurations() + "," + eveHist + ","
                        + objHist);
                    }
                    catch (System.InvalidOperationException) { }
                    emm.setisprompt(false);
                }
                else if (emm.getRE_Category().Equals("Relational") || emm.relfound || !emm.relfound)
                {
                    try
                    {
                        StartTime = emm.Dianafocustime;
                        dianafocuspointTime = emm.Dianafocustime;
                        string speech = nlu.GetUserSpeech();
                        StartTime = startTime(speech, "none", nlu.GetspeechTime(), "none");

                        if (emm.relfound)
                        {
                            //logFile.WriteLine("relational");
                            AddToDialogueHist();
                            logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," + "speech only"+ "," + "multimodal" + "," + SpeechRecognition() + "," + dp.getform().Replace(",", ";") + "," +
                            emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + GameObject.Find(emm.Focus1).transform.position.ToString("f9").Replace(",", ":") + "," + "None" + "," +
                            "None" + "," + emm.Focus1 + "," + "None" + "," + "None"
                            + "," + Demonstrative(nlu.GetUserSpeech()) + "," + "None" + "," + "None" + "," + dianafocuspointTime + "," + "Acting without disambiguation" + "," + "None" + "," + "No" + "," + Distance(emm.Focus1) + "," +
                            emm.spaceRelations + "," + Configurations() + "," + eveHist + ","
                            + objHist);
                        }
                        else
                        {
                            if (nlu.GetPosack())
                            {
                                speech = emm.speech;
                                focuspointing = "After Ask for Disambiguation";
                                StartTime = startTime(speech, focuspointing, nlu.GetspeechTime(), "none");

                                nlu.setPosack(false);
                                AddToDialogueHist();//
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + emm.speech + "," +
                                "speech" + "," + "multimodal" + "," + SpeechRecognition() + "," + emm.parsing + "," +
                                "Relational" + "," + emm.eventplf1.Replace(",", ";") + "," + GameObject.Find(emm.Focus1).transform.position.ToString("f9").Replace(",", ":") + "," + "None" + "," +
                                "None" + "," + emm.Focus1 + "," + "None" + "," + "None" + "," + Demonstrative(emm.speech) + "," + focuspointing + "," + "None" + "," +
                                dianafocuspointTime + "," + "Disambiguate before pointing" + "," + "None" + "," + "Yes" + "," + Distance(emm.Focus1) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                eveHist + "," + objHist);
                                posRelcompTime = emm.promptCompletionTime;
                            }
                            else if (nlu.Getisneg())
                            {
                                speech = emm.speech;
                                nlu.setisneg(false);
                                //AddToDialogueHist();
                                logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + emm.speech + "," +
                                HumanUsedModality(speech, "none") + "," + "multimodal" + "," + SpeechRecognition() + "," + emm.parsing + "," +
                                "Relational" + "," + emm.eventplf2.Replace(",", ";") + "," + GameObject.Find(emm.Focus2).transform.position.ToString("f9").Replace(",", ":") + "," + humanfocuspointTime + "," +
                                "None" + "," + emm.Focus2 + "," + "None" + "," + "None" + "," + Demonstrative(emm.speech) + "," + "None" + "," + "None" + "," +
                                dianafocuspointTime + "," + "Disambiguate before pointing" + "," + "None" + "," + "Yes" + "," + Distance(emm.Focus2) + "," + emm.spaceRelations + "," + Configurations() + "," +
                                eveHist + "," + objHist);
                            }
                        }
                    }
                    catch (System.InvalidOperationException) { }
                    emm.setisprompt(false);

                }
                // if no condition found
                else
                {
                    StartTime = nlu.GetspeechTime();
                    string speech = nlu.GetUserSpeech();
                    StartTime = startTime(speech, focuspointing, nlu.GetspeechTime(), "none");

                    try
                    {
                        logFile.WriteLine(System.DateTime.Now.ToString("yyyy/MM/dd") + "," + StartTime + "," + emm.promptCompletionTime + "," + ExecutionLength(StartTime, emm.promptCompletionTime) + "," + nlu.GetUserSpeech() + "," +
                            HumanUsedModality(speech, focuspointing) + "," + DianaUsedModality(speech, focuspointing) + "," + SpeechRecognition() + "," + speech.Replace(",", ";") + "," +
                     emm.getRE_Category() + "," + emm.getlogeventStr().Replace(",", ";") + "," + "None" + "," + humanfocuspointTime + "," + "None" + "," + "None" + "," + "None" + "," + "None" +
                     "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + "None" + "," + emm.spaceRelations + "," + Configurations() + "," + eveHist + "," + objHist);
                    }
                    catch (System.InvalidOperationException) { }
                    emm.setisprompt(false);
                }

            }
    }

    }
    private void Awake()
    {
        //eventmanager = new EventManager();
        emm = GameObject.FindObjectOfType<EventManagementModule>();
        dp = new DianaParser();
        dh = new DialogueHistory();
        nlu = new NLUModule();
        stream = new StreamingRecognizer();
        //PointingMarkerObj = new UserPointMarker();
    }

    public string Demonstrative(string utter)
    {
        string dem = "";
        if (utter.Contains("this")) dem = "this";
        else if (utter.Contains("that")) dem = "that";
        else dem = "the";

        return dem;
    }


    public string SpeechRecognition()
    {
        string SR = "";
        
        if (stream.getgsr())
        {
            SR = "GSR Voice Entry";
            stream.setgsr(false);
        }
        else 
        {

            SR = "Keyboard Entry";
        }

        return SR;
    }

    public string Configurations()
    {
        string config = "";
        Vector3 red1 = GameObject.Find("RedBlock1").transform.position;
        Vector3 red2 = GameObject.Find("RedBlock2").transform.position;
        Vector3 green1 = GameObject.Find("GreenBlock1").transform.position;
        Vector3 green2 = GameObject.Find("GreenBlock2").transform.position;
        Vector3 blue1 = GameObject.Find("BlueBlock1").transform.position;
        Vector3 blue2 = GameObject.Find("BlueBlock2").transform.position;
        Vector3 pink1 = GameObject.Find("PinkBlock1").transform.position;
        Vector3 pink2 = GameObject.Find("PinkBlock2").transform.position;
        Vector3 yellow1 = GameObject.Find("YellowBlock1").transform.position;
        Vector3 yellow2 = GameObject.Find("YellowBlock2").transform.position;
        config = "RedBlock1: " + red1.ToString("f9").Replace(",", ":") + " | " + "RedBlock2: " + red2.ToString("f9").Replace(",", ":") + " | " + "GreenBlock1: " + green1.ToString("f9").Replace(",", ":") + " | "
            + "GreenBlock2: " + green2.ToString("f9").Replace(",", ":") + " | " + "BlueBlock1: " + blue1.ToString("f9").Replace(",", ":") + " | " + "BlueBlock2: " + blue2.ToString("f9").Replace(",", ":") + " | "
            + "PinkBlock1: " + pink1.ToString("f9").Replace(",", ":") + " | " + "PinkBlock2: " + pink2.ToString("f9").Replace(",", ":") + " | " + "YellowBlock1: " + yellow1.ToString("f9").Replace(",", ":") + " | "
            + "YellowBlock2: " + yellow2.ToString("f9").Replace(",", ":");


        return config;
    }
    public float Distance(string focusobj)
    {
        float dis;
        GameObject agent = GameObject.Find("Diana2"); ;
        GameObject focus = GameObject.Find(focusobj);

         dis = Vector3.Distance(agent.transform.position, focus.transform.position);
        return dis;
    }

    // time spent by humans to recognize agent's disambiguation
    public TimeSpan DisRecognitionTime(string StartTime, string focusTime)
    {
        TimeSpan diff;
        var parsedStartTime = DateTime.Parse(StartTime);
        var parsedfocusTime = DateTime.Parse(focusTime);

        if (parsedStartTime > parsedfocusTime)
           diff = parsedStartTime - parsedfocusTime;
        else
            diff = parsedfocusTime - parsedStartTime;

        return diff;
    }

    // time spent by Diana to execute an event
    public TimeSpan ExecutionLength(string StartTime, string completionTime)
    {
        TimeSpan diff;
        diff = DisRecognitionTime(StartTime, completionTime);
        return diff;
    }

    public string HumanUsedModality(string speech, string focusPointing)
    {
        if (!speech.Equals("none") && focusPointing.Equals("Before Ask for Disambiguation")) {
            HumanModality = "multimodal";
        }
        else if (!speech.Equals("none") && (focusPointing.Equals("After Ask for Disambiguation") || focusPointing.Equals("none")))
        {
            HumanModality = "speech only";
        }
        else if (speech.Equals("none"))
        {
            HumanModality = "pointing only";
        }
        return HumanModality;
    }

      public string startTime(string speech, string focusPointing, string speechTime, string pointingTime)
      {
        string start="";
        if (!speech.Equals("none") && focusPointing.Equals("Before Ask for Disambiguation"))
        {
            start = pointingTime;
        }
        else if (!speech.Equals("none") && (focusPointing.Equals("After Ask for Disambiguation")|| focusPointing.Equals("none")))
        {
            start = speechTime;
        }
        else if (speech.Equals("none"))
        {
            start = pointingTime; // action, pointing and say OK
        }
        else
        {
            start = System.DateTime.Now.ToString("HH:mm:ss");
        }
        return start;
    }


    public string DianaUsedModality(string speech, string focusPointing)
    {
        if (!speech.Equals("none") && focusPointing.Equals("Before Ask for Disambiguation"))
        {
            dianaMpdality = "multimodal";
        }
        else if (!speech.Equals("none") && (focusPointing.Equals("After Ask for Disambiguation") || focusPointing.Equals("none")))
        {
            dianaMpdality = "speech only";
        }
        else if (speech.Equals("none"))
        {
            dianaMpdality = "multimodal"; // action, pointing and say OK
        }
        return dianaMpdality;
    }

    void Log(string msg)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (!sceneName.Equals("Scene0"))
        {

            if (logFileStream == null)
            {
                var fname = string.Format(sceneName + "_RE_LogDialogueInteraction.log");

                FileStream fs = new FileStream(fname, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                logFileStream = new StreamWriter(fs);

                //var fname = string.Format(sceneName + "_RE_LogDialogueInteraction.log");
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



    protected override void ValueChanged(string key)
    {
        // in this method we listen for all key changes
        //  how we act depends on what key changed

        // rewrite the stack with the new blackboard state
        // this only triggers a state change in a few cases
        //  (see DialogueStateMachine.cs)

        stateMachine.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
            stateMachine.GenerateStackSymbol(DataStore.instance)));

        // only if interacting
        if (DataStore.GetBoolValue("user:isInteracting"))
        {
            // if we're question answering
            if (DataStore.GetBoolValue("me:question:isAnswering"))
            {

                // never mind exits the question answering loop
                if (key == "user:intent:isNevermind")
                {
                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("user:intent:isQuestion", false, string.Empty);
                        SetValue("me:question:isAnswering", false, string.Empty);
                        //SetValue("me:speech:intent", "OK. Nevermind", string.Empty);
                    }
                }
                // so does posack
                // TODO: maybe Diana should say something different after nevermind vs. after posack
                else if (key == "user:intent:posack")
                {
                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("user:intent:isQuestion", false, string.Empty);
                        SetValue("me:question:isAnswering", false, string.Empty);
                    }
                }
            }
            // if we're affordance learning
            //else if (DataStore.GetBoolValue("me:affordances:isLearning")) {
            //    // never mind reactivates all possible grasps (not learning one in particular)
            //    //  and empties targets, affordance learning events, returns to default pose,
            //    //  exits learning state
            //    if (key == "user:intent:isNevermind") {
            //        if (DataStore.GetBoolValue(key)) {
            //            GameObject graspedObj = GameObject.Find(DataStore.GetStringValue("user:intent:object"));
            //            // get all interaction targets
            //            List<InteractionObject> graspPoses = graspedObj.GetComponentsInChildren<Transform>(true)
            //                .Where(t => t.gameObject.GetComponent<InteractionObject>() != null).Select(g => g.GetComponent<InteractionObject>()).ToList();

            //            // reactivate all interaction targets
            //            foreach (InteractionObject graspPose in graspPoses) {
            //                graspPose.gameObject.SetActive(true);
            //            }

            //            SetValue("user:intent:object", DataStore.StringValue.Empty, string.Empty);
            //            SetValue("user:intent:lastEvent", DataStore.StringValue.Empty, string.Empty);
            //            SetValue("user:intent:append:event", DataStore.StringValue.Empty, string.Empty);
            //            SetValue("me:intent:lookAt", "user", string.Empty);
            //            SetValue("me:intent:targetObj", string.Empty, string.Empty);
            //            SetValue("me:intent:action", string.Empty, string.Empty);
            //            SetValue("me:affordances:isLearning", false, string.Empty);
            //            SetValue("me:oneshot:isLearning", false, string.Empty);
            //            SetValue("me:speech:intent", "OK, never mind.", string.Empty);
            //        }
            //    }
            //    // posack while affordance learning can mean:
            //    //  1) this is the grasp pose I want Diana to learn (if the object has no grasp convention associated with it
            //    //  2) the gesture was just learned and I'd like to move on (e.g., let go of the object)
            //    else if (key == "user:intent:isPosack")
            //    {
            //        if (DataStore.GetBoolValue(key))
            //        {
            //            GameObject graspedObj = GameObject.Find(DataStore.GetStringValue("user:intent:object"));
            //            Voxeme graspedObjVox = graspedObj.GetComponent<Voxeme>();

            //            // if there's no assigned grasp convention
            //            if (graspedObjVox.graspConvention == null)
            //            {
            //                // get all interaction objects
            //                List<InteractionObject> graspPoses = graspedObj.GetComponentsInChildren<Transform>(true)
            //                    .Where(t => t.gameObject.GetComponent<InteractionObject>() != null).Select(g => g.GetComponent<InteractionObject>()).ToList();
            //                Debug.Log(string.Format("{0} has {1} grasp poses", graspedObj.name, graspPoses.Count));
            //                // get the (first active) interaction objects
            //                InteractionObject activeGrasp = graspedObj.GetComponentsInChildren<InteractionObject>().First(g => g.isActiveAndEnabled);
            //                Debug.Log(string.Format("{0} active grasp is {1} (index {2})", graspedObj.name, activeGrasp.name, graspPoses.IndexOf(activeGrasp)));

            //                // grasp poses ending with 0 are the default "claw" pose
            //                // not every object will use this
            //                if (activeGrasp.name.EndsWith("0"))
            //                {
            //                    // set grasp convention
            //                    if (graspedObjVox != null)
            //                    {
            //                        graspedObjVox.graspConvention = activeGrasp.gameObject;
            //                    }

            //                    // stop learning
            //                    SetValue("me:affordances:isLearning", false, string.Empty);
            //                    SetValue("me:speech:intent", "OK.", string.Empty);
            //                }
            //                else
            //                {
            //                    // set grasp convention
            //                    if (graspedObjVox != null)
            //                    {
            //                        graspedObjVox.graspConvention = activeGrasp.gameObject;
            //                    }

            //                    // ask to define a gesture
            //                    // start learning
            //                    SetValue("me:intent:lookAt", "user", string.Empty);
            //                    SetValue("me:speech:intent", "Is there a gesture for that?",
            //                        string.Format("Asking for new gesture to define {0}", activeGrasp.name));
            //                    SetValue("me:oneshot:isLearning", true, "Starting learning");
            //                    learningUpdateTimer.Enabled = true;
            //                }
            //            }
            //            // there's a grasp convention assigned now
            //            //  posack means let it go
            //            else
            //            {
            //                SetValue("me:affordances:isLearning", false, string.Empty);
            //                SetValue("user:intent:partialEvent", string.Format("ungrasp({0})", graspedObj.name), string.Empty);
            //            }
            //        }
            //    }
            //    // negack while affordance learning can mean:
            //    //  1) do not learn a new gesture to grasp this object
            //    //  2) try a different grasp pose and ask if she should learn it
            //    else if (key == "user:intent:isNegack")
            //    {
            //        if (DataStore.GetBoolValue(key))
            //        {
            //            // no new gesture
            //            if (DataStore.GetBoolValue("me:oneshot:isLearning"))
            //            {
            //                learningUpdateTimer.Interval = learningUpdateTimerTime;
            //                learningUpdateTimer.Enabled = false;
            //                SetValue("me:speech:intent", "OK, I won't learn a gesture for this object.", string.Empty);
            //                SetValue("me:oneshot:isLearning", false, "Aborting learning");
            //                SetValue("me:affordances:isLearning", false, string.Empty);
            //            }
            //            // try next grasp pose
            //            else
            //            {
            //                //TODO: move grasp pose finding, activating, deactivating to InteractionHelper
            //                // regrasp
            //                GameObject graspedObj = GameObject.Find(DataStore.GetStringValue("user:intent:object"));
            //                // get all interaction targets
            //                List<InteractionObject> graspPoses = graspedObj.GetComponentsInChildren<Transform>(true)
            //                    .Where(t => t.gameObject.GetComponent<InteractionObject>() != null).Select(g => g.GetComponent<InteractionObject>()).ToList();
            //                Debug.Log(string.Format("{0} has {1} grasp poses", graspedObj.name, graspPoses.Count));
            //                // get the (first active) interaction target
            //                InteractionObject activeGrasp = graspedObj.GetComponentsInChildren<InteractionObject>().First(g => g.isActiveAndEnabled);
            //                Debug.Log(string.Format("{0} active grasp is {1} (index {2})", graspedObj.name, activeGrasp.name, graspPoses.IndexOf(activeGrasp)));

            //                // out of grasp poses
            //                //  exit to confusion
            //                if (graspPoses.IndexOf(activeGrasp) == graspPoses.Count - 1)
            //                {
            //                    // out of pose options
            //                    SetValue("user:intent:object", DataStore.StringValue.Empty, string.Empty);
            //                    SetValue("user:intent:lastEvent", DataStore.StringValue.Empty, string.Empty);
            //                    SetValue("user:intent:append:event", DataStore.StringValue.Empty, string.Empty);
            //                    SetValue("me:intent:lookAt", "user", string.Empty);
            //                    SetValue("me:intent:targetObj", string.Empty, string.Empty);
            //                    SetValue("me:intent:action", string.Empty, string.Empty);
            //                    SetValue("me:affordances:isLearning", false, string.Empty);
            //                    SetValue("me:speech:intent", "Sorry, I'm confused.", string.Empty);
            //                    SetValue("me:emotion", "confusion", string.Empty);

            //                    // reactivate all interaction targets
            //                    foreach (InteractionObject graspPose in graspPoses)
            //                    {
            //                        graspPose.gameObject.SetActive(true);
            //                    }
            //                }
            //                // try next one
            //                else
            //                {
            //                    // deactivate current one
            //                    graspPoses[graspPoses.IndexOf(activeGrasp)].gameObject.SetActive(false);
            //                    // activate next one
            //                    graspPoses[graspPoses.IndexOf(activeGrasp) + 1].gameObject.SetActive(true);
            //                    Debug.Log(string.Format("{0} active grasp is now {1} (index {2})", graspedObj.name, graspPoses[graspPoses.IndexOf(activeGrasp) + 1].name,
            //                        graspPoses.IndexOf(activeGrasp) + 1));
            //                    SetValue("me:intent:action", string.Empty, string.Empty);
            //                    Debug.Log(string.Format("Set me:intent:action to string.Empty: {0}", string.IsNullOrEmpty(DataStore.GetStringValue("me:intent:action"))));
            //                    SetValue("me:intent:action", "grasp", string.Empty);

            //                    SetValue("me:intent:lookAt", "user", string.Empty);
            //                    // set some variability in her dialogye
            //                    if ((graspPoses.IndexOf(activeGrasp) + 1 % 2) == 0)
            //                    {
            //                        SetValue("me:speech:intent", "Should I grasp it like this?", string.Empty);
            //                    }
            //                    else
            //                    {
            //                        SetValue("me:speech:intent", "How about like this?", string.Empty);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    // otherwise, a new programmatic gesture should exit affordance learning
            //    //  since there's no new gesture being made and held
            //    else if ((key == "user:intent:isPushLeft") || (key == "user:intent:isPushRight") ||
            //        (key == "user:intent:isPushFront") || (key == "user:intent:isPushBack") ||
            //        (key == "user:intent:isClaw") || (key == "user:isPointing"))
            //    {
            //        if (DataStore.GetBoolValue("me:oneshot:isLearning"))
            //        {
            //            if (DataStore.GetBoolValue(key))
            //            {
            //                SetValue("me:affordances:isLearning", false, string.Empty);
            //            }
            //        }
            //    }
            //}

            //  the below is not an else statement because some of them check if 
            //  something above caused us to exit affordance learning
            //  the rest behave as normal in a non-affordance learning situation
            if (!DataStore.GetBoolValue("me:affordances:isLearning"))
            {
                if (key == "user:intent:isNevermind" || key == "user:intent:isNegack")
                {
                    Log("Nevermind" + "  |  " + "True");
                    if (DataStore.GetBoolValue(key))
                    {
                        // for all servos - if true, stop
                        if (DataStore.GetBoolValue("user:intent:isServoLeft"))
                        {
                            SetValue("user:intent:isServoLeft", false, string.Empty);

                        }
                        else if (DataStore.GetBoolValue("user:intent:isServoRight"))
                        {
                            SetValue("user:intent:isServoRight", false, string.Empty);

                        }
                        else if (DataStore.GetBoolValue("user:intent:isServoFront"))
                        {
                            SetValue("user:intent:isServoFront", false, string.Empty);
                        }
                        else if (DataStore.GetBoolValue("user:intent:isServoBack"))
                        {
                            SetValue("user:intent:isServoBack", false, string.Empty);
                        }
                        // handle nevermind for lastEvent
                        else if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:lastEvent")))
                        {
                            string lastEventStr = DataStore.GetStringValue("user:intent:lastEvent");
                            Log("user:intent:lastEvent" + "  |  " + DataStore.GetStringValue("user:intent:lastEvent"));

                            // Nada: undo for pointing and replace the content 
                            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")) && !DataStore.GetStringValue("user:intent:replaceContent").Contains("{0}"))
                            {
                                Debug.Log("Nada: DIM: RC " + DataStore.GetStringValue("user:intent:replaceContent"));

                                UndoLastEvent(DataStore.GetStringValue("user:intent:replaceContent"));
                                Log("user:intent:replaceContent" + "  |  " + DataStore.GetStringValue("user:intent:lastEvent"));
                                // focusObject = DataStore.GetStringValue("user:intent:object");
                                //ForgetFocusObject();
                            }
                            // Nada: e.g., no this block
                            else if (DataStore.GetStringValue("user:intent:replaceContent").Contains("{0}"))
                            {
                                SetValue("user:intent:object", DataStore.GetStringValue("user:lastPointedAt:name"), string.Empty);
                                SetValue("user:intent:partialEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                            }
                            else
                            {
                                // focusObject = DataStore.GetStringValue("user:intent:object");
                                UndoLastEvent();
                                ForgetFocusObject();
                            }
                        }
                        // handle nevermind for objects - e.g., Pointing only
                        else if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
                        {
                            if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                            {
                                Debug.Log("Nada: DIM: RC1 " + DataStore.GetStringValue("user:intent:replaceContent"));
                                SetValue("user:intent:lastEvent", DataStore.GetStringValue("user:intent:replaceContent"), string.Empty);
                                UndoLastEvent(DataStore.GetStringValue("user:intent:replaceContent"));
                                Log("user:intent:replaceContent" + "  |  " + DataStore.GetStringValue("user:intent:lastEvent"));
                                SetValue("me:intent:lookAt", "user", string.Empty);
                                SetValue("me:intent:targetObj", string.Empty, string.Empty);
                            }
                            else
                            {
                                //focusObject = DataStore.GetStringValue("user:intent:object");
                                ForgetFocusObject();
                            }
                            Log("user:intent:object" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                        }

                        if (!emm.getDisam() && !emm.getCdisam() &&
                            string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent"))) ForgetFocusObject();
                        // get frustrated
                        SetValue("me:emotion", "frustration", string.Empty);
                    }
                }
                // fill in push actions 
                else if (key == "user:intent:isPushLeft")
                {
                    Log("user:intent:isPushLeft" + "  |  " + DataStore.GetBoolValue("user:intent:isPushLeft"));
                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("user:intent:action", "slide({0},{1}(left))", string.Empty);
                        // Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                    }
                }
                else if (key == "user:intent:isPushRight")
                {
                    Log("user:intent:isPushRight" + "  |  " + DataStore.GetBoolValue("user:intent:isPushRight"));

                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("user:intent:action", "slide({0},{1}(right))", string.Empty);
                        //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                    }
                }
                else if (key == "user:intent:isPushFront")
                {
                    Log("user:intent:isPushFront" + "  |  " + DataStore.GetBoolValue("user:intent:isPushFront"));

                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("user:intent:action", "slide({0},{1}(front))", string.Empty);
                        //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                    }
                }
                else if (key == "user:intent:isPushBack")
                {
                    Log("user:intent:isPushBack" + "  |  " + DataStore.GetBoolValue("user:intent:isPushBack"));

                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("user:intent:action", "slide({0},{1}(back))", string.Empty);
                        //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                    }
                }
                // fill in servo actions and start timer
                else if (key == "user:intent:isServoLeft")
                {

                    if (DataStore.GetBoolValue(key))
                    {
                        // if servo is true and there isn't already a slide on the blackboard
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the intended action
                            SetValue("user:intent:action", "servo({0},{1}(left))", string.Empty);
                            SetValue("me:isCheckingServo", false, string.Empty);
                            //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                            servoLoopTimer.Interval = servoLoopTimerTime;
                            servoLoopTimer.Enabled = true;
                        }
                    }
                    else
                    {
                        // if servo is false and there isn't already a slide on the blackboard
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the appended action
                            SetValue("user:intent:append:action", "servo({0},{1}(left)).  ungrasp({0})", string.Empty);
                            //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                        }
                        SetValue("user:intent:action", string.Empty, string.Empty);
                        SetValue("me:isCheckingServo", false, string.Empty);
                        SetValue("me:isInServoLoop", false, string.Empty);

                        servoLoopTimer.Interval = servoLoopTimerTime;
                        servoLoopTimer.Enabled = false;
                    }
                }
                else if (key == "user:intent:isServoRight")
                {
                    if (DataStore.GetBoolValue(key))
                    {
                        // if servo is true and there isn't already a slide on the blackboard
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the intended action
                            SetValue("user:intent:action", "servo({0},{1}(right))", string.Empty);
                            SetValue("me:isCheckingServo", false, string.Empty);
                            //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                            servoLoopTimer.Interval = servoLoopTimerTime;
                            servoLoopTimer.Enabled = true;
                        }
                    }
                    else
                    {
                        // if servo is false and there isn't already a slide on the blackboard
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the appended action
                            SetValue("user:intent:append:action", "servo({0},{1}(right)).  ungrasp({0})", string.Empty);
                        }
                        SetValue("user:intent:action", string.Empty, string.Empty);
                        SetValue("me:isCheckingServo", false, string.Empty);
                        SetValue("me:isInServoLoop", false, string.Empty);

                        servoLoopTimer.Interval = servoLoopTimerTime;
                        servoLoopTimer.Enabled = false;
                    }
                }
                else if (key == "user:intent:isServoFront")
                {
                    if (DataStore.GetBoolValue(key))
                    {
                        // if servo is true and there isn't already a slide on the blackboard
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the intended action
                            SetValue("user:intent:action", "servo({0},{1}(front))", string.Empty);
                            SetValue("me:isCheckingServo", false, string.Empty);
                            //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                            servoLoopTimer.Interval = servoLoopTimerTime;
                            servoLoopTimer.Enabled = true;
                        }
                    }
                    else
                    {
                        // if servo is false and there isn't already a slide on the blackboard
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the appended action
                            SetValue("user:intent:append:action", "servo({0},{1}(front)).  ungrasp({0})", string.Empty);
                            //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                        }
                        SetValue("user:intent:action", string.Empty, string.Empty);
                        SetValue("me:isCheckingServo", false, string.Empty);
                        SetValue("me:isInServoLoop", false, string.Empty);

                        servoLoopTimer.Interval = servoLoopTimerTime;
                        servoLoopTimer.Enabled = false;
                    }
                }
                else if (key == "user:intent:isServoBack")
                {
                    // if servo is true and there isn't already a slide on the blackboard
                    if (DataStore.GetBoolValue(key))
                    {
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the intended action
                            SetValue("user:intent:action", "servo({0},{1}(back))", string.Empty);
                            SetValue("me:isCheckingServo", false, string.Empty);
                            //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                            servoLoopTimer.Interval = servoLoopTimerTime;
                            servoLoopTimer.Enabled = true;
                        }
                    }
                    else
                    {
                        // if servo is false and there isn't already a slide on the blackboard
                        if (GlobalHelper.GetTopPredicate(DataStore.GetStringValue("user:intent:action")) != "slide")
                        {
                            // then set servo as the appended action
                            SetValue("user:intent:append:action", "servo({0},{1}(back)).  ungrasp({0})", string.Empty);
                        }
                        SetValue("user:intent:action", string.Empty, string.Empty);
                        SetValue("me:isCheckingServo", false, string.Empty);
                        SetValue("me:isInServoLoop", false, string.Empty);

                        servoLoopTimer.Interval = servoLoopTimerTime;
                        servoLoopTimer.Enabled = false;
                    }
                }
                // fill in grasp action 
                else if (key == "user:intent:isClaw")
                {
                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("user:intent:action", "grasp({0})", string.Empty);
                        //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                    }
                }
                // if pointing is no longer true 
                else if (key == "user:isPointing")
                {
                    if (!DataStore.GetBoolValue(key))
                    {
                        // clear lastPointedAt
                        SetValue("user:lastPointedAt:name", DataStore.StringValue.Empty, string.Empty);
                        //Log("user:lastPointedAt:name" + "  |  " + DataStore.GetStringValue("user:lastPointedAt:name"));

                        SetValue("user:lastPointedAt:position", DataStore.Vector3Value.Zero, string.Empty);
                        //Log("user:lastPointedAt:position" + "  |  " + DataStore.GetVector3Value("user:lastPointedAt:position"));
                    }
                }
                // if pointing at an object
                else if (key == "user:lastPointedAt:name")
                {
                    //  Debug.Log(string.Format("user:intent:object = {0}; user:lastPointedAt:name = {1};",
                    //    DataStore.GetStringValue("user:intent:object"), DataStore.GetStringValue("user:lastPointedAt:name")));

                    if (!string.IsNullOrEmpty(DataStore.GetStringValue(key)))
                    {
                        // set object intent if Diana is paying attention
                        if (DataStore.GetBoolValue("me:isAttendingPointing"))
                        {

                            if (DataStore.GetStringValue("user:lastPointedAt:name") != DataStore.GetStringValue("me:intent:lastTheme"))
                            {
                                logged = false;
                                //Log("key == user:lastPointedAt:name");
                                SetObjectIntent();
                                //Log("user:intent:object _ focus" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                                //Log("me:intent:lastTheme _ target" + "  |  " + DataStore.GetStringValue("user:lastPointedAt:name"));
                                //Log("user:lastPointedAt:name" + "  |  " + DataStore.GetStringValue("user:lastPointedAt:name"));
                                //Log("-------------------------------------------------------------------------------------------------");

                            }
                        }
                    }
                }
                // if pointing at a location
                else if (key == "user:lastPointedAt:position")
                {
                    Debug.Log("Nada: 1 user:lastPointedAt:position ");

                    // set location intent if Diana is paying attention
                    if (DataStore.GetBoolValue("me:isAttendingPointing"))
                    {
                        Debug.Log("1 SetLocationIntent");

                        SetLocationIntent();
                    }
                }
                // if an object has been focused
                else if (key == "user:intent:object")
                {
                    if (!string.IsNullOrEmpty(DataStore.GetStringValue(key)))
                    {
                        // Log("user:intent:object" + "  |  " + DataStore.GetStringValue("user:intent:object"));

                        if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:action")))
                        {
                            //Log("user:intent:action" + "  |  " + DataStore.GetStringValue("user:intent:action"));

                            // point/look at it if not already doing anything else
                            SetValue("me:intent:action", "point", string.Empty);
                            //Log("me:intent:action" + "  |  " + DataStore.GetStringValue("me:intent:action"));
                            SetValue("me:intent:lookAt", DataStore.GetStringValue(key), string.Empty);
                            SetValue("me:intent:targetObj", DataStore.GetStringValue(key), string.Empty);
                            //Log("me:intent:targetObj" + "  |  " + DataStore.GetStringValue("me:intent:targetObj"));

                        }
                    }
                }
                // Diana gets happy when you give her a thumbs up
                else if (key == "user:intent:isPosack")
                {
                    if (DataStore.GetBoolValue(key))
                    {
                        SetValue("me:emotion", "joy", string.Empty);
                    }
                }
                // if attending pointing changed
                else if (key == "me:isAttendingPointing")
                {
                    Debug.Log("key == me: isAttendingPointing");

                    // if it's now true
                    if (DataStore.GetBoolValue(key))
                    {
                        // if user was pointing somewhere when isAttendingPointing
                        //  changed back to true, attend to that
                        if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:lastPointedAt:name")))
                        {
                            Debug.Log("lastPointedAt Nada");

                            if (DataStore.GetStringValue("user:lastPointedAt:name") != DataStore.GetStringValue("me:intent:lastTheme"))
                            {
                                logged = true;
                                Debug.Log("SetObjectIntent");
                                SetObjectIntent();
                                // location
                                //Log("user:intent:object _ focus" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                                //Log("user:lastPointedAt:name _ target" + "  |  " + DataStore.GetStringValue("user:lastPointedAt:name"));
                            }
                        }
                        else
                        {
                            SetLocationIntent();
                            Debug.Log("2 SetLocationIntent");
                            //Log("user:lastPointedAt:position" + "  |  " + DataStore.GetVector3Value("user:lastPointedAt:position"));
                        }
                    }
                }
            }
        }
    }
    //void SetObjectIntent()
    //{
    //    GameObject target = GameObject.Find(DataStore.GetStringValue("user:lastPointedAt:name"));
    //    GameObject blocker = null;

    //    Debug.Log(string.Format("SetObjectIntent: user:intent:object = {0}; user:lastPointedAt:name = {1}; SurfaceClear({1}) = {2}",
    //        DataStore.GetStringValue("user:intent:object"), target.name, DialogueUtility.SurfaceClear(target, out blocker)));

    //    // if no object is currently in focus
    //    if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
    //    {
    //        // if no current user:intent:object and the pointed-at object has a clear surface
    //        if (DialogueUtility.SurfaceClear(target, out blocker))
    //        {
    //            // make the pointed-at object the focus object
    //            SetValue("user:intent:object", DataStore.GetStringValue("user:lastPointedAt:name"), string.Empty);
    //        }
    //        // otherwise look for objects that are blocking this object until one
    //        //  with a clear surface is found
    //        else
    //        {
    //            do
    //            {
    //                bool surfaceClear = DialogueUtility.SurfaceClear(target, out blocker);
    //                if (blocker != null)
    //                {
    //                    target = blocker;
    //                }
    //            } while (blocker != null);
    //            SetValue("user:intent:object", target.name, string.Empty);
    //        }
    //    }
    //    // if an object is currently in focus
    //    else
    //    {
    //        // if the pointed at object is not the current focus/theme object
    //        if (DataStore.GetStringValue("user:intent:object") != DataStore.GetStringValue("user:lastPointedAt:name"))
    //        {
    //            // then place the theme object relative to the pointed at object

    //            // get the pointed-at location
    //            Vector3 curPointPos = DataStore.GetVector3Value("user:pointPos");
    //            // if the pointed-at location is not within the bounds of the pointed-at object
    //            if (!GlobalHelper.ContainingObjects(curPointPos).Contains(target))
    //            {
    //                Debug.Log(string.Format("{0} does not contain {1}", DataStore.GetStringValue("user:lastPointedAt:name"),
    //                    GlobalHelper.VectorToParsable(curPointPos)));
    //                Bounds objBounds = GlobalHelper.GetObjectWorldSize(target);
    //                // place the theme next to that object at the appropriate location
    //                if (curPointPos.x <= objBounds.min.x)
    //                {
    //                    SetValue("user:intent:partialEvent",
    //                        string.Format("put({0},left({1}))",
    //                        DataStore.GetStringValue("user:intent:object"),
    //                        target.name), string.Empty);
    //                }
    //                else if (curPointPos.x >= objBounds.max.x)
    //                {
    //                    SetValue("user:intent:partialEvent",
    //                        string.Format("put({0},right({1}))",
    //                        DataStore.GetStringValue("user:intent:object"),
    //                        target.name), string.Empty);
    //                }
    //                else if (curPointPos.z <= objBounds.min.z)
    //                {
    //                    SetValue("user:intent:partialEvent",
    //                        string.Format("put({0},in_front({1}))",
    //                        DataStore.GetStringValue("user:intent:object"),
    //                        target.name), string.Empty);
    //                }
    //                else if (curPointPos.z >= objBounds.max.z)
    //                {
    //                    SetValue("user:intent:partialEvent",
    //                        string.Format("put({0},behind({1}))",
    //                        DataStore.GetStringValue("user:intent:object"),
    //                        target.name), string.Empty);
    //                }
    //            }
    //            // otherwise put the theme on top of that object
    //            else if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
    //            {
    //                Debug.Log(string.Format("{0} contains {1}", target.name,
    //                    GlobalHelper.VectorToParsable(curPointPos)));
    //                // in case user is pointing at the lower portions of a stack
    //                //  find a block in that stack with a clear surface, i.e., the topmost block
    //                do
    //                {
    //                    bool surfaceClear = DialogueUtility.SurfaceClear(target, out blocker);
    //                    if ((blocker != null) && (blocker != GameObject.Find(DataStore.GetStringValue("user:intent:object"))))
    //                    {
    //                        target = blocker;
    //                    }
    //                } while ((blocker != null) && (blocker != GameObject.Find(DataStore.GetStringValue("user:intent:object"))));
    //                SetValue("user:intent:partialEvent",
    //                    string.Format("put({0},on({1}))",
    //                    DataStore.GetStringValue("user:intent:object"),
    //                    target.name), string.Empty);
    //            }
    //        }
    //    }
    //}

    // this method will infer the target object based on where the user points
    //  or will infer an intended event to execute if a theme object is already on
    //  the blackboard
    void SetObjectIntent()
    {
        //Log("---------------------------------NEW POINTING LOGS----------------------------------------");


        GameObject target = GameObject.Find(DataStore.GetStringValue("user:lastPointedAt:name"));
        GameObject AmbigBlock = null;
        List<object> PointingTargetAttr;
        List<object> SpeechTargetAttr = null;
        GameObject blocker = null;
        // attributes of the last pointed at block (target by pointing)
        PointingTargetAttr = target.GetComponent<AttributeSet>().attributes.Cast<object>().ToList();

        if (emm.getDisam())
        {
            //the block the user asked to pick by speech (target by speech)
            AmbigBlock = GameObject.Find(emm.getambigBlock());
            // attributes of the target mention in the speech 
            SpeechTargetAttr = AmbigBlock.GetComponent<AttributeSet>().attributes.Cast<object>().ToList();
        }


        // if no object is currently in focus
        if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
        {

            // if no current user:intent:object and the pointed-at object has a clear surface
            if (DialogueUtility.SurfaceClear(target, out blocker) /*&& emm.getDisam()*/)
            {
                Debug.Log(string.Format("1: SetObjectIntent: user:intent:object = {0}; user:lastPointedAt:name = {1}; SurfaceClear({1}) = {2}",
                DataStore.GetStringValue("user:intent:object"), target.name, DialogueUtility.SurfaceClear(target, out blocker)));

                // Nada: to ensure that the target mentioned in speech is the same as
                // target mentioned by pointing 
                if (emm.getDisam())
                {
                    //if (PointingTargetAttr[0].Equals(SpeechTargetAttr[0]))
                    //{
                        // make the pointed-at object the focus object
                        SetValue("user:intent:object", DataStore.GetStringValue("user:lastPointedAt:name"), string.Empty);
                        emm.setDisam(false);
                        Debug.Log(string.Format("2: Nada: DIM: ENTERED: {0}", emm.getDisam().ToString()));

                        if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                        {
                            focuspointing = "After Ask for Disambiguation";
                            DianaFocusPointing = "disambiguate before pointing";
                            IsDianaDis = "Yes";
                            DianaTargetPointing = "None";
                            //HumanModality = "Speech only";
                            //dianaMpdality = "Speech only";
                            humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                            targetpointing = "";
                            focusObject = DataStore.GetStringValue("user:intent:object");
                            Debug.Log(string.Format(" Nada: DIM: focusObject: {0}", focusObject));
                            focusPosition = GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position.ToString();
                            targetObject = "None";
                            AddToDialogueHist();
                            SetValue("user:intent:replaceContent", string.Empty, string.Empty);
                            SetValue("user:intent:append:event", string.Empty, string.Empty);
                            issetobj = true;
                        }
                        focuspointing = "After Ask for Disambiguation";
                        DianaFocusPointing = "disambiguate before pointing";
                        DianaTargetPointing = "None";
                        IsDianaDis = "Yes";
                        //HumanModality = "Speech only";
                        //dianaMpdality = "Speech only";
                        humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                        targetpointing = "";
                        focusObject = DataStore.GetStringValue("user:intent:object");
                        Debug.Log(string.Format(" Nada: DIM: focusObject: {0}", focusObject));
                        focusPosition = GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position.ToString();
                        targetObject = "None";
                        // to save data in .log file 
                        Log("---------------------------------Pointing to the FOCUS After Disambiguation----------------------------------------");
                        Log("user:intent:object _ focus" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                        Log("Focus object position" + "  |  " + GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position);
                        Log("Distance from agent to focus obj:" + Distance(DataStore.GetStringValue("user:intent:object")));
                        Log("Diana points" + "  |  " + DataStore.GetStringValue("me:intent:targetObj"));
                        Log("--------------------------------- History Stack after SetObjectIntent ----------------------------------------");
                        issetobj = true;
                        AddToDialogueHist();
                        visited = true;
                    //}
                    //else
                    //{
                    //    //disRespond(target, SpeechTargetAttr);
                    //    Debug.Log(string.Format("233: Nada: DIM: ENTERED: {0}", emm.getDisam().ToString()));
                    //    SetValue("user:intent:object", string.Empty, string.Empty);

                    //    // to save data in csv file 
                    //    focuspointing = string.Empty;
                    //    targetpointing = string.Empty;
                    //    humanfocuspointTime = null;
                    //    focusObject = string.Empty;
                    //    focusPosition = null;
                    //    targetObject = string.Empty;
                    //    visited = false;
                    //}

                }
                // Nada: e.g., User: take this red block, Diana: which red block?
                else if (emm.getCdisam())
                {
                    //if (PointingTargetAttr[0].ToString().Equals(dp.getColor()))
                    //{
                        emm.setCdisam(false);
                        SetValue("user:intent:object", DataStore.GetStringValue("user:lastPointedAt:name"), string.Empty);

                        Debug.Log(string.Format("2: Nada: color: ENTERED: {0}", emm.getCdisam().ToString()));
                        if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent")))
                        {
                            focuspointing = "After Ask for Disambiguation";
                            DianaFocusPointing = "disambiguate before pointing";
                            IsDianaDis = "Yes";
                            DianaTargetPointing = "None";
                            //HumanModality = "Speech only";
                            //dianaMpdality = "Speech only";
                            targetpointing = "";
                            humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                            focusObject = DataStore.GetStringValue("user:intent:object");
                            Debug.Log(string.Format(" Nada: DIM: focusObject: {0}", focusObject));
                            focusPosition = GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position.ToString();
                            targetObject = "None";
                            issetobj = true;
                            AddToDialogueHist();

                            SetValue("user:intent:replaceContent", string.Empty, string.Empty);
                            SetValue("user:intent:append:event", string.Empty, string.Empty);
                        }
                        // SetValue("user:intent:object", DataStore.GetStringValue("user:lastPointedAt:name"), string.Empty);
                        // to save data in csv file 
                        focuspointing = "After Ask for Disambiguation";
                        DianaFocusPointing = "disambiguate before pointing";
                        IsDianaDis = "Yes";
                        DianaTargetPointing = "None";
                        //HumanModality = "Speech only";
                        //dianaMpdality = "Speech only";
                        humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                        targetpointing = "None";
                        focusObject = DataStore.GetStringValue("user:intent:object");
                        targetObject = "None";
                        focusPosition = GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position.ToString();
                        issetobj = true;
                        // to save data in .log file 
                        Log("---------------------------------Pointing to the FOCUS After Disambiguation----------------------------------------");
                        Log("user:intent:object _ focus" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                        Log("Focus object position" + "  |  " + GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position);
                        Log("Distance from agent to focus obj:" + Distance(DataStore.GetStringValue("user:intent:object")));
                        Log("Diana points" + "  |  " + DataStore.GetStringValue("me:intent:targetObj"));
                        Log("------------------------------------------------------------------------------");
                        Log("--------------------------------- History Stack after SetObjectIntent ----------------------------------------");

                        AddToDialogueHist();
                        visited = true;
                    //}
                    //else
                    //{
                    //    disRespond(target, dp.getColor());
                    //    SetValue("user:intent:object", string.Empty, string.Empty);
                    //    Debug.Log(string.Format("22: Nada: color: ENTERED: {0}", emm.getDisam().ToString()));
                    //    // to save data in csv file 
                    //    focuspointing = "No Appropriate Pointiing to the Focus After Ask for Disambiguation";
                    //    targetpointing = "";
                    //    humanfocuspointTime = null;
                    //    focusObject = "Not selected properly";
                    //    targetObject = "None";
                    //    focusPosition = null;
                    //    visited = false;
                    //}


                }

                // if there is no disambiguation
                else if (!emm.getCdisam() && !emm.getDisam())
                {
                    // make the pointed-at object the focus object
                    SetValue("user:intent:object", DataStore.GetStringValue("user:lastPointedAt:name"), string.Empty);
                    if (emm.getdemdis())
                    {
                        // to save data in .log file 
                        Log("---------------------------------Pointing to the FOCUS After Disambiguation----------------------------------------");
                        Log("user:intent:object _ focus" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                        Log("Focus object position" + "  |  " + GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position);
                        Log("Distance from agent to focus obj:" + Distance(DataStore.GetStringValue("user:intent:object")));
                        Log("Diana points" + "  |  " + DataStore.GetStringValue("me:intent:targetObj"));
                        Log("--------------------------------- History Stack after SetObjectIntent ----------------------------------------");

                        // to save data in csv file 
                        focuspointing = "After Ask for Disambiguation";
                        DianaFocusPointing = "disambiguate before pointing";
                        IsDianaDis = "Yes";
                        DianaTargetPointing = "None";
                        //HumanModality = "Speech only";
                        //dianaMpdality = "Speech only";
                        targetpointing = "";
                        humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                        focusObject = DataStore.GetStringValue("user:intent:object");
                        targetObject = "None";
                        focusPosition = GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position.ToString();
                        issetobj = true;
                        AddToDialogueHist();
                        visited = true;
                    }
                    else
                    {
                        // to save data in .log file 
                        Log("---------------------------------Pointiing to the FOCUS without Speech----------------------------------------");
                        Log("user:intent:object _ focus" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                        Log("Focus object position" + "  |  " + GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position);
                        Log("Distance from agent to focus obj:" + Distance(DataStore.GetStringValue("user:intent:object")));
                        Log("Diana points" + "  |  " + DataStore.GetStringValue("me:intent:targetObj"));
                        Log("--------------------------------- History Stack after SetObjectIntent ----------------------------------------");

                        // to save data in csv file 
                        focuspointing = "Before Ask for Disambiguation";
                        DianaFocusPointing = "Pointing without disambiguation";
                        IsDianaDis = "No";
                        DianaTargetPointing = "None";
                        //HumanModality = "Multimodal";
                        //dianaMpdality = "Multimodal";
                        targetpointing = "";
                        humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                        focusObject = DataStore.GetStringValue("user:intent:object");
                        targetObject = "None";
                        focusPosition = GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position.ToString();
                        issetobj = true;
                        befdis = true;
                        visited = true;
                        //AddToDialogueHist();
                    }
                }
            }
            // if no surface clear
            // otherwise look for objects that are blocking this object until one
            // with a clear surface is found
            else
            {
                Debug.Log("5: Nada: DIM: ENTERED");
                Debug.Log(string.Format("2: SetObjectIntent: user:intent:object = {0}; user:lastPointedAt:name = {1}; SurfaceClear({1}) = {2}",
                DataStore.GetStringValue("user:intent:object"), target.name, DialogueUtility.SurfaceClear(target, out blocker)));

                do
                {
                    bool surfaceClear = DialogueUtility.SurfaceClear(target, out blocker);
                    if (blocker != null)
                    {
                        target = blocker;
                    }
                } while (blocker != null);
                SetValue("user:intent:object", target.name, string.Empty);

                // to save data in .log file 
                Log("---------------------------------Pointiing to the FOCUS without Speech----------------------------------------");
                Log("user:intent:object _ focus" + "  |  " + DataStore.GetStringValue("user:intent:object"));
                Log("Focus object position" + "  |  " + GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position);
                Log("Distance from agent to focus obj:" + Distance(DataStore.GetStringValue("user:intent:object")));
                Log("Diana points" + "  |  " + DataStore.GetStringValue("me:intent:targetObj"));
                Log("--------------------------------- History Stack after SetObjectIntent ----------------------------------------");

                // to save data in csv file 
                focuspointing = "Before Ask for Disambiguation";
                DianaFocusPointing = "pointing without disambiguation";
                IsDianaDis = "No";
                DianaTargetPointing = "None";
                //HumanModality = "Multimodal";
                //dianaMpdality = "Multimodal";
                targetpointing = "";
                humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                focusObject = DataStore.GetStringValue("user:intent:object");
                targetObject = "None";
                focusPosition = GameObject.Find(DataStore.GetStringValue("user:intent:object")).transform.position.ToString();
                visited = true;
                befdis = true;
                issetobj = true;
                AddToDialogueHist();

            }
        }
        // if an object is currently in focus
        else
        {
            // if the pointed at object is not the current focus/theme object
            if (DataStore.GetStringValue("user:intent:object") != DataStore.GetStringValue("user:lastPointedAt:name"))
            {
                // then place the theme object relative to the pointed at object
                // get the pointed-at location
                Vector3 curPointPos = DataStore.GetVector3Value("user:pointPos");
                // if Diana disambiguate the target block, when user refers to a block by speech

                if (emm.getDisam())
                {
                    //if (PointingTargetAttr[0].Equals(SpeechTargetAttr[0]))
                    //{
                        //speechonly = true;
                        nonpointing = true;
                        //pointonly = false;
                        // to put it on the side that utterred 
                        SpeechRelationalTransfer(curPointPos, target, blocker, AmbigBlock);
                        Debug.Log("NNada:SetObjectIntent:toSpeechRelationalTransfer: " + speechonly);

                        emm.setDisam(false);

                        if (!DataStore.GetStringValue("user:intent:object").Equals(target.name) && logged == false)
                        {
                            // logging to .log file 
                            Log("---------------------------------Pointiing to the TARGET After Disambiguation ----------------------------------------");
                            Log("target object" + "  |  " + target.name);
                            Log("target object position" + "  |  " + target.transform.position);
                            Log("---------------------------------The Partial Event----------------------------------------");
                            Log("partialEvent" + "  |  " + DataStore.GetStringValue("user:intent:partialEvent"));
                            Log("Distance from agent to focus obj:" + Distance(DataStore.GetStringValue("user:intent:object")));
                            Log("Diana executed the event");
                            Log("--------------------------------- History Stack after SetObjectIntent ----------------------------------------");

                            // logging to csv file
                            targetpointing = "After Ask for Disambiguation";
                            DianaTargetPointing = "disambiguate before acting";
                            IsDianaDis = "Yes";
                            humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                            targetpointTime = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                            //focuspointing = "";
                            focusObject = DataStore.GetStringValue("user:intent:object");
                            targetObject = target.name;
                            focusPosition = GameObject.Find(target.name).transform.position.ToString();
                            AddToDialogueHist();
                            visited = true;
                            issetobj = true;
                            befdis = true;
                        }
                    //}
                    //else
                    //{
                    //    disRespond(target, SpeechTargetAttr);
                    //    visited = false;
                    //}

                }
                // if there is no speech commands from a user and disambiguation from Diana, e.g., just use pointing
                else if (!nonpointing && !emm.getCdisam() && !emm.getDisam())
                {
                    //speechonly = false; 
                    PointingRelationalTransfer(curPointPos, target, blocker);
                    Debug.Log("NNada:SetObjectIntent:toPointingRelationalTransfer: " + pointonly);

                    if (!DataStore.GetStringValue("user:intent:object").Equals(target.name) && logged == false)
                    {
                        Log("---------------------------------Pointiing to the TARGET without Speech ----------------------------------------");
                        Log("target object" + "  |  " + target.name);
                        Log("target object position" + "  |  " + target.transform.position);
                        Log("Distance from agent to focus obj:" + Distance(DataStore.GetStringValue("user:intent:object")));
                        Log("---------------------------------The Partial Event----------------------------------------");
                        Log("partialEvent" + "  |  " + DataStore.GetStringValue("user:intent:partialEvent"));

                        // logging to csv file
                        targetpointing = "Pointing to move the focus to the target object";
                        targetpointTime = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                        focuspointing = "Before Ask for Disambiguation";
                        DianaFocusPointing = "pointing without disambiguation";
                        DianaTargetPointing = "Executing the event";
                        humanfocuspointTime = System.DateTime.Now.ToString("HH:mm:ss");
                        IsDianaDis = "No";
                        focusObject = DataStore.GetStringValue("user:intent:object");
                        targetObject = target.name;
                        //targetPosition = GameObject.Find(target.name).transform.position.ToString();
                        issetobj = true;
                        visited = true;
                        Log("--------------------------------- History Stack after SetObjectIntent ----------------------------------------");
                        AddToDialogueHist();
                    }
                }
            }
            else
            {
                SetLocationIntent();
            }
        }


        Debug.Log(string.Format(" Nada: DIM: focusObject: issetobj {0}", issetobj));
    }

    public void AddToDialogueHist()
    {
        string stro = "", stre = "", eventStr = "";
        if (!string.IsNullOrEmpty(emm.getRE_Category())) {
            if (emm.relfound == false && emm.getRE_Category().Equals("Relational"))
            {
                eventStr = emm.eventplf1;
                // Debug.Log(string.Format("111testNada : last_event: " + eventStr));
            }

            else
            {
                eventStr = DataStore.GetStringValue("user:intent:event");
                // Debug.Log(string.Format("222testNada : last_event: " + eventStr));
            }
            // Debug.Log(string.Format("444testNada : last_event: " + eventStr));
        }
        string last_event = GlobalHelper.GetTopPredicate(eventStr);
        string last_object = DataStore.GetStringValue("user:intent:object");

        // if (!emm.getRE_Category().Equals("Historical") && !emm.getRE_Category().Equals("Relational")) {
        if (last_event.Equals(""))
        {
            eventStr = DataStore.GetStringValue("user:intent:partialEvent");
            if (!eventStr.Equals(""))
                last_event = GlobalHelper.GetTopPredicate(eventStr);
            else if (SetLocation)
            {
                hisSetLocation = false;
                last_event = "put";
            }
        }

        Debug.Log(string.Format("Nada : last_event: " + last_event + " last_object: " + last_object));
        Debug.Log(string.Format("Nada : objtemp count: " + dh.getobjtemp().Count + " objtemp count: " + dh.getevtemp().Count));
        (eventHist_stack, objHist_stack) = dh.DialogueHistory_hre(last_object, last_event);
        setobjlist(objHist_stack.ToList());

        //Nada: logging dialogue history (objects and events)
        foreach (var o in objHist_stack) { stro = stro + " " + o; }
        foreach (var e in eventHist_stack) { stre = stre + " " + e; }
        if (!stro.Equals(""))
        {
            //Log("---------------------------------Object History Stack ----------------------------------------");

            Debug.Log("Nada: objects hist: " + stro);
            Log("Object history stack" + "  |  " + stro);
            objHist = stro;
        }
        if (!stre.Equals(""))
        {
            //Log("---------------------------------Events History Stack ----------------------------------------");
            Debug.Log("Nada:events hist: " + stre);
            Log("Events history stack" + "  |  " + stre);
            eveHist = stre;
        }
        relations = emm.RelationsLog();
    }

    // this method will infer the target location based on where the user points
    void SetLocationIntent()
    {
        // if no object is being pointed at
        if (string.IsNullOrEmpty(DataStore.GetStringValue("user:lastPointedAt:name")))
        {
            // if the pointed-at location is a valid location
            //  set user:intent:location to it
            if (DataStore.GetVector3Value("user:lastPointedAt:position") != default /*&& DataStore.GetBoolValue("user:pointValid")*/)
            {

                //if (speechonly == false) { 
                SetValue("user:intent:location", DataStore.GetVector3Value("user:lastPointedAt:position"), string.Empty);

                hisSetLocation = true;

                Debug.Log(string.Format("Setting user:intent:location to {0} ({1}) object {2}", DataStore.GetVector3Value("user:lastPointedAt:position"), "user:lastPointedAt:position", DataStore.GetStringValue("user:intent:object")));
                // Nada: insert events to Dialogue History after setting the object intent
                Log("--------------------------------- SetLocationIntent ----------------------------------------");

                Log("user:lastPointedAt:position" + "  |  " + DataStore.GetVector3Value("user:lastPointedAt:position"));
                targetpointing = "Pointiing only to move the focus to target location";
                focusObject = DataStore.GetStringValue("user:intent:object");
                targetObject = DataStore.GetVector3Value("user:lastPointedAt:position").ToString().Replace(",", ":");
                focuspointing = "Before Ask for Disambiguation";
                DianaTargetPointing = "Executing the event";
                Log("--------------------------------- History Stack after SetLocationIntent ----------------------------------------");
                AddToDialogueHist();

                SetLocation = true;
            }
            //else if (!DataStore.GetBoolValue("user:pointValid") && DataStore.GetBoolValue("me:isAttendingPointing", false)) {
            //    SetValue("me:speech:intent", string.Format("This is not valid pointing. Please undo this prompt. Say nevermind!"), string.Empty);
            //    return;
            //}
        }
        //}
    }
    //nada: to ensure the selected target is the same as what utterred 
    void disRespond(GameObject target, List<object> speechAttr)
    {
        // if (string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:replaceContent"))) { 
        if (emm.getambigEvent().Contains("white"))
        {
            SetValue("me:speech:intent", string.Format("This is not a {0} block. Which {1} block you are referring to?", speechAttr[1].ToString(), speechAttr[1].ToString()), string.Empty);
            emm.logTocsv(nlu.GetUserSpeech(), string.Format("This is not a {0} block. Which {1} block you are referring to?", speechAttr[1].ToString(), speechAttr[1].ToString()), dp.getform(), emm.RelationsLog(), Configurations());

            SetValue("me:intent:lookAt", target.name, string.Empty);
            SetValue("me:intent:lookAt", string.Empty, string.Empty);
            //SetValue("user:intent:object", string.Empty, string.Empty);
            Debug.Log(string.Format("disRespond: Nada: DIM: ENTERED: {0}", emm.getDisam().ToString()));

            return;
        }
        else
        {
            SetValue("me:speech:intent", string.Format("This is not a {0} block. Which {1} block you are referring to?", speechAttr[0].ToString(), speechAttr[0].ToString()), string.Empty);
            emm.logTocsv(nlu.GetUserSpeech(), string.Format("This is not a {0} block. Which {1} block you are referring to?", speechAttr[0].ToString(), speechAttr[0].ToString()), dp.getform(), emm.RelationsLog(), Configurations());

            SetValue("me:intent:lookAt", target.name, string.Empty);
            SetValue("me:intent:lookAt", string.Empty, string.Empty);
            //SetValue("user:intent:object", string.Empty, string.Empty);
            Debug.Log(string.Format("3: Nada: DIM: ENTERED: {0}", emm.getDisam().ToString()));

            return;
        }
    }
    //nada: to ensure the selected target is the same as what utterred 
    void disRespond(GameObject target, string color)
    {
        Debug.Log(string.Format("disRespond: Nada: color: ENTERED: {0}", emm.getDisam().ToString()));

        SetValue("me:speech:intent", string.Format("This is not a {0} block, Which {1} block you are referring to?", dp.getColor(), dp.getColor()), string.Empty);
        emm.logTocsv(nlu.GetUserSpeech(), string.Format("This is not a {0} block. Which {1} block you are referring to?", dp.getColor(), dp.getColor()), dp.getform(), emm.RelationsLog(), Configurations());

        SetValue("me:intent:lookAt", target.name, string.Empty);
        SetValue("me:intent:lookAt", string.Empty, string.Empty);
        //SetValue("user:intent:object", string.Empty, string.Empty);
        Debug.Log(string.Format("color: Nada: DIM: ENTERED: {0}", emm.getDisam().ToString()));

        return;
    }
    // if user provides speech with relational words (right, left, front, behind),
    // diana disambiguate to specify the block if it duplicated. the user point to the intended block,
    // diana should put the block as the user said even user points diffrently
    void SpeechRelationalTransfer(Vector3 curPointPos, GameObject target, GameObject blocker, GameObject ambigBlock)
    {

        // place the theme next to that object at the appropriate location
        if (emm.getambigEvent().Contains("left")) //if speech include left put it left
        {
            Debug.Log("NNada:left:SpeechRelationalTransfer");
            SetValue("user:intent:partialEvent",
                string.Format("put({0},left({1}))",
                DataStore.GetStringValue("user:intent:object"),
                target.name), string.Empty);
            speechonly = true;
        }
        else if (emm.getambigEvent().Contains("right"))
        {
            Debug.Log("NNada:left:SpeechRelationalTransfer");
            SetValue("user:intent:partialEvent",
            string.Format("put({0},right({1}))",
            DataStore.GetStringValue("user:intent:object"),
            target.name), string.Empty);
            speechonly = true;
        }
        else if (emm.getambigEvent().Contains("beside"))
        {
            SetValue("user:intent:partialEvent",
                string.Format("put({0},left({1}))",
                DataStore.GetStringValue("user:intent:object"),
                target.name), string.Empty);
            speechonly = true;
        }
        else if (emm.getambigEvent().Contains("front"))
        {
            SetValue("user:intent:partialEvent",
                string.Format("put({0},in_front({1}))",
                DataStore.GetStringValue("user:intent:object"),
                target.name), string.Empty);
            speechonly = true;
        }
        else if (emm.getambigEvent().Contains("behind"))
        {
            SetValue("user:intent:partialEvent",
                string.Format("put({0},behind({1}))",
                DataStore.GetStringValue("user:intent:object"),
                target.name), string.Empty);
            speechonly = true;
        }
        else if (emm.getambigEvent().Contains("on"))
        {
            do
            {
                bool surfaceClear = DialogueUtility.SurfaceClear(target, out blocker);
                if ((blocker != null) && (blocker != GameObject.Find(DataStore.GetStringValue("user:intent:object"))))
                {
                    target = blocker;
                }
            } while ((blocker != null) && (blocker != GameObject.Find(DataStore.GetStringValue("user:intent:object"))));
            SetValue("user:intent:partialEvent",
                string.Format("put({0},on({1}))",
                DataStore.GetStringValue("user:intent:object"),
                target.name), string.Empty);

            speechonly = true;

            Debug.Log("NNada:on: SpeechRelationalTransfer: " + speechonly);
        }
        //nonpointing = false;

    }
    // Nada: Diana put the block in the location that is pointed by the user 
    void PointingRelationalTransfer(Vector3 curPointPos, GameObject target, GameObject blocker)
    {
        Debug.Log("NNada:PointingRelationalTransfer");

        //if (string.IsNullOrEmpty(DataStore.GetStringValue("user:lastPointedAt:name")))
        //{
        //    SetValue("user:intent:partialEvent",
        //        string.Format("put({0},({1}))",
        //        DataStore.GetStringValue("user:intent:object"),
        //        "{1}"), string.Empty);

        //    //SetLocationIntent();

        //}
        //if (speechonly == false)
        //{
        if (!GlobalHelper.ContainingObjects(curPointPos).Contains(target))
            {
                Debug.Log(string.Format("{0} does not contain {1}", DataStore.GetStringValue("user:lastPointedAt:name"),
            GlobalHelper.VectorToParsable(curPointPos)));
                Bounds objBounds = GlobalHelper.GetObjectWorldSize(target);

                // place the theme next to that object at the appropriate location
                if (curPointPos.x <= objBounds.min.x)
                {
                    SetValue("user:intent:partialEvent", string.Format("put({0},left({1}))",
                    DataStore.GetStringValue("user:intent:object"), target.name), string.Empty);
                    pointonly = true;
                }
                else if (curPointPos.x >= objBounds.max.x)
                {
                    SetValue("user:intent:partialEvent", string.Format("put({0},right({1}))",
                    DataStore.GetStringValue("user:intent:object"),
                    target.name), string.Empty);
                    pointonly = true;
                }
                else if (curPointPos.z <= objBounds.min.z)
                {
                    SetValue("user:intent:partialEvent", string.Format("put({0},in_front({1}))",
                    DataStore.GetStringValue("user:intent:object"),
                    target.name), string.Empty);
                    pointonly = true;
                }
                else if (curPointPos.z >= objBounds.max.z)
                {
                    SetValue("user:intent:partialEvent", string.Format("put({0},behind({1}))",
                    DataStore.GetStringValue("user:intent:object"),
                    target.name), string.Empty);
                    pointonly = true;
                }
            }

            // otherwise put the theme on top of that object
            else if (!string.IsNullOrEmpty(DataStore.GetStringValue("user:intent:object")))
            {
                Debug.Log("8: Nada: DIM: ENTERED");
                Debug.Log("NNada:on:PointingRelationalTransfer");

                Debug.Log(string.Format("{0} contains {1}", target.name,
                    GlobalHelper.VectorToParsable(curPointPos)));
                // in case user is pointing at the lower portions of a stack
                // find a block in that stack with a clear surface, i.e., the topmost block
                do
                {
                    bool surfaceClear = DialogueUtility.SurfaceClear(target, out blocker);
                    if ((blocker != null) && (blocker != GameObject.Find(DataStore.GetStringValue("user:intent:object"))))
                    {
                        target = blocker;
                    }
                } while ((blocker != null) && (blocker != GameObject.Find(DataStore.GetStringValue("user:intent:object"))));
                SetValue("user:intent:partialEvent",
                    string.Format("put({0},on({1}))",
                    DataStore.GetStringValue("user:intent:object"),
                    target.name), string.Empty);
                pointonly = true;
            }
       // }
    }

    // this method calculates the inverse of the last thing Diana did,
    //  and does that
    //  also appends an event if replacement content was supplied
    //  (e.g., "grab the green block" "wait, the yellow one"
    void UndoLastEvent(string replacementContent = "")
    {

        string lastEventStr = DataStore.GetStringValue("user:intent:lastEvent");
        string undoEventStr = string.Empty;
        string appendEventStr = string.Empty;

        switch (GlobalHelper.GetTopPredicate(lastEventStr))
        {
            case "grasp":
                // reverse of grasp is ungrasp
                undoEventStr = lastEventStr.Replace("grasp", "ungrasp");
                // if replacement content is supplied append a new grasp event with that
                if ((DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "attributes") ||
                    (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "functions"))
                {
                    appendEventStr = string.Format("grasp({0})", replacementContent);
                }
                break;

            case "lift":
                // reverse of lift is put down
                undoEventStr = string.Format("put({0},{1})",
                    DataStore.GetStringValue("me:lastTheme"),
                    GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")));
                // if replacement content is supplied append a new lift event with that
                if ((DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "attributes") ||
                    (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "functions"))
                {
                    appendEventStr = string.Format("lift({0})", replacementContent);
                }
                break;

            case "put":
                // reverse of put is put back, so lastThemePos must be valid
                //  and we have to adjust the Y coordinate using FindSurfaceBelow
                if (DataStore.GetVector3Value("me:lastThemePos") != default)
                {
                    GameObject lastTheme = GameObject.Find(DataStore.GetStringValue("me:lastTheme"));
                    Vector3 undoPos = new Vector3(DataStore.GetVector3Value("me:lastThemePos").x,
                        GlobalHelper.GetObjectWorldSize(GlobalHelper.FindSurfaceBelow(DataStore.GetVector3Value("me:lastThemePos"),
                            lastTheme)).max.y + GlobalHelper.GetObjectWorldSize(lastTheme).extents.y,
                        DataStore.GetVector3Value("me:lastThemePos").z);
                    Debug.Log(string.Format("UndoLastEvent: GlobalHelper.FindSurfaceBelow({0},{1}) = {2}",
                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")), DataStore.GetStringValue("me:lastTheme"),
                        GlobalHelper.FindSurfaceBelow(DataStore.GetVector3Value("me:lastThemePos"),
                            GameObject.Find(DataStore.GetStringValue("me:lastTheme")))));
                    undoEventStr = string.Format("put({0},{1})",
                        DataStore.GetStringValue("me:lastTheme"),
                        GlobalHelper.VectorToParsable(undoPos));
                    // if replacement content is supplied append a new put event with that
                    //  replacement content can be an NP or a PP
                    //  if NP - append event with replacement theme
                    //  if PP - append event with replacement destination
                    if ((DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "attributes") ||
                        (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "functions"))
                    {
                        string predArgs = lastEventStr.Split(new char[] { '(' }, 2)[1];
                        predArgs = predArgs.Remove(predArgs.Length - 1);
                        appendEventStr = string.Format(lastEventStr.Replace(predArgs.Split(',')[0], "{0}"), replacementContent);
                    }
                    else if (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "relations")
                    {
                        undoEventStr = string.Empty;

                        string predArgs = lastEventStr.Split(new char[] { '(' }, 2)[1];
                        predArgs = predArgs.Remove(predArgs.Length - 1);
                        appendEventStr = string.Format(lastEventStr.Replace(predArgs.Split(',')[1], "{0}"), replacementContent);
                    }
                }
                break;

            case "slide":
                // reverse of slide is slide back, so lastThemePos must be valid
                if (DataStore.GetVector3Value("me:lastThemePos") != default)
                {
                    undoEventStr = string.Format("slide({0},{1})",
                        DataStore.GetStringValue("me:lastTheme"),
                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")));
                    // if replacement content is supplied append a new slide event with that
                    //  replacement content can be an NP or a PP
                    //  if NP - append event with replacement theme
                    //  if PP - append event with replacement destination
                    if ((DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "attributes") ||
                        (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "functions"))
                    {
                        string predArgs = lastEventStr.Split(new char[] { '(' }, 2)[1];
                        predArgs = predArgs.Remove(predArgs.Length - 1);
                        appendEventStr = string.Format(lastEventStr.Replace(predArgs.Split(',')[0], "{0}"), replacementContent);
                    }
                    else if (DialogueUtility.GetPredicateType(GlobalHelper.GetTopPredicate(replacementContent), voxmlLibrary) == "relations")
                    {
                        undoEventStr = string.Empty;

                        string predArgs = lastEventStr.Split(new char[] { '(' }, 2)[1];
                        predArgs = predArgs.Remove(predArgs.Length - 1);
                        appendEventStr = string.Format(lastEventStr.Replace(predArgs.Split(',')[1], "{0}"), replacementContent);
                    }
                }
                break;

            case "servo":
                // reverse of servo is slide back, so lastThemePos must be valid
                if (DataStore.GetVector3Value("me:lastThemePos") != default)
                {
                    undoEventStr = string.Format("slide({0},{1})",
                        DataStore.GetStringValue("me:lastTheme"),
                        GlobalHelper.VectorToParsable(DataStore.GetVector3Value("me:lastThemePos")));
                }
                break;

            default:
                break;
        }

        // if no undo event on the blackboard, post a new one      
        if (!string.IsNullOrEmpty(undoEventStr))
        {
            Debug.Log(string.Format("Undo: Undoing last event {0} (reverse event calculated as {1})", lastEventStr, undoEventStr));

            // set isUndoing flag and set user:intent:event
            SetValue("me:isUndoing", true, string.Empty);
            SetValue("user:intent:event", undoEventStr, string.Empty);

            // append the replacement event if one exists
            if (!string.IsNullOrEmpty(appendEventStr))
            {
                Debug.Log(string.Format("Undo: Appending replacement event {0}", appendEventStr));
                if (appendEventStr.Contains("{0}"))
                    SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
                else
                    SetValue("user:intent:append:event", appendEventStr, string.Empty);
            }
            // Nada: append the replacement event if one exists
            if (string.IsNullOrEmpty(appendEventStr) && !string.IsNullOrEmpty(replacementContent))
            {
                Debug.Log(string.Format("Undo: Appending replacement event {0}", replacementContent));
                if (replacementContent.Contains("{0}"))
                    SetValue("user:intent:append:partialEvent", replacementContent, string.Empty);
                else
                    SetValue("user:intent:append:event", replacementContent, string.Empty);
            }
            // if no replacement content, empty the key
            if (string.IsNullOrEmpty(replacementContent))
            {
                SetValue("user:intent:replaceContent", string.Empty, string.Empty);
            }
        }
        else if (!string.IsNullOrEmpty(appendEventStr))
        {
            Debug.Log(string.Format("Undo: Undoing last event {0} (alternate event calculated as {1})", lastEventStr, appendEventStr));
            if (appendEventStr.Contains("{0}"))
                SetValue("user:intent:append:partialEvent", appendEventStr, string.Empty);
            else
                SetValue("user:intent:append:event", appendEventStr, string.Empty);
        }
        else if (string.IsNullOrEmpty(undoEventStr) && !string.IsNullOrEmpty(replacementContent))
        {
            Debug.Log(string.Format("Undo: Appending replacement event {0}", replacementContent));
            if (replacementContent.Contains("{0}"))
                SetValue("user:intent:append:partialEvent", replacementContent, string.Empty);
            else
                SetValue("user:intent:append:event", replacementContent, string.Empty);
        }
    }

    // forget the object of focus, stop pointing, and look at the user
    void ForgetFocusObject()
    {

        Debug.Log(string.Format("Undo: forgetting focus object {0}", DataStore.GetStringValue("user:intent:object")));
        SetValue("user:intent:object", DataStore.StringValue.Empty, string.Empty);
        SetValue("me:intent:lookAt", "user", string.Empty);
        SetValue("me:intent:targetObj", string.Empty, string.Empty);
        SetValue("me:intent:action", string.Empty, string.Empty);
        SetValue("me:lastTheme", DataStore.StringValue.Empty, string.Empty);
        SetValue("me:speech:intent", "OK. nevermind.", string.Empty);


    }

    // servoLoopTimer callback
    void CheckServoStatus(object sender, ElapsedEventArgs e)
    {
        checkServoStatus = true;

        servoLoopTimer.Interval = servoLoopTimerTime;
        servoLoopTimer.Enabled = true;
    }

    // learningUpdateTimer callback
    void ProvideLearningStatusUpdate(object sender, ElapsedEventArgs e)
    {
        provideLearningStatusUpdate = true;

        learningUpdateTimer.Interval = learningUpdateTimerTime;
        learningUpdateTimer.Enabled = false;
    }



    public void Ready(object content)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        // do anything that needs to happen when we first enter the ready state
        SetValue("me:intent:lookAt", "user", string.Empty);
        if (sceneName.Equals("Scene0"))
        {
            DataStore.SetValue("user:isEngaged", new DataStore.BoolValue(true), this, "The user is in the engagment zone");
            SetValue("me:speech:intent",
             "Hello. Thank you for your participation!" +
             "You can press Start button when you're ready", string.Empty);
        }
        else
        {
            SetValue("me:speech:intent",
                "We are on " + sceneName + ". Which block should we focus on?", string.Empty);
        }
        SetValue("me:affordances:forget", false, string.Empty);
        // TODO: this animation looks like a crazy alien wave
        // no really, it terrified me to the pit of my soul when I first saw it
        /* To: Nikhil
                 __.,,------.._
              ,'"   _      _   "`.
             /.__, ._  -=- _"`    Y
            (.____.-.`      ""`   j
             VvvvvvV`.Y,.    _.,-'       ,     ,     ,
                Y    ||,   '"\         ,/    ,/    ./
                |   ,'  ,     `-..,'_,'/___,'/   ,'/   ,
           ..  ,;,,',-'"\,'  ,  .     '     ' ""' '--,/    .. ..
         ,'. `.`---'     `, /  , Y -=-    ,'   ,   ,. .`-..||_|| ..
        ff\\`. `._        /f ,'j j , ,' ,   , f ,  \=\ Y   || ||`||_..
        l` \` `.`."`-..,-' j  /./ /, , / , / /l \   \=\l   || `' || ||...
         `  `   `-._ `-.,-/ ,' /`"/-/-/-/-"'''"`.`.  `'.\--`'--..`'_`' || ,
                    "`-_,',  ,'  f    ,   /      `._    ``._     ,  `-.`'//         ,
                  ,-"'' _.,-'    l_,-'_,,'          "`-._ . "`. /|     `.'\ ,       |
                ,',.,-'"          \=) ,`-.         ,    `-'._`.V |       \ // .. . /j
                |f\\               `._ )-."`.     /|         `.| |        `.`-||-\\/
                l` \`                 "`._   "`--' j          j' j          `-`---'
                 `  `                     "`,-  ,'/       ,-'"  /
                                         ,'",__,-'       /,, ,-'
                                         Vvv'            VVv'
        From: Dave *  /* To: Dave
    How long did it take you to do this?

    To: anybody
    Can we get a wave animation that is... less?

    From: Nikhil
    */
        SetValue("user:isInteracting", true, string.Empty);
    }

    public void ModularInteractionLoop(object content)
    {
        // do anything that needs to happen when we first enter the main
        //  interaction loop here
        SetValue("user:intent:object", string.Empty, string.Empty);
        SetValue("user:intent:action", string.Empty, string.Empty);
        SetValue("user:intent:location", Vector3.zero, string.Empty);
        SetValue("user:intent:partialEvent", string.Empty, string.Empty);
        SetValue("user:intent:event", string.Empty, string.Empty);
        SetValue("me:intent:action", string.Empty, string.Empty);
        SetValue("me:intent:lookAt", "user", string.Empty);
        SetValue("me:isAttendingPointing", true, string.Empty);
        SetValue("me:intent:targetObj", string.Empty, string.Empty);
    }

    public void LearningInteractionLoop(object content)
    {
        // do anything that needs to happen when we first enter the learning
        //  interaction loop here
    }

    public void QuestionAnsweringLoop(object content)
    {
        // set question answering flag
        SetValue("me:question:isAnswering", true, string.Empty);
    }

    public void CleanUp(object content)
    {
        // if there's an object in focus
        //  e.g., is picked up, pointed at, etc.
        //  remove it from focus
        if (DataStore.GetStringValue("user:intent:object") != string.Empty)
        {
            SetValue("user:intent:isNevermind", true, string.Empty);
            SetValue("user:intent:isNevermind", false, string.Empty);
        }

        // say goodbye!
        //  (and end the interaction, forget learned affordances)
        SetValue("me:speech:intent", "Bye!", string.Empty);
        SetValue("user:isInteracting", false, string.Empty);
        SetValue("me:affordances:forget", true, string.Empty);
    }
}