using System;
using System.Collections.Generic;
using Convai.Scripts;
using Service;
using UnityEngine.Serialization;

[Serializable]
public class P10Response
{
    public List<ResponseAudio> responseAudios = new List<ResponseAudio>();
    public List<ByteArr> visemesDataArray = new List<ByteArr>();
}

[Serializable]
public struct ByteArr
{
    public byte[] visemesData;

    public ByteArr(byte[] data) : this()
    {
        visemesData = data;
    }
}