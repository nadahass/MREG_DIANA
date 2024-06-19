using Grpc.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Text.RegularExpressions;
using UnityEngine;
using VoxSimPlatform.NLU;

#if !UNITY_WEBGL
using VoxSimPlatform.Network;
using VoxSimPlatform.NLU;


#endif
//using System.Diagnostics;
//using System.Net.NetworkInformation;

namespace VoxSimPlatform
{
    namespace NLU
    {


        public class DianaParser : INLParser
        {

            /* this is to execlude all relations that are parsed as nouns by the parser
			* e.g., "move this block to the right". "Right" here is noun! this affect the parsing process
			*/
            private static List<string> _nouns = new List<string>(new[] {
                "left","right","back","front","top","port","starboard","left of","right of",
                "center of","edge of","slide","servo","grasp"
            });
            //------------------------------------------------------------------------------------------------

            //there verbs are not parsed as verbs by corenlp parser; they are parsed as NN
            private static List<string> _verbs = new List<string>(new[] {
                "servo","slide"
            });
            //------------------------------------------------------------------------------------------------
            private static List<String> trans_no_goal = new List<string>(new[] { "bring", "select", "pick up", "lift", "grab", "grasp", "take", "let go of", "ungrasp", "drop", "release", "find", "go to" });

            private static List<String> trans_goal = new List<string>(new[] { "move", "put", "push", "pull", "slide", "place", "shift", "scoot", "servo", "bring" });

            static string color ="";
            public string getColor() { return color; }
            public void setColor(string value) { color = value; }

            static string form="";
            public string getform() { return form; }
            public void setform(string value) { form = value; }

            //NLUModule nlu;
                /*
                * This function is to parse the string input to JSON format
                */
            //    private void Awake()
            //{
            //    nlu = new NLUModule();
            //}
            public static string JSONOutput(string currentInput)
            {
                //denver
                //WebRequest request = WebRequest.Create("http://129.82.44.141:9000/?properties={%22annotators%22%3A%22tokenize%2Cssplit%2Cpos%2Clemma%2Cner%2Cparse%22%2C%22outputFormat%22%3A%22json%22}");

                //yokun
                // WebRequest request = WebRequest.Create("http://10.1.44.112:9000/?properties={%22annotators%22%3A%22tokenize%2Cssplit%2Cpos%2Clemma%2Cner%2Cparse%22%2C%22outputFormat%22%3A%22json%22}");

                //localhost
                WebRequest request = WebRequest.Create("http://localhost:9000/?properties={%22annotators%22%3A%22tokenize%2Cssplit%2Cpos%2Clemma%2Cner%2Cparse%22%2C%22outputFormat%22%3A%22json%22}");

                // preprocessing
                if (currentInput.ToLower().StartsWith("diana,")) currentInput = currentInput.Substring(6).TrimStart();
                if (currentInput.ToLower().StartsWith("please")) currentInput = currentInput.Substring(6).TrimStart();
                if (currentInput.ToLower().StartsWith("ok")) currentInput = currentInput.Substring(2).TrimStart();

                string dataStr = currentInput.ToLower();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                // Get response
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();
                //System.Console.WriteLine(responseString.ToString());
                Debug.Log(responseString);

                return responseString.ToString();
            }



            /*
			* This function is to extract the input dependency parsing value from the JSON file
			*/

