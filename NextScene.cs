using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoxSimPlatform.Logging;
using VoxSimPlatform.Vox;

public class NextScene : ModuleBase
{
    Animator spriteAnimator1, spriteAnimator2, spriteAnimator3,
        spriteAnimator4, spriteAnimator5, spriteAnimator6, spriteAnimator7,
        spriteAnimator8, spriteAnimator9, spriteAnimator10;

    //Timer focusTimeoutTimer;
    public Image focusCircle1, focusCircle2, focusCircle3,
        focusCircle4, focusCircle5, focusCircle6, focusCircle7,
        focusCircle8, focusCircle9, focusCircle10;
    public static int focusTimeoutTime = 100;

    public UnityEngine.UI.InputField sceneInputField;
    //bool timeoutFocus;

    static List<Image> focusCircles, focusCircles1;
    static List<Animator> spriteAnimators, spriteAnimators1;

    //EventManagementModule em;
    DialogueInteractionModule dim;
    // Nada: dublicated blocks
    private static List<string> dublicated_blocks =
        new List<string>(new[] { "RedBlock2", "RedBlock1","PinkBlock1", "PinkBlock2"
            ,"YellowBlock1", "YellowBlock2", "GreenBlock1", "GreenBlock2", "BlueBlock1", "BlueBlock2"});

    List<GameObject> ReferedbBlocks;
    List<GameObject> nonReferedbBlocks;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        focusCircle1.enabled = false;
        spriteAnimator1 = focusCircle1.GetComponent<Animator>();
        spriteAnimator1.enabled = false;

        focusCircle2.enabled = false;
        spriteAnimator2 = focusCircle2.GetComponent<Animator>();
        spriteAnimator2.enabled = false;

        focusCircle3.enabled = false;
        spriteAnimator3 = focusCircle3.GetComponent<Animator>();
        spriteAnimator3.enabled = false;

        focusCircle4.enabled = false;
        spriteAnimator4 = focusCircle4.GetComponent<Animator>();
        spriteAnimator4.enabled = false;

        focusCircle5.enabled = false;
        spriteAnimator5 = focusCircle5.GetComponent<Animator>();
        spriteAnimator5.enabled = false;

        focusCircle6.enabled = false;
        spriteAnimator6 = focusCircle6.GetComponent<Animator>();
        spriteAnimator6.enabled = false;

        focusCircle7.enabled = false;
        spriteAnimator7 = focusCircle7.GetComponent<Animator>();
        spriteAnimator7.enabled = false;

        focusCircle8.enabled = false;
        spriteAnimator8 = focusCircle8.GetComponent<Animator>();
        spriteAnimator8.enabled = false;

        focusCircle9.enabled = false;
        spriteAnimator9 = focusCircle9.GetComponent<Animator>();
        spriteAnimator9.enabled = false;

        focusCircle10.enabled = false;
        spriteAnimator10 = focusCircle10.GetComponent<Animator>();
        spriteAnimator10.enabled = false;

        ReferedbBlocks = new List<GameObject>();
        nonReferedbBlocks = new List<GameObject>();

        spriteAnimators1 = new List<Animator>();
        focusCircles1 = new List<Image>();

        spriteAnimators = new List<Animator>(new[] { spriteAnimator1, spriteAnimator2, spriteAnimator3, spriteAnimator4, spriteAnimator5, spriteAnimator6, spriteAnimator7,
        spriteAnimator8, spriteAnimator9, spriteAnimator10 });
        focusCircles = new List<Image>(new[] { focusCircle1, focusCircle2, focusCircle3, focusCircle4, focusCircle5, focusCircle6, focusCircle7,
        focusCircle8, focusCircle9, focusCircle10 });

