﻿using Newtonsoft.Json;
using System.IO;
using System.Text;
using UnityEngine;

namespace CowMilking.Persistency
{
    public class PersistencyManager
    {
        private readonly string _key = "知らないないない生き方ひとつ耐えらんない夜に涙ふたつ笑えええない毎日にどうかどうかサヨナラをしたい";

        private string Encrypt(string s)
        {
            StringBuilder str = new();
            for (var i = 0; i < s.Length; i++)
            {
                str.Append((char)(s[i] ^ _key[i % _key.Length]));
            }
            return str.ToString();
        }

        private static PersistencyManager _instance;
        public static PersistencyManager Instance
        {
            get
            {
                _instance ??= new();
                return _instance;
            }
        }

        private SaveData _saveData;
        public SaveData SaveData
        {
            get
            {
                if (_saveData == null)
                {
                    if (File.Exists($"{Application.persistentDataPath}/save.bin"))
                    {
                        _saveData = JsonConvert.DeserializeObject<SaveData>(Encrypt(File.ReadAllText($"{Application.persistentDataPath}/save.bin")));
                    }
                    else
                    {
                        _saveData = new();
                        _saveData.OwnedCows.AddRange(new[] { "NEUTRAL", "NEUTRAL", "NEUTRAL" }); // Thanks Newtonsoft for being shit
                    }
                }
                return _saveData;
            }
        }

        public void Save()
        {
            File.WriteAllText($"{Application.persistentDataPath}/save.bin", Encrypt(JsonConvert.SerializeObject(_saveData)));
        }
    }
}