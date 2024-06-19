using System;
using System.IO;
using UnityEngine;

//namespace Assets._Core.Scripts.CogArch.Modules
//{
    public class Logging

    {
    public StreamWriter logFileStream;

    public Logging()
        {
            //logFileStream = logfile;
        }

        public void Log(string msg)
        {
            if (logFileStream == null)
            {
                var fname = string.Format("DianaInteraction-{0}.log", System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
                logFileStream = File.AppendText(fname);
                Debug.Log("Opened log file at " + fname);
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


    }
//}

