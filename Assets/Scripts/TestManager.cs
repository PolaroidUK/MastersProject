using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Convai.Scripts;
using Convai.Scripts.Utils;
using DefaultNamespace;
using Service;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = System.Random;

public class TestManager : MonoBehaviour
{
    [FormerlySerializedAs("responseList")] [SerializeField] private TestConfig testConfig;
    public TestMode testMode;
    
    [SerializeField] private KeyCode up = KeyCode.UpArrow;
    [SerializeField] private KeyCode down = KeyCode.DownArrow;
    [SerializeField] private KeyCode play = KeyCode.P;
    [SerializeField] private KeyCode record = KeyCode.R;
    [SerializeField] private Slider _slider;
    
    private int phraseIndex = 0;
    private bool firstRun = true;
    private void Start()
    {
        if (testConfig.recordMode)
        {
            testConfig.InitializeResponseList(testConfig.recordingPhrases.Count);
        }
    }
    private void OnDestroy() => testConfig.recordMode = false;

    private void OnApplicationQuit()
    {
        Save();
    }

    private void Save()
    {
        string root = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\P10Data";
        GetOrCreateDirectory(root);
        
        var currLargSubDirNr = new DirectoryInfo(root).GetDirectories()
            .Where(e => int.TryParse(e.Name, out _))
            .Select(e => int.Parse(e.Name))
            .OrderByDescending(e => e)
            .ToArray();
        var path = currLargSubDirNr.Length == 0
            ? $"{root}\\0"
            : $"{root}\\{int.Parse(currLargSubDirNr.First().ToString()) + 1}";
        GetOrCreateDirectory(path);

        var chatboxParent = GameObject.Find("ChatBox").transform.parent;
        var text = string.Join("\n", chatboxParent.GetComponentsInChildren<TextMeshProUGUI>().Select(e => e.text));
        var sr = File.CreateText($"{path}/{SceneManager.GetActiveScene().name}.txt");
        sr.WriteLine(RemoveRichText(text));
        sr.Close();
    }

