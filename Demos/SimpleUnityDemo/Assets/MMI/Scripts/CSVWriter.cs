using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// This static class can be used to write into an csv file asynchronously via ConcurrentQueues.
/// </summary>
public static class CSVWriter {
    public static string filename = "/test.csv";
    private static StreamWriter _tw;
    private static bool _running = false;
    private static ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
    //private static CSVWriter _cw; 


    public static void AddToQueue(string s)
    {
        _queue.Enqueue(s);
    }

    public static void StartConCurrentCSVWrite()
    {
        if (!_running)
        {
            _running = true;
            _tw = new StreamWriter(Application.dataPath + filename, true);
            Thread _t = new Thread(new ThreadStart(DequeueAndWrite));
            _t.Start();
        }

    }

    public static void ChangeFileName(string s)
    {
        filename = "./" + s;
    }

    private static void DequeueAndWrite()
    {
        while (_running)
        {
            string s;
            while (_queue.TryDequeue(out s)) _tw.WriteLine(s);
        }
    }

    public static void StopThread()
    {
        _running = false;
        _queue = new ConcurrentQueue<string>();
        _tw.Close();        
    }
    public static void CreateCSVwithString(string s)
    {
        _tw = new StreamWriter(Application.dataPath + filename, false);
        _tw.WriteLine(s);
        _tw.Close();
    }

    public static void WriteLineWithString(string s)
    {
        _tw = new StreamWriter(Application.dataPath + filename, true);
        _tw.WriteLine(s);
        _tw.Close();
    }
}
