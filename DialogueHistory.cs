using System;
using System.Collections.Generic;
using UnityEngine;

//namespace AssemblyCSharp.Assets._Core.Scripts.CogArch.Modules
//{
public class DialogueHistory
{
    public Stack<string> eventHist_stack;
    public Stack<string> objHist_stack;
    //public List<string> objHist_list1;
    public static Stack<string> objtemp;
    public Stack<string> getobjtemp() { return objtemp; }
    public void setobjtemp(Stack<string> value) { objtemp = value; }

    public static Stack<string> evtemp;
    public Stack<string> getevtemp() { return evtemp; }
    public void setevtemp(Stack<string> value) { evtemp = value; }


    // Nada: actions that can be understood by Diana
    public static List<string> _actions = new List<string>(new[] {  "bring", "servo",
                "put", "slide", "push", "take", "lift", "pick up", "pick", "grab", "grasp",
                "pull", "move", "drop", "release", "ungrasp", "let go", "shift", "scoot","that", "this","the",
        "brought", "servoed","put", "slid", "pushed", "took", "lifted", "picked up", "picked", "grabed", "grasped",
                "pulled", "moved", "dropped", "released", "ungrasped", "let went", "shifted", "scooted","hi" });

    public DialogueHistory()
    {
        //popedEventstack = new Stack<string>();
        //popedObjHiststack = new Stack<string>();
        //Nada:stack of events
        eventHist_stack = new Stack<string>();
        //Nada:stack of events' objects
        objHist_stack = new Stack<string>();
        //Nada: temp stacks
        objtemp = new Stack<string>();
        evtemp = new Stack<string>();
        //        objHist_list1 = new List<string>();
    }

    //Nada: Dialogue History
    //parameter: event received
    //returns events stacks and objects stacks ...
    //... for historical referring expressions
    public (Stack<string>, Stack<string>) DialogueHistory_hre(string last_object, string last_event)
    {

        // push poped elements
        while (objtemp.Count > 0)
        {
            Debug.Log(string.Format("Nada : temp count: " + objtemp.Count));
            string o = objtemp.Pop();
            //objHist_list1.Add(o);
            objHist_stack.Push(o);

            Debug.Log(string.Format("Nada : temp " + o + " obj pushed "));

            string e = evtemp.Pop();
            eventHist_stack.Push(e);
            Debug.Log(string.Format("Nada : temp " + e + " event pushed "));

            // }
        }
        // push new events
        if (!string.IsNullOrEmpty(last_event) && !string.IsNullOrEmpty(last_object))
        {
            if (objHist_stack.Count > 0)
            {
                string obj = objHist_stack.Peek();
                string ev = eventHist_stack.Peek();

                if ((!obj.Equals(last_object) || !ev.Equals(last_event)) && _actions.Contains(ev))
                {
                    objHist_stack.Push(last_object);
                    eventHist_stack.Push(last_event);
                    Debug.Log("Nada: stack not null/ pushing" + last_event + " and " + last_object + " done");
                    Debug.Log("Nada in parial Event: Current_stack: last event: " + eventHist_stack.Peek() + " last object: " + objHist_stack.Peek());
                }
            }
            else
            {
                objHist_stack.Push(last_object);
                eventHist_stack.Push(last_event);
                Debug.Log("Nada: stack null/ pushing" + last_event + " and " + last_object + " done");
                Debug.Log("Nada in parial Event: Current_stack: last event: " + eventHist_stack.Peek() + " last object: " + objHist_stack.Peek());
            }
        }
        else
        {
            Debug.Log("The object for the event: " + last_event + " is null: the event was not added to Dialogue History ");
        }
        return (eventHist_stack, objHist_stack);
    }
}
//}