    private DirectoryInfo GetOrCreateDirectory(string path)
    {
        var directoryInfo = new DirectoryInfo(path);
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }
        return directoryInfo;
    }
    void Update()
    {
        if (!testConfig.recordMode) return;
        if (Input.GetKeyDown(up))
        {
            testConfig.currentIndex++;
            Debug.Log($"Next recording index {testConfig.currentIndex}");
        }
        if (Input.GetKeyDown(down))
        {
            testConfig.currentIndex = math.max(0, testConfig.currentIndex-1);
            Debug.Log($"Previous recording index {testConfig.currentIndex}");
        }
        if (Input.GetKeyDown(play))
        {
            PlayRandomFiller();
        }
        if (Input.GetKeyDown(record))
        {
            if (phraseIndex >= testConfig.recordingPhrases.Count)
            {
                Debug.Log("No more phrases to record");
                return;
            }
            ConvaiNPCManager.Instance.activeConvaiNPC.SendTextDataAsync($"Can you say \"{testConfig.recordingPhrases[phraseIndex]}\" for me");
            phraseIndex++;
            if (!firstRun)
            {
                testConfig.currentIndex++;
            }
            firstRun = false;
        }
    }
    private float timeSinceStartTalking = 0f;
    public IEnumerator FillerSequence()
    {
        timeSinceStartTalking = Time.time;
        yield return new WaitForSeconds(testConfig.StartDelaySeconds);
        PlayRandomFiller();
        while (ConvaiNPCManager.Instance.activeConvaiNPC.IsCharacterTalking)
        {
            yield return null;
        }
        yield return new WaitForSeconds(testConfig.AfterDelaySeconds);
        var timeSoFar = Time.time - timeSinceStartTalking;
        yield return new WaitForSeconds(Math.Max(0, GetNextValue()-timeSoFar));
        ConvaiNPCManager.Instance.activeConvaiNPC.allowPlayback = true;
    }

    public IEnumerator EmptySequence()
    {
        yield return new WaitForSeconds(GetNextValue());
        ConvaiNPCManager.Instance.activeConvaiNPC.allowPlayback = true;

    }
    public IEnumerator PlayLoadingBar()
    {
        yield return new WaitForSeconds(0.1f);
        _slider.gameObject.SetActive(true);
        _slider.value = 0f;
        var startTime = Time.time;
        float percentage;
        const float initialFillTime = 1.4f;
        while (Time.time - startTime < initialFillTime)
        {
            percentage = (Time.time - startTime) / initialFillTime;
            _slider.value = math.remap(0f,1f,0f,0.7f, percentage);
            yield return null;
        }
        var nextStartTime = Time.time;
        var duration = GetNextValue() - initialFillTime - 0.1f;
        var totalFillTime = 7 - initialFillTime;

        while (Time.time - startTime < duration)  
        {
            percentage = (Time.time - nextStartTime) / totalFillTime;
            _slider.value = math.remap(0f,1f,0.7f,1f, percentage);
            yield return null;
        }
        _slider.value = 1f;
        yield return new WaitForSeconds(0.08f);
        _slider.value = 0f;
        _slider.gameObject.SetActive(false);
        ConvaiNPCManager.Instance.activeConvaiNPC.allowPlayback = true;
    }

    private Random _random = new Random(Seed:Guid.NewGuid().GetHashCode());
    private List<float> waitingValues = new List<float>() { 2, 3.25f, 4.5f, 5.75f, 7};
    private List<float> defaultValues = new List<float>() { 2, 3.25f, 4.5f, 5.75f, 7};
    private float GetNextValue()
    {
        if (waitingValues.Count == 0)
        {
            foreach (var defaultValue in defaultValues)
            {
                waitingValues.Add(defaultValue);
            }
        }
        var index = _random.Next(0, waitingValues.Count);
        var res = waitingValues[index];
        waitingValues.RemoveAt(index);
        return res;
    }
    private void PlayRandomFiller()
    {
        var npc = ConvaiNPCManager.Instance.activeConvaiNPC;
        npc.GetFillerResponses.Clear();
        foreach (var responseAudio in testConfig.GetRandomFiller())
        {
            npc.GetFillerResponses.Enqueue(responseAudio);
        }
        var data = testConfig.GetVisemesData();
        foreach (var visemesData in data)
        {
            switch (visemesData.Visemes.Sil)
            {
                case -2:
                    npc.convaiLipSync.faceDataList.Add(new List<VisemesData>());
                    break;
                default:
                    npc.convaiLipSync.faceDataList[^1].Add(visemesData);
                    break;
            }
        }
    }
    public static string RemoveRichText(string input)
    {
 
        input = RemoveRichTextDynamicTag(input, "color");
 
        input = RemoveRichTextTag(input, "b");
        input = RemoveRichTextTag(input, "i");
 
 
        // TMP
        input = RemoveRichTextDynamicTag(input, "align");
        input = RemoveRichTextDynamicTag(input, "size");
        input = RemoveRichTextDynamicTag(input, "cspace");
        input = RemoveRichTextDynamicTag(input, "font");
        input = RemoveRichTextDynamicTag(input, "indent");
        input = RemoveRichTextDynamicTag(input, "line-height");
        input = RemoveRichTextDynamicTag(input, "line-indent");
        input = RemoveRichTextDynamicTag(input, "link");
        input = RemoveRichTextDynamicTag(input, "margin");
        input = RemoveRichTextDynamicTag(input, "margin-left");
        input = RemoveRichTextDynamicTag(input, "margin-right");
        input = RemoveRichTextDynamicTag(input, "mark");
        input = RemoveRichTextDynamicTag(input, "mspace");
        input = RemoveRichTextDynamicTag(input, "noparse");
        input = RemoveRichTextDynamicTag(input, "nobr");
        input = RemoveRichTextDynamicTag(input, "page");
        input = RemoveRichTextDynamicTag(input, "pos");
        input = RemoveRichTextDynamicTag(input, "space");
        input = RemoveRichTextDynamicTag(input, "sprite index");
        input = RemoveRichTextDynamicTag(input, "sprite name");
        input = RemoveRichTextDynamicTag(input, "sprite");
        input = RemoveRichTextDynamicTag(input, "style");
        input = RemoveRichTextDynamicTag(input, "voffset");
        input = RemoveRichTextDynamicTag(input, "width");
 
        input = RemoveRichTextTag(input, "u");
        input = RemoveRichTextTag(input, "s");
        input = RemoveRichTextTag(input, "sup");
        input = RemoveRichTextTag(input, "sub");
        input = RemoveRichTextTag(input, "allcaps");
        input = RemoveRichTextTag(input, "smallcaps");
        input = RemoveRichTextTag(input, "uppercase");
        // TMP end
 
 
        return input;
 
    }
 
 
 
    private static string RemoveRichTextDynamicTag (string input, string tag)
    {
        int index = -1;
        while (true)
        {
            index = input.IndexOf($"<{tag}=");
            //Debug.Log($"{{{index}}} - <noparse>{input}");
            if (index != -1)
            {
                int endIndex = input.Substring(index, input.Length - index).IndexOf('>');
                if (endIndex > 0)
                    input = input.Remove(index, endIndex + 1);
                continue;
            }
            input = RemoveRichTextTag(input, tag, false);
            return input;
        }
    }
    private static string RemoveRichTextTag (string input, string tag, bool isStart = true)
    {
        while (true)
        {
            int index = input.IndexOf(isStart ? $"<{tag}>" : $"</{tag}>");
            if (index != -1)
            {
                input = input.Remove(index, 2 + tag.Length + (!isStart).GetHashCode());
                continue;
            }
            if (isStart)
                input = RemoveRichTextTag(input, tag, false);
            return input;
        }
    }
}