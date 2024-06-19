using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoxSimPlatform.Global;

public class PointingCircleVariance : MonoBehaviour
{
    public GameObject PointingMarkerObj;

    public Vector3 StandardScale,StandardPosition;
    Vector3 targetPos;
    Vector3 startPos;
    public float transformSpeed;
    float dom;
    float scaleFactor;
    public Vector3 targetScale;

    // Start is called before the first frame update
    void Start()
    {
        if (DataStore.GetBoolValue("user:pointValid")) {
            PointingMarkerObj.transform.position = DataStore.GetVector3Value("user:pointPos");
            startPos = PointingMarkerObj.transform.position;
            targetPos = new Vector3(startPos.x + 0.3f, startPos.y, startPos.z + -0.3f);
            PointingMarkerObj.transform.localScale = StandardScale;
            scaleFactor = RandomHelper.RandomFloat(
                StandardScale.magnitude - Mathf.Sqrt(StandardScale.x / 2f * StandardScale.x / 2f * 3),
                StandardScale.magnitude + Mathf.Sqrt(StandardScale.x / 2f * StandardScale.x / 2f * 3),
                (int)(RandomHelper.RangeFlags.MinInclusive | RandomHelper.RangeFlags.MaxInclusive));
            targetScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }
    }

    private void LateUpdate()
    {
        //if (DataStore.GetBoolValue("user:pointValid"))
        //{
        startPos = PointingMarkerObj.transform.position;

        if (Mathf.Abs(targetPos.magnitude - startPos.magnitude) > Constants.EPSILON)
        {
            dom = Time.deltaTime * 7f;
            PointingMarkerObj.transform.position = Vector3.Lerp(startPos, targetPos, dom / (startPos - targetPos).magnitude);
            //Debug.Log(string.Format("0 pointing noisy: " + startPos + " and " + targetPos));
        }
        else
        {
            startPos = targetPos;
            //Debug.Log(string.Format("1 pointing noisy: " + startPos + " and " + targetPos));
            targetPos = new Vector3(startPos.x + 0.3f, startPos.y, startPos.z + -0.3f);
            //Debug.Log(string.Format("2 pointing noisy: " + targetPos ));
        }
        // }
    }

    // Update is called once per frame
    void Update()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        startPos = PointingMarkerObj.transform.position;
        targetPos = new Vector3(startPos.x + 0.3f, startPos.y, startPos.z + -0.3f);

        if (!sceneName.Equals("Scene0"))
        {
            if (DataStore.GetBoolValue("user:pointValid"))
            {

                if (Mathf.Abs(targetScale.magnitude - PointingMarkerObj.transform.localScale.magnitude) > Constants.EPSILON)
                {
                    if (targetScale.magnitude < PointingMarkerObj.transform.localScale.magnitude)
                    {
                        PointingMarkerObj.transform.localScale = new Vector3(
                            PointingMarkerObj.transform.localScale.x - Time.deltaTime * transformSpeed,
                            PointingMarkerObj.transform.localScale.y - Time.deltaTime * transformSpeed,
                            PointingMarkerObj.transform.localScale.z - Time.deltaTime * transformSpeed);

                    }
                    else if (targetScale.magnitude > PointingMarkerObj.transform.localScale.magnitude)
                    {
                        PointingMarkerObj.transform.localScale = new Vector3(
                            PointingMarkerObj.transform.localScale.x + Time.deltaTime * transformSpeed,
                            PointingMarkerObj.transform.localScale.y + Time.deltaTime * transformSpeed,
                            PointingMarkerObj.transform.localScale.z + Time.deltaTime * transformSpeed);
                    }
                }
                else
                {
                    scaleFactor = RandomHelper.RandomFloat(
                        StandardScale.magnitude - Mathf.Sqrt(StandardScale.x / 2f * StandardScale.x / 2f * 3),
                        StandardScale.magnitude + Mathf.Sqrt(StandardScale.x / 2f * StandardScale.x / 2f * 3),
                        (int)(RandomHelper.RangeFlags.MinInclusive | RandomHelper.RangeFlags.MaxInclusive));
                    targetScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                }

            }
            else
            {
                PointingMarkerObj.transform.localScale = StandardScale;
            }


        }


    }
}
