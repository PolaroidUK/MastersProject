using System;
using System.Collections.Generic;
using System.IO;
using Convai.Scripts;
using Service;
using UnityEngine;
using Google.Protobuf;
using Unity.Mathematics;
using Random = System.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace DefaultNamespace
{
    [CreateAssetMenu(menuName = "P10", fileName = "Response List")]
    public class TestConfig : ScriptableObject
    {
        [Header("Loading Bar")] 
        public float InitialFillupTime = 5;
        
        [Header("Fillerwords and gestures")]
        [SerializeField] private int _currentIndex = 0;
        public bool recordMode;
        public float StartDelaySeconds = 1;
        public float AfterDelaySeconds = 1;
        [SerializeField] public List<String> recordingPhrases;
        [SerializeField] private List<P10Response> _responses;
        

        public void InitializeResponseList(int n)
        {
            foreach (var response in _responses)
            {
                for (var i = response.responseAudios.Count - 1; i >= 0; i--)
                {
#if UNITY_EDITOR
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(response.responseAudios[i].AudioClip));
#endif
                }
            }
            _responses = new List<P10Response>(n);
            for (int i = 0; i < n; i++)
            {
                _responses.Add(new P10Response());
            }
            currentIndex = 0;
        }
        public int currentIndex
        {
            get => _currentIndex;
            set
            {
                if (value - currentIndex  == 1 && value > _responses.Count-1) _responses.Add(new P10Response());
                _currentIndex = value;
            }
        }
        
        public string GetNewClipName()
        {
            return $"response-{currentIndex}-{_responses[currentIndex].responseAudios.Count}";
        }
        
        public void Set(ResponseAudio responseAudio)
        {
            if (_responses.Count == 0) _responses.Add(new P10Response());
            _responses[currentIndex].responseAudios.Add(responseAudio);
        }
        
        public List<ResponseAudio> GetResponseAudio()
        {
            return _responses[currentIndex] != null ? _responses[currentIndex].responseAudios : new List<ResponseAudio>();
        }

        private List<List<ResponseAudio>> tempHolderList = new List<List<ResponseAudio>>();
        Random _random = new Random(Seed:Guid.NewGuid().GetHashCode());
        public List<ResponseAudio> GetRandomFiller()
        {
            if (tempHolderList.Count == 0)
            {
                foreach (var p10Response in _responses)
                {
                    tempHolderList.Add(p10Response.responseAudios);
                }
            }
            var index = _random.Next(0, tempHolderList.Count);
            var res = tempHolderList[index];
            tempHolderList.RemoveAt(index);
            return res;
        }
        public void Set(VisemesData visemesData)
        {
            if (_responses.Count == 0) _responses.Add(new P10Response());
            MemoryStream stream = new MemoryStream();
            visemesData.WriteTo(stream);
            byte[] serializedData = stream.ToArray();
            _responses[currentIndex].visemesDataArray.Add(new ByteArr(serializedData));
        }
        
        public List<VisemesData> GetVisemesData()
        {
            List<VisemesData> deserializedData = new List<VisemesData>();
            foreach (var visemesdata in _responses[currentIndex].visemesDataArray)
            {
                VisemesData visemesData = new VisemesData();
                visemesData.MergeFrom(new CodedInputStream(visemesdata.visemesData));
                deserializedData.Add(visemesData);
            }
            return deserializedData;
        }
    }
}