            public string NLParse(string rawInput)
            {

                //------------------------------------------------------------------------------------------------
                // JToken variables
                JToken pos = null, word1 = null, cc = null, dep = null, dep2 = null, dep3 = null,
                    dep4 = null, dep5 = null, gov = null, gov2 = null, gov3 = null, gov4 = null, gov5 = null,
                    govnum = null, govnum5 = null, govnum4 = null, depnum5 = null, depnum1 = null, depnum4 = null, depGloss = null, depGloss2 = null, depGloss3 = null, depGloss4 = null, depGloss5 = null;
                // boolean variables 
                bool det=false, Visit = false, anaphorVar = false, amodSet = false, amodSet1 = false, amodSet2 = false,
                    histJJ = false, PRP = false, relation = false, vbz = false, adjSet = false;
                // ints
                int open_pranth = 0;
                //vars
               

                string form2 = "";
                //strings
                string rela_Obj = "";
                //lists
                List<JToken> Allwords = new List<JToken>();

                //------------------------------------------------------------------------------------------------

                // Reading JSON output
                string json = JSONOutput(rawInput);

                // Convert strings to JSON objects
                var jobj = JObject.Parse(json);

                //Length of "basicDependencies" -json array
                JArray DepenArray = (JArray)jobj.SelectToken("sentences[0].tokens");
                int Dep_length = DepenArray.Count;

                //length of "tokens" -json array
                JArray tokensArray = (JArray)jobj.SelectToken("sentences[0].basicDependencies");
                int Token_length = tokensArray.Count;

                // Extracting the root
                JToken root = jobj.SelectToken("sentences[0].tokens[0].word");
                JToken Rootpos = jobj.SelectToken("sentences[0].tokens[0].pos");

                // Start building predicate arguments 

                form = root + "(";
                open_pranth++;

                //------------------------------------------------------------------------------------------------

                // Extract constituents based on their POS 
                for (int i = 0; i < Token_length; i++)
                {
                    pos = jobj.SelectToken("sentences[0].tokens[" + i.ToString() + "].pos");
                    if (pos != null)
                    {
                        // Extract "and (CC)"
                        if (pos.ToString() == "CC")
                        {
                            cc = jobj.SelectToken("sentences[0].tokens[" + i.ToString() + "].word");
                        }

                        // is there adj in the input?
                        if (pos.ToString() == "JJ")
                        {
                            amodSet1 = true;
                        }

                        // is there pronoun in the input?
                        if (pos.ToString() == "PRP")
                        {
                            PRP = true;
                        }

                        // is there vbz in the input?
                        if (pos.ToString() == "VBZ")
                        {
                            vbz = true;
                            relation = true;
                        }

                        // Extract "noun (NN)"
                        if (pos.ToString() == "NN" || pos.ToString() == "NNS" || pos.ToString() == "CD")
                        {
                            word1 = jobj.SelectToken("sentences[0].tokens[" + i.ToString() + "].word");
                            if (!_nouns.Contains(word1.ToString()))
                            {
                                Allwords.Add(word1);
                            }
                        }
                    }
                }

                //------------------------------------------------------------------------------------------------

                // If nouns are the same, keep only one word
                if (Allwords.Count > 1 && Allwords.TrueForAll(i => i.Equals(Allwords.FirstOrDefault())))
                {
                    Allwords.RemoveAt(1);
                }
                /* adding "it" to get inside the loop
				* in case the input does not have nouns e.g., "put it there"*/
                if (Allwords.Count == 0 && rawInput.Split().Contains("it"))
                {
                    Allwords.Add("it");
                }

                //------------------------------------------------------------------------------------------------

                // bunch of if-else statements handling various referring expressions states

                foreach (JToken word in Allwords)
                {
                    bool nounSet = false;

                    //System.Console.WriteLine("word: "+ word);

                    /* in relational REs there will be more than one noun, so only the target object should be considered here
					* e.g, "bring the green block that is on the plate", "block" is the target and "plate" used for description
					*/

                    if (word.ToString() == rela_Obj)
                        break;

                    //------------------------------------------------------------------------------------------------
                    // "historical adjectives"
                    for (int counter = 0; counter < Dep_length; counter++)
                    {
                        // dependency between two constituents 
                        dep = jobj.SelectToken("sentences[0].basicDependencies[" + counter.ToString() + "].dep");
                        //the governor constituent
                        gov = jobj.SelectToken("sentences[0].basicDependencies[" + counter.ToString() + "].governorGloss");
                        //the dependent constituent
                        depGloss = jobj.SelectToken("sentences[0].basicDependencies[" + counter.ToString() + "].dependentGloss");
                        //the govnum 
                        govnum = jobj.SelectToken("sentences[0].basicDependencies[" + counter.ToString() + "].governor");
                        depnum1 = jobj.SelectToken("sentences[0].basicDependencies[" + counter.ToString() + "].dependent");

                        /* Case#: Extract "historical adjectives" as in: pick up the block you just put down,
						*        based on "parataxis", "SBAR" and "nsubj" dependency*/

                        if ((jobj.SelectToken("sentences[0].parse").ToString().Contains("SBAR") && PRP == true) || dep.ToString() == "parataxis")
                        {
                            histJJ = true;
                        }

                        //Cont.# "historical adjectives"
                        if (dep.ToString() == "nsubj" && histJJ == true)
                        {
                            //if there is an adj e.g: green or big
                            if (amodSet == true)
                            {
                                form += "(" + gov.ToString() + "_adj(" + word.ToString() + "))";
                                nounSet = true;
                            }
                            else
                            {
                                form += gov.ToString() + "_adj(" + word.ToString() + ")";
                                nounSet = true;
                            }
                        }
                        //------------------------------------------------------------------------------------------------

                        /* Case#: Extract "relational adjectives" as in: slide the block that is beside the plate
						*        based on "parataxis", "SBAR" dependency
						*/
                        if (jobj.SelectToken("sentences[0].parse").ToString().Contains("SBAR") && vbz == true)
                        {
                            if (dep.ToString() == "case")
                            {
                                // e.g, bring the green block that is on the plate
                                if (trans_no_goal.Contains(root.ToString()) && depGloss.ToString() != "of" && !jobj.SelectToken("sentences[0].parse").ToString().Contains("of"))
                                {
                                    for (int m = 0; m < Dep_length; m++)
                                    {
                                        //dependency between two constituents 
                                        dep5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dep");
                                        //the governor constituent
                                        gov5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governorGloss");
                                        //the dependent constituent
                                        depGloss5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependentGloss");
                                        //the govnum 
                                        govnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governor");

                                        if ((dep5.ToString() == "amod") && (gov.ToString() == gov5.ToString()) && (govnum.Equals(govnum5)))
                                        {
                                            amodSet2 = true;
                                            break;
                                        }

                                    }
                                    // e.g., take the red block that is beside the plate
                                    if (amodSet == true && amodSet2 == false)
                                    {
                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);
                                        form += "(" + depGloss.ToString() + "_adj" + "(" + gov.ToString() + "(" + word.ToString() + ")))";
                                        nounSet = true;
                                        rela_Obj = gov.ToString();
                                        relation = true;
                                    }

                                    // e.g., take the block that is beside the red block
                                    else if (amodSet == false && amodSet2 == true)
                                    {
                                        Debug.Log(string.Format("Nada Parser: amodSet:" + amodSet + " amodSet2:" + amodSet2));

                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);
                                        form += depGloss.ToString() + "_adj" + "(" + depGloss5.ToString() + "_" + gov.ToString() + "(" + word.ToString() + "))";
                                        nounSet = true;
                                        rela_Obj = gov.ToString();
                                        relation = true;
                                    }
                                    // e.g., take the red block that is beside the blue block
                                    else if (amodSet == true && amodSet2 == true)
                                    {
                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                        form += "(" + depGloss.ToString() + "_adj" + "(" + depGloss5.ToString() + "_" + gov.ToString() + "(" + word.ToString() + ")))";
                                        nounSet = true;
                                        amodSet2 = false;
                                        relation = true;

                                    }
                                    // e.g., take the block that is beside the cup
                                    else if (amodSet == false && amodSet2 == false)
                                    {
                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                        form += depGloss.ToString() + "_adj" + "(" + gov.ToString() + "(" + word.ToString() + "))";
                                        nounSet = true;
                                        rela_Obj = gov.ToString();
                                        relation = true;
                                    }

                                }
                                // e.g., bring the green block that is in front of the cup
                                else if (trans_no_goal.Contains(root.ToString()) && depGloss.ToString() != "of" && jobj.SelectToken("sentences[0].parse").ToString().Contains("of") && gov.ToString() != "right" && gov.ToString() != "left")

                                {
                                    for (int y = 0; y < Dep_length; y++)
                                    {
                                        // dependency between two constituents 
                                        dep4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dep");
                                        //the governor constituent
                                        gov4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].governorGloss");
                                        //the dependent constituent
                                        depGloss4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dependentGloss");
                                        govnum4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].governor");
                                        depnum4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dependent");

                                        if (dep4.ToString() == "nmod" && gov4.ToString() == gov.ToString())
                                        {
                                            for (int m = 0; m < Dep_length; m++)
                                            {
                                                //dependency between two constituents 
                                                dep5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dep");
                                                //the governor constituent
                                                gov5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governorGloss");
                                                //the dependent constituent
                                                depGloss5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependentGloss");
                                                //the govnum 
                                                govnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governor");
                                                //the depnum 
                                                depnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependent");

                                                if ((dep5.ToString() == "amod") && (depGloss4.ToString() == gov5.ToString()) && (govnum5.Equals(depnum4)))
                                                {
                                                    amodSet2 = true;
                                                    break;
                                                }

                                            }
                                            // e.g., take the red block that is in front of the cup 
                                            if (amodSet == true && amodSet2 == false)
                                            {
                                                System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                                form += "(" + depGloss.ToString() + "_" + gov.ToString() + "_adj(" + depGloss4.ToString() + "(" + word.ToString() + ")))";
                                                nounSet = true;
                                                relation = true;
                                                rela_Obj = depGloss4.ToString();
                                            }
                                            // e.g., take the yellow block that is in front of the red block 

                                            else if (amodSet == true && amodSet2 == true)
                                            {
                                                System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                                form += "(" + depGloss.ToString() + "_" + gov.ToString() + "_adj(" + depGloss5.ToString() + "_" + depGloss4.ToString() + "(" + word.ToString() + ")))";
                                                nounSet = true;
                                                relation = true;
                                                rela_Obj = depGloss4.ToString();

                                            }
                                            // e.g., take the block that is in front of the red block 
                                            else if (amodSet == false && amodSet2 == true)
                                            {
                                                Debug.Log(string.Format("Nada Parser that: amodSet:" + amodSet + " amodSet2:" + amodSet2));

                                                System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);
                                                form += depGloss.ToString() + "_" + gov.ToString() + "_adj" + "(" + depGloss5.ToString() + "_" + depGloss4.ToString() + "(" + word.ToString() + "))";
                                                nounSet = true;
                                                rela_Obj = gov.ToString();
                                                relation = true;
                                            }
                                            // e.g., take the block that is in front of the cup 

                                            else if (amodSet == false && amodSet2 == false)
                                            {
                                                System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                                form += depGloss.ToString() + "_" + gov.ToString() + "_adj(" + depGloss4.ToString() + "(" + word.ToString() + "))";
                                                nounSet = true;
                                                relation = true;
                                                rela_Obj = depGloss4.ToString();
                                            }
                                        }
                                    }
                                }

                            }
                            // e.g., bring the green block that is to the right of the cup
                            else if (trans_no_goal.Contains(root.ToString()) && dep.ToString() == "nmod" && jobj.SelectToken("sentences[0].parse").ToString().Contains("of") && gov.ToString() != "front" && !rawInput.Contains("and"))
                            {
                                for (int m = 0; m < Dep_length; m++)
                                {
                                    //dependency between two constituents 
                                    dep5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dep");
                                    //the governor constituent
                                    gov5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governorGloss");
                                    //the dependent constituent
                                    depGloss5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependentGloss");
                                    //the govnum 
                                    govnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governor");
                                    //the depnum 
                                    depnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependent");

                                    if ((dep5.ToString() == "amod") && (depGloss.ToString() == gov5.ToString()) && (depnum1.Equals(govnum5)))
                                    {
                                        amodSet2 = true;
                                        break;
                                    }

                                }
                                // e.g., take the red block that is to the right of the cup
                                if (amodSet == true && amodSet2 == false)
                                {
                                    form += "(" + gov.ToString() + "_adj(" + depGloss.ToString() + "(" + word.ToString() + ")))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                                // e.g., take the red block that is to the right of the blue block
                                else if (amodSet == true && amodSet2 == true)
                                {
                                    form += "(" + gov.ToString() + "_adj(" + depGloss5.ToString() + "_" + depGloss.ToString() + "(" + word.ToString() + ")))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                                // e.g., take the block that is to the right of the red block
                                else if (amodSet == false && amodSet2 == true)
                                {
                                    form += gov.ToString() + "_adj(" + depGloss5.ToString() + "_" + depGloss.ToString() + "(" + word.ToString() + ")))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                                // e.g., take the block that is to the right of the cup
                                else if (amodSet == false && amodSet2 == false)
                                {
                                    form += gov.ToString() + "_adj(" + depGloss.ToString() + "(" + word.ToString() + "))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                            }

                        }
                            // ------------------------------------------SHORT RELATIONAL DESCRIPTION------------------------------------------

                        // e.g., take that red block in front of the knife .. non transitional verb with if front of relation
                        else if (trans_no_goal.Contains(root.ToString()) && jobj.SelectToken("sentences[0].parse").ToString().Contains("of")
                            && !rawInput.Contains("and") && !rawInput.Contains("right") && !rawInput.Contains("left"))
                        {
                            relation = true;

                            for (int y = 0; y < Dep_length; y++)
                            {
                                // dependency between two constituents 
                                dep4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dep");
                                //the governor constituent
                                gov4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].governorGloss");
                                //the dependent constituent
                                depGloss4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dependentGloss");
                                govnum4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].governor");
                                depnum4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dependent");

                                if (dep4.ToString() == "nmod" && gov4.ToString() == gov.ToString() && depGloss.ToString().Equals("in"))
                                {
                                    for (int m = 0; m < Dep_length; m++)
                                    {
                                        //dependency between two constituents 
                                        dep5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dep");
                                        //the governor constituent
                                        gov5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governorGloss");
                                        //the dependent constituent
                                        depGloss5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependentGloss");
                                        //the govnum 
                                        govnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governor");
                                        //the depnum 
                                        depnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependent");

                                        if ((dep5.ToString() == "amod") && (depGloss4.ToString() == gov5.ToString()) && (govnum5.Equals(depnum4)))
                                        {
                                            amodSet2 = true;
                                            break;
                                        }

                                    }
                                    // e.g., take the green block in front of the cup
                                    if (amodSet == true && amodSet2 == false)
                                    {
                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                        form += "(" + depGloss.ToString() + "_" + gov.ToString() + "_adj(" + depGloss4.ToString() + "(" + word.ToString() + ")))";
                                        nounSet = true;
                                        relation = true;
                                        rela_Obj = depGloss4.ToString();
                                    }
                                    // e.g., take the green block in front of the yellow block 
                                    else if (amodSet == true && amodSet2 == true)
                                    {
                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                        form += "(" + depGloss.ToString() + "_" + gov.ToString() + "_adj(" + depGloss5.ToString() + "_" + depGloss4.ToString() + "(" + word.ToString() + ")))";
                                        nounSet = true;
                                        relation = true;
                                        rela_Obj = depGloss4.ToString();

                                    }
                                    // e.g., take the block in front of the red block
                                    else if (amodSet == false && amodSet2 == true)
                                    {
                                        Debug.Log(string.Format("Nada Parser: amodSet:" + amodSet + " amodSet2:" + amodSet2));

                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);
                                        form += depGloss.ToString() + "_" + gov.ToString() + "_adj" + "(" + depGloss5.ToString() + "_" + depGloss4.ToString() + "(" + word.ToString() + "))";
                                        nounSet = true;
                                        rela_Obj = gov.ToString();
                                        relation = true;
                                    }
                                    // e.g., take the block in front of the cup 
                                    else if (amodSet == false && amodSet2 == false)
                                    {
                                        System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                        form += depGloss.ToString() + "_" + gov.ToString() + "_adj(" + depGloss4.ToString() + "(" + word.ToString() + "))";
                                        nounSet = true;
                                        relation = true;
                                        rela_Obj = depGloss4.ToString();
                                    }
                                }
                            }
                        }
                        // e.g, bring the green block on the plate
                        else if (trans_no_goal.Contains(root.ToString()) /*&& depGloss.ToString() != "of"*/ && !jobj.SelectToken("sentences[0].parse").ToString().Contains("of") && (
                        (rawInput.Contains("on") || rawInput.Contains("right") || rawInput.Contains("left") || rawInput.Contains("behind")
                            || rawInput.Contains("next") || rawInput.Contains("beside") || rawInput.Contains("touching") || rawInput.Contains("under") || rawInput.Contains("below") || rawInput.Contains("underneath")) && !rawInput.Contains("and")))
                        {
                            System.Console.WriteLine("trans_no_goal: YES");
                            relation = true;
                            if (dep.ToString() == "case" /*&& gov.ToString() == word.ToString() && depGloss.ToString() != "of"*/)
                            {
                                for (int y = 0; y < Dep_length; y++)
                                {
                                    // dependency between two constituents 
                                    dep4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dep");
                                    //the governor constituent
                                    gov4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].governorGloss");
                                    //the dependent constituent
                                    depGloss4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dependentGloss");
                                    govnum4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].governor");
                                    depnum4 = jobj.SelectToken("sentences[0].basicDependencies[" + y.ToString() + "].dependent");

                                    if (dep4.ToString() == "obj")
                                    {

                                        for (int m = 0; m < Dep_length; m++)
                                        {
                                            //dependency between two constituents 
                                            dep5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dep");
                                            //the governor constituent
                                            gov5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governorGloss");
                                            //the dependent constituent
                                            depGloss5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependentGloss");
                                            //the govnum 
                                            govnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governor");

                                            if ((dep5.ToString() == "amod") && (gov.ToString() == gov5.ToString()) && (govnum.Equals(govnum5)))
                                            {
                                                amodSet2 = true;
                                                break;
                                            }

                                        }

                                        // e.g., take the red block beside the cup
                                        if (amodSet == true && amodSet2 == false)
                                        {
                                            System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);
                                            form += "(" + depGloss.ToString() + "_adj" + "(" + gov.ToString() + "(" + depGloss4.ToString() + ")))";
                                            nounSet = true;
                                            rela_Obj = gov.ToString();
                                            relation = true;

                                        }
                                        // e.g., take the red block beside the green block
                                        else if (amodSet == true && amodSet2 == true)
                                        {
                                            System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);

                                            form += "(" + depGloss.ToString() + "_adj" + "(" + depGloss5.ToString() + "_" + gov.ToString() + "(" + depGloss4.ToString() + ")))";
                                            nounSet = true;
                                            amodSet2 = false;
                                            relation = true;
                                            //rela_Obj = gov.ToString();
                                        }
                                         
                                         // e.g., take the block beside the red block
                                        else if (amodSet == false && amodSet2 == true)
                                        {
                                            Debug.Log(string.Format("Nada Parser: amodSet:" + amodSet + " amodSet2:" + amodSet2));

                                            System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);
                                            form += depGloss.ToString() + "_adj" + "(" + depGloss5.ToString() + "_" + gov.ToString() + "(" + word.ToString() + "))";
                                            nounSet = true;
                                            rela_Obj = gov.ToString();
                                            relation = true;
                                        }
                                        // e.g., take the block beside the cup                                     
                                        else if (amodSet == false && amodSet2 == false)
                                        {
                                            System.Console.WriteLine("amodSet:" + amodSet + " amodSet2:" + amodSet2);
                                            Debug.Log(string.Format("Nada Parser: amodSet:" + amodSet + " amodSet2:" + amodSet2));

                                            form +=  depGloss.ToString() + "_adj" + "(" + gov.ToString() + "(" + depGloss4.ToString() + "))";
                                            nounSet = true;
                                            rela_Obj = gov.ToString();
                                            relation = true;
                                        }

                                    }
                                }
                            }
                        }
                        // e.g., bring the green block to the right of the cup
                        else if (trans_no_goal.Contains(root.ToString()) && jobj.SelectToken("sentences[0].parse").ToString().Contains("of") &&
                            (jobj.SelectToken("sentences[0].parse").ToString().Contains("right") || jobj.SelectToken("sentences[0].parse").ToString().Contains("left")
                            ) && !rawInput.Contains("and"))
                        {
                            //looknmod = true;
                            relation = true;
                            if (dep.ToString().Equals("nmod"))
                            {
                                //looknmod = false;
                                System.Console.WriteLine("trans_no_goal: YES");
                                
                                for (int m = 0; m < Dep_length; m++)
                                {
                                    //dependency between two constituents 
                                    dep5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dep");
                                    //the governor constituent
                                    gov5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governorGloss");
                                    //the dependent constituent
                                    depGloss5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependentGloss");
                                    //the govnum 
                                    govnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governor");
                                    //the depnum 
                                    depnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependent");

                                    if ((dep5.ToString() == "amod") && (depGloss.ToString() == gov5.ToString()) && (depnum1.Equals(govnum5)))
                                    {
                                        amodSet2 = true;
                                        break;
                                    }

                                }
                                // e.g., bring the green block to the right of the cup

                                if (amodSet == true && amodSet2 == false)
                                {
                                    //if there is an adj e.g: green or big
                                    form += "(" + gov.ToString() + "_adj(" + depGloss.ToString() + "(" + word.ToString() + ")))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                                 // e.g., bring the green block to the right of the red block

                                else if (amodSet == true && amodSet2 == true)
                                {
                                    form += "(" + gov.ToString() + "_adj(" + depGloss5 + "_" + depGloss.ToString() + "(" + word.ToString() + ")))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                                // e.g., bring the block to the right of the red block

                                else if (amodSet == false && amodSet2 == true)
                                {
                                    form +=  gov.ToString() + "_adj(" + depGloss5 + "_" + depGloss.ToString() + "(" + word.ToString() + ")))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                                // e.g., bring the block to the right of the cup

                                else if (amodSet == false && amodSet2 == false)
                                {
                                    form += gov.ToString() + "_adj(" + depGloss.ToString() + "(" + word.ToString() + "))";
                                    nounSet = true;
                                    rela_Obj = depGloss.ToString();
                                    relation = true;
                                }
                            }

                            }  //------------------------------------------------------------------------------------------------

                        // Case#: Extract obsolute or relative adjective; e.g, red, big, based on "amod" dependency
               
                        if (dep.ToString() == "amod" && gov.ToString() == word.ToString() && adjSet == false)
                        {
                             Debug.Log(string.Format("Nada amod_1"));

                            if ((histJJ == true || relation == true) && (int)govnum < 5)
                            {   // if there is a historical discription
                                form += depGloss.ToString();
                                adjSet = true;
                                nounSet = true;
                                amodSet = true;

                            }
                            else if (histJJ == false && relation == false /*&& (int)govnum > 5*/)

                            {
                                Debug.Log(string.Format("Nada amod_2: relation is: " + relation));

                                form += depGloss.ToString() + "(" + word.ToString() + ")";
                                // noun should be added here in the next step
                                if (det)
                                {
                                    form += ")";
                                    open_pranth--;
                                }
                                nounSet = true;
                            }
                        }



                        //------------------------------------------------------------------------------------------------

                        // Case#: Extract the second event based on "CC" dependency
                        if (cc != null && Visit == false)
                        {

                            if ((dep.ToString() == "cc") && (depGloss.ToString() == cc.ToString()))
                            {
                                // close opened prantheses 
                                for (int p1 = 0; p1 < open_pranth; p1++)
                                {
                                    form += ")";
                                }
                                open_pranth = 0;
                                // build the second event
                                root = gov;
                                form += "+" + gov.ToString() + "(";
                                open_pranth += 1;
                                // visit just one time
                                Visit = true;
                            }
                        }
                        //------------------------------------------------------------------------------------------------

                        // Case#: Extract the the pronoun(e.g: it) based on "obj" dependency
                        if (dep.ToString() == "obj" && depGloss.ToString() == "it" && anaphorVar == false)
                        {
                            form += "{0}";
                            anaphorVar = true;
                        }
                        //------------------------------------------------------------------------------------------------

                        // Case#: Extract the prepositions
                        if (dep.ToString() == "case" && gov.ToString() != null)
                        {
                            // Case#: Extract the the prepositions(e.g: on, in under, etc) based on "case" dependency
                            if (trans_goal.Contains(root.ToString()) && dep.ToString() == "case" && gov.ToString() == word.ToString() && depGloss.ToString() != "of" && relation == false)
                            {
                                form += "," + depGloss.ToString() + "(";
                                open_pranth++;
                                //nounSet = false;
                            }
                            //------------------------------------------------------------------------------------------------

                            for (int i = 0; i < Dep_length; i++)
                            {
                                //dependency between two constituents 
                                dep2 = jobj.SelectToken("sentences[0].basicDependencies[" + i.ToString() + "].dep");
                                //the governor constituent
                                gov2 = jobj.SelectToken("sentences[0].basicDependencies[" + i.ToString() + "].governorGloss");
                                //the dependent constituent
                                depGloss2 = jobj.SelectToken("sentences[0].basicDependencies[" + i.ToString() + "].dependentGloss");


                               /* 
                                * Case#: Extract the prepositions (e.g: right of ...) based on nmod and case dependency
								* e.g., Push the glass to the right of the green block
								*/
                                if (trans_goal.Contains(root.ToString()) && (gov2.ToString() == "right" || gov2.ToString() == "left") && relation == false)
                                {

                                    if (dep2.ToString() == "nmod" && depGloss2.ToString() == word.ToString() && gov2.ToString() == gov.ToString())
                                    {
                                        form += "," + gov + "(";
                                        open_pranth++;
                                    }
                                }
                                //------------------------------------------------------------------------------------------------

                                /* Case#: Extract the prepositions (e.g: in front of ...) based on nmod and case dependency
								* e.g, move the yellow block and place the green plate in front of red knife
								*/
                                else if (trans_goal.Contains(root.ToString()) && dep2.ToString() == "nmod" && depGloss2.ToString() == word.ToString() && gov2.ToString() == gov.ToString() && relation == false)
                                {
                                    Debug.Log(string.Format("Extract the prepositions (e.g: in front of ...)"));
                                    form += "," + depGloss + "_" + gov + "(";
                                    open_pranth++;
                                }
                                //------------------------------------------------------------------------------------------------

                                /* Case#: Extract the prepositions (e.g:...to the right) based on obl and case dependency
								* e.g., move the green block to the left
								*/
                                else if (((dep2.ToString() == "obl" && /*depGloss2.ToString() == gov.ToString() &&*/
                                    (depGloss2.ToString() == "right" || depGloss2.ToString() == "left") && Allwords.Count == 1
                                    && !jobj.SelectToken("sentences[0].parse").ToString().Contains("of")) || (_verbs.Contains(root.ToString())
                                    && dep2.ToString() == "nmod" && gov2.ToString() == word.ToString())) && trans_goal.Contains(root.ToString()))
                                {
                                    if (nounSet == false && !Allwords.Contains("it"))
                                    {
                                        form += word.ToString() + "),{1}(" + gov + ")";
                                        nounSet = true;
                                    }
                                    else
                                    {
                                        form += ",{1}(" + gov + ")";
                                    }
                                }
                            }
                        }
                        //------------------------------------------------------------------------------------------------


                        // Case#: Extract determiner of the noun based on "det" dependency
                        if ((dep.ToString() == "det") && (gov.ToString() == word.ToString()) && (histJJ == false) && (relation == false))
                        {

                            if (amodSet1 == true)
                            {
                                Debug.Log(string.Format("Nada amod_3"));


                                for (int m = 0; m < Dep_length; m++)
                                {
                                    //dependency between two constituents 
                                    dep5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dep");
                                    //the governor constituent
                                    gov5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governorGloss");
                                    //the dependent constituent
                                    depGloss5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].dependentGloss");
                                    //the govnum 
                                    govnum5 = jobj.SelectToken("sentences[0].basicDependencies[" + m.ToString() + "].governor");

                                    if ((dep5.ToString() == "amod") && (gov.ToString() == gov5.ToString()) && (govnum.Equals(govnum5)))
                                    {
                                        amodSet2 = true;
                                        break;
                                    }

                                }
                            }
                            if (amodSet2 == true)
                            {
                                Debug.Log(string.Format("Nada amod_4"));

                                form += depGloss.ToString() + "(";
                                amodSet2 = false;
                                open_pranth++;
                                det = true;
                            }
                            else
                            {
                                Debug.Log(string.Format("Nada amod_5"));

                                form += depGloss.ToString() + "(" + word.ToString() + ")";
                                nounSet = true;
                            }

                        }
                        //------------------------------------------------------------------------------------------------

                        // Case#: Extract "there and here" based on "advmod" dependency
                        if (dep.ToString() == "advmod" && (depGloss.ToString() == "there" || depGloss.ToString() == "here"))
                        {
                            if (trans_goal.Contains(root.ToString()))
                            {
                                for (int j = 0; j < Dep_length; j++)
                                {

                                    dep3 = jobj.SelectToken("sentences[0].basicDependencies[" + j.ToString() + "].dep");
                                    //the governor constituent
                                    gov3 = jobj.SelectToken("sentences[0].basicDependencies[" + j.ToString() + "].governorGloss");
                                    //the dependent constituent
                                    depGloss3 = jobj.SelectToken("sentences[0].basicDependencies[" + j.ToString() + "].dependentGloss");
                                    if ((dep3.ToString() == "obj" && depGloss3.ToString() == word.ToString() && gov.ToString() == gov3.ToString()) || anaphorVar == true)
                                    {
                                        if (nounSet == false && anaphorVar == false && !Allwords.Contains("it"))
                                        {
                                            form += word.ToString() + "," + "{1}";
                                            nounSet = true;
                                            break;
                                        }
                                        else
                                        {
                                            form += "," + "{1}";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //------------------------------------------------------------------------------------------------

                    //Adding objects or nouns after enrolling all determiners, objectives or prepositions if word variable not integer.

                    if (nounSet == false && !Allwords.Contains("it"))
                    {
                        Debug.Log(string.Format("Nada amod_6"));

                        form += word + ")";
                        open_pranth--;
                        nounSet = true;
                    }
                }
                //------------------------------------------------------------------------------------------------

                // close opened prantheses 
                for (int p = 0; p < open_pranth; p++)
                {
                    form += ")";
                }
                //------------------------------------------------------------------------------------------------

                // if the input contains "one" or "ones", replace it with {2}
                form = Replacement(rawInput, form);
                form2= Replacement2(form);
                //------------------------------------------------------------------------------------------------

                // loging the final form
                Debug.Log("DianaParser's form: " + form);
                Debug.Log("DianaParser's form2: " + form2);
                //------------------------------------------------------------------------------------------------

                // return the form 
                return form2;
            }

            //------------------------------------------------------------------------------------------------
            // if the input contains "one" or "ones", replace it with {2}
            public static string Replacement(string input, string form)
            {
                if (input.Split().Contains("one") && form.Contains("{2}"))
                {
                    form = Regex.Replace(form, @"\bone\b", "{2}");
                }
                if (input.Split().Contains("ones"))
                {
                    form = Regex.Replace(form, @"\bones\b", "{2}");
                }
                if (form.Contains("put")&& form.Contains("{1}") && (form.Contains("right")|| form.Contains("left")))
                {
                    form = form.Replace("put", "slide");
                }
                if (input.Split().Contains("picked up"))
                {
                    form = form.Replace("picked up", "lifted");
                }
                if (form.Contains("picked"))
                {
                    form = form.Replace("picked", "lifted");
                }
                if (form.Contains("take"))
                {
                    form = form.Replace("take", "grasp");
                }
                if (form.Contains("grabed"))
                {
                    form = form.Replace("grabed", "grasped");
                }
                if (form.Contains("took"))
                {
                    form = form.Replace("took", "grasped");
                }
                if (form.Contains("went of"))
                {
                    form = form.Replace("went of", "ungrasped");
                }
                if (form.Contains("dropped"))
                {
                    form = form.Replace("dropped", "ungrasped");
                }
                if (form.Contains("released"))
                {
                    form = form.Replace("released", "ungrasped");
                }
                if (form.Contains("moved"))
                {
                    form = form.Replace("moved", "put");
                }
                if (form.Contains("moved"))
                {
                    form = form.Replace("moved", "put");
                }
                if (form.Contains("pushed"))
                {
                    form = form.Replace("pushed", "slid");
                }
                if (form.Contains("pulled"))
                {
                    form = form.Replace("pulled", "slid");
                }
                // these replaced after parsing since the paser does not consider "servo" as verb
                // if user says "servo", "servo" will be replaced with "bring",
                // parsed and then "servo" will returned instead of "bring"
                if (form.Contains("scooted"))
                {
                    form = form.Replace("scoot", "servoed");
                }
                if (form.Contains("shifted"))
                {
                    form = form.Replace("shifted", "servoed");
                }
                if (form.Contains("brought"))
                {
                    form = form.Replace("brought", "servoed");
                }
                if (form.Contains("scoot"))
                {
                    form = form.Replace("scoot", "servo");
                }
                if (form.Contains("shift"))
                {
                    form = form.Replace("shift", "servo");
                }
                if (form.Contains("bring"))
                {
                    form = form.Replace("bring", "servo");
                }
                return form;
            }

            public static string Replacement2(string form)
            {
                if (form.Contains(","))
                {
                    if (form.Split(',')[0].Contains("the(black(block))"))
                    {
                        form = form.Replace("the(black(block))", "{0}");
                        color = "black";
                    }
                    if (form.Split(',')[0].Contains("the(pink(block))"))
                    {
                        form = form.Replace("the(pink(block))", "{0}");
                        color = "pink";
                    }
                    if (form.Split(',')[0].Contains("the(yellow(block))"))
                    {
                        form = form.Replace("the(yellow(block))", "{0}");
                        color = "yellow";
                    }
                    if (form.Split(',')[0].Contains("the(red(block))"))
                    {
                        form = form.Replace("the(red(block))", "{0}");
                        color = "red";
                    }
                    if (form.Split(',')[0].Contains("the(green(block))"))
                    {
                        form = form.Replace("the(green(block))", "{0}");
                        color = "green";
                    }
                    if (form.Split(',')[0].Contains("the(gray(block))"))
                    {
                        form = form.Replace("the(gray(block))", "{0}");
                        color = "gray";
                    }
                    if (form.Split(',')[0].Contains("the(white(block))"))
                    {
                        form = form.Replace("the(white(block))", "{0}");
                        color = "white";
                    }
                    if (form.Split(',')[0].Contains("the(blue(block))"))
                    {
                        form = form.Replace("the(blue(block))", "{0}");
                        color = "blue";
                    }
                }
                if (!form.Contains("_adj"))
                {
                    if (form.Contains("this(block)"))
                    {
                        form = form.Replace("this(block)", "{0}");
                    }
                    if (form.Contains("that(block)"))
                    {
                        form = form.Replace("that(block)", "{0}");
                    }
                    if (form.Contains("the(block)"))
                    {
                        form = form.Replace("the(block)", "{0}");
                    }

                    if (form.Contains("this(red(block))"))
                    {
                        form = form.Replace("this(red(block))", "{0}");
                        color = "red";
                    }
                    if (form.Contains("this(pink(block))"))
                    {
                        form = form.Replace("this(pink(block))", "{0}");
                        color = "pink";
                    }
                    if (form.Contains("this(yellow(block))"))
                    {
                        form = form.Replace("this(yellow(block))", "{0}");
                        color = "yellow";
                    }
                    if (form.Contains("this(green(block))"))
                    {
                        form = form.Replace("this(green(block))", "{0}");
                        color = "green";
                    }
                    if (form.Contains("this(gray(block))"))
                    {
                        form = form.Replace("this(gray(block))", "{0}");
                        color = "gray";
                    }
                    if (form.Contains("this(black(block))"))
                    {
                        form = form.Replace("this(black(block))", "{0}");
                        color = "black";
                    }
                    if (form.Contains("this(white(block))"))
                    {
                        form = form.Replace("this(white(block))", "{0}");
                        color = "white";
                    }
                    if (form.Contains("this(blue(block))"))
                    {
                        form = form.Replace("this(blue(block))", "{0}");
                        color = "blue";
                    }

                    if (form.Contains("that(red(block))"))
                    {
                        form = form.Replace("that(red(block))", "{0}");
                        color = "red";
                    }
                    if (form.Contains("that(pink(block))"))
                    {
                        form = form.Replace("that(pink(block))", "{0}");
                        color = "pink";
                    }
                    if (form.Contains("that(yellow(block))"))
                    {
                        form = form.Replace("that(yellow(block))", "{0}");
                        color = "yellow";
                    }
                    if (form.Contains("that(green(block))"))
                    {
                        form = form.Replace("that(green(block))", "{0}");
                        color = "green";
                    }
                    if (form.Contains("that(gray(block))"))
                    {
                        form = form.Replace("that(gray(block))", "{0}");
                        color = "gray";
                    }
                    if (form.Contains("that(black(block))"))
                    {
                        form = form.Replace("that(black(block))", "{0}");
                        color = "black";
                    }
                    if (form.Contains("that(white(block))"))
                    {
                        form = form.Replace("that(white(block))", "{0}");
                        color = "white";
                    }
                    if (form.Contains("that(blue(block))"))
                    {
                        form = form.Replace("that(blue(block))", "{0}");
                        color = "blue";
                    }
                }

                    if (form.Contains("+"))
                    {
                        if (form.Split('+')[0].Contains("the(black(block))"))
                        {
                            form = form.Replace("the(black(block))", "{0}");
                            color = "black";
                        }
                        if (form.Split('+')[0].Contains("the(blue(block))"))
                        {
                            form = form.Replace("the(blue(block))", "{0}");
                            color = "blue";
                        }
                        if (form.Split('+')[0].Contains("the(pink(block))"))
                        {
                            form = form.Replace("the(pink(block))", "{0}");
                            color = "pink";
                        }
                        if (form.Split('+')[0].Contains("the(yellow(block))"))
                        {
                            form = form.Replace("the(yellow(block))", "{0}");
                            color = "yellow";
                        }
                        if (form.Split('+')[0].Contains("the(red(block))"))
                        {
                            form = form.Replace("the(red(block))", "{0}");
                            color = "red";
                        }
                        if (form.Split('+')[0].Contains("the(green(block))"))
                        {
                            form = form.Replace("the(green(block))", "{0}");
                            color = "green";
                        }
                        if (form.Split('+')[0].Contains("the(gray(block))"))
                        {
                            form = form.Replace("the(gray(block))", "{0}");
                            color = "gray";
                        }
                        if (form.Split('+')[0].Contains("the(white(block))"))
                        {
                            form = form.Replace("the(white(block))", "{0}");
                            color = "white";
                        }
                    }
                
                    return form;
                }
            //------------------------------------------------------------------------------------------------

            // MAIN method

            /*public static void Main(string[] args)
			{

				string script1 = "take the block and put it on the glass";
				string script2 = "bring the white block and put it on the red block";
				string script3 = "bring the glass and put it on the red block";
				string script4 = "take the block and put it on the glass";
				string script5 = "put it on the glass";
				string script6 = "move the yellow block";
				string script7 = "move the yellow block and place the green plate in front of red knife";
				string script8 = "Push that glass to the right of the green block";
				string script9 = "Push that knife to the left of the blue block";
				string script10 = "move the block beside the plate";
				string script11 = "move the green block to the left";
				string script12 = "move the green block there";
				string script13 = "move this block there";
				string script14 = "take this block and put it there";
				string script15 = "put this block on the table";
				string script16 = "push the block you just moved";
				string script17 = "bring the green block you just picked";
				string script18 = "bring the block that you moved";
				string script19 = "bring the block which you moved";
				string script20 = "bring the block that is beside the plate";
				string script21 = "bring the green block that is on the plate";
				string script22 = "bring the green block that is in front of the cup";
				string script23 = "bring the green block that is right of the cup";
				string script24 = "put this block on the red block";
				string script25 = "put the red block on this block";
				string script26 = "push the blue block to the left of the yellow block";
				string script27 = "push it to the left";
				string script28 = "slide it to the left";
				string script29 = "slide this block to the left";
				string script30 = "servo the red block";
				string script31 = "servo this block";
				string script32 = "grasp this block and put it on the blue block";
				string script33 = "take this one";
				string script34 = "take it and put it on the blue block"; //TODO: FIX: --> SECONT "it" IS NOT PLACED CORRECTLY
				string script35 = "put this one to the left of the blue block";
				string script36 = "slide the red block there";
				string script37 = "bring the green block that is beside the blue block";
				string script38 = "bring the green block that is in front of the red block";
				string script39= "bring the green block that is right of the blue block ";
				string script40 = "bring the blue block that is in front of the white block";
				string script41 = "bring the red block that is left of the gray block";
				string script42 = "bring the red block that is right of the gray block";
				string formout = "";

				List<string> ScriptList = new List<string>();
				ScriptList.Add(script1);
				ScriptList.Add(script2);
				ScriptList.Add(script3);
				ScriptList.Add(script4);
				ScriptList.Add(script5);
				ScriptList.Add(script6);
				ScriptList.Add(script7);
				ScriptList.Add(script8);
				ScriptList.Add(script9);
				ScriptList.Add(script10);
				ScriptList.Add(script11);
				ScriptList.Add(script12);
				ScriptList.Add(script13);
				ScriptList.Add(script14);
				ScriptList.Add(script15);
				ScriptList.Add(script16);
				ScriptList.Add(script17);
				ScriptList.Add(script18);
				ScriptList.Add(script19);
				ScriptList.Add(script20);
				ScriptList.Add(script21);
				ScriptList.Add(script22);
				ScriptList.Add(script23);
				ScriptList.Add(script24);
				ScriptList.Add(script25);
				ScriptList.Add(script26);
				ScriptList.Add(script27);
				ScriptList.Add(script28);
				ScriptList.Add(script29);
				ScriptList.Add(script30);
				ScriptList.Add(script31);
				ScriptList.Add(script32);
				ScriptList.Add(script33);
				ScriptList.Add(script34);
				ScriptList.Add(script35);
				ScriptList.Add(script36);
				ScriptList.Add(script37);
				ScriptList.Add(script38);
				ScriptList.Add(script39);
				ScriptList.Add(script40);
				ScriptList.Add(script41);
				ScriptList.Add(script42);
				for (int e = 0; e < 42; e++)
				{
					formout = NLParse(ScriptList.ElementAt(e));
					System.Console.WriteLine("________________OUTPUT" + (e + 1) + "________________________");
					System.Console.WriteLine(ScriptList.ElementAt(e));
					System.Console.WriteLine(formout);
				}

			}*/

            //string INLParser.NLParse(string rawSent)
            //{
            //    throw new NotImplementedException();
            //}

            string INLParser.ConcludeNLParse()
            {
                throw new NotImplementedException();
            }

#if !UNITY_WEBGL
            public void InitParserService(SocketConnection socketConnection, Type expectedSyntax)
            {
                throw new System.NotImplementedException();
            }

            public void InitParserService(RESTClient restClient, Type expectedSyntax)
            {
                throw new System.NotImplementedException();
            }
#endif
        }
    }
}