        dim.setobjlist(null);

    }

    void Awake()
    {
        //em = GameObject.FindObjectOfType<EventManagementModule>();
        dim = new DialogueInteractionModule();
        spriteAnimators = new List<Animator>(new[] { spriteAnimator1, spriteAnimator2, spriteAnimator3, spriteAnimator4, spriteAnimator5, spriteAnimator6, spriteAnimator7,
        spriteAnimator8, spriteAnimator9, spriteAnimator10 });

        focusCircles = new List<Image>(new[] { focusCircle1, focusCircle2, focusCircle3, focusCircle4, focusCircle5, focusCircle6, focusCircle7,
        focusCircle8, focusCircle9, focusCircle10 });
    }
    void Update()
    {
        if (nonReferedbBlocks != null)
        {
            // Debug.Log(string.Format("nada: step1: nonReferedbBlocks: {0}", string.Join(",", nonReferedbBlocks)));
            foreach (Image f in focusCircles1)
            {
                // Debug.Log(string.Format("nada: step2: enabled focusCircle: {0}", f));
            }
            if (dim.getobjlist() != null)
            {
                //Debug.Log(string.Format("nada: step3: getobjlist: {0}", string.Join(",", em.getobjlist())));

                for (int i = 0; i < nonReferedbBlocks.Count; i++)
                {

                    if (dim.getobjlist().Contains(nonReferedbBlocks[i].name))
                    {
                        focusCircles1[i].enabled = false;
                        focusCircles1.RemoveAt(i);
                        nonReferedbBlocks.RemoveAt(i);
                    }
                }

            }

        }

    }

    [Obsolete]
    public void playGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1, LoadSceneMode.Single);



        if (DataStore.GetBoolValue("user:isInteracting") || sceneName.Equals("Scene0"))
        {
            foreach (string block in dublicated_blocks)
            {
                if (sceneName.Equals("Scene0")) break;
                if (dim.getobjlist() != null)
                {

                    if (dim.getobjlist().Contains(block))
                    {
                        if (!ReferedbBlocks.Contains(GameObject.Find(block)))
                        {
                            ReferedbBlocks.Add(GameObject.Find(block));
                        }

                    }
                    else
                    {
                        if (!nonReferedbBlocks.Contains(GameObject.Find(block)))
                        {
                            nonReferedbBlocks.Add(GameObject.Find(block));
                        }
                    }
                }
                else
                {
                    SetValue("me:speech:intent", "Please refer to all blocks here!", string.Empty);

                }
            }

            if (sceneName.Equals("Scene0"))
            {
                //ScenesInput = GameObject.Find("MiniOptionsCanvas").GetComponent<InputField>();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + Int32.Parse(sceneInputField.text) + 1, LoadSceneMode.Single);

            }
            else if (/*sceneName.Equals("Scene0") || (*/nonReferedbBlocks.Count == 0 && (ReferedbBlocks.Count == 10 /*|| ReferedbBlocks.Count > 4)*/))
            {

                bool unload = SceneManager.UnloadScene(SceneManager.GetActiveScene().buildIndex);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1, LoadSceneMode.Single);

                //if (ReferedbBlocks.Count.Equals(10))
                //{
                //    for (int y = 0; y < 10; y++) { ReferedbBlocks.RemoveAt(y); }
                //    Debug.Log("NadaTest: loop_nonReferedbBlocks: {1} " + nonReferedbBlocks.Count);
                //    Debug.Log("NadaTest: loop_ReferedbBlocks: {1} " + ReferedbBlocks.Count);
                //}

            }
            else if (nonReferedbBlocks.Count > 0 && ReferedbBlocks.Count == 0)
            {
                SetValue("me:speech:intent", "Please refer to all blocks here!", string.Empty);

            }
            else if (nonReferedbBlocks.Count > 0 && ReferedbBlocks.Count > 0 && ReferedbBlocks.Count < 10)
            {
                SetValue("me:speech:intent", "Still! you need to refer to the selected blocks, please!", string.Empty);

                for (int i = 0; i < nonReferedbBlocks.Count; i++)
                {
                    Debug.Log(string.Format("#: {0}, nonReferedbBlocks: {1} ", i, nonReferedbBlocks[i].name));
                    if (!focusCircles1.Contains(focusCircles[i]))
                    {
                        focusCircles1.Add(focusCircles[i]);
                        focusCircles1[i].enabled = true;
                        focusCircles1[i].transform.position = Camera.main.WorldToScreenPoint(nonReferedbBlocks[i].transform.position);
                        spriteAnimators1.Add(spriteAnimators[i]);
                        spriteAnimators1[i].enabled = true;
                        spriteAnimators1[i].Play("circle_anim_test", 0, 0);
                    }
                }
            }
        }
        else
        {
            SetValue("me:speech:intent", "Please! start the interaction. Refer to all blocks. Then switch the scene!", string.Empty);
        }

        Debug.Log("NadaTest: nonReferedbBlocks: {1} " + nonReferedbBlocks.Count);
        Debug.Log("NadaTest: ReferedbBlocks: {1} " + ReferedbBlocks.Count);

    }


}


