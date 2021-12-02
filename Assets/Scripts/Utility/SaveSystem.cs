//using System.Collections;
//using System.Collections.Generic;
//using System;
//using System.Runtime.Serialization.Formatters.Binary;
//using System.IO;
//using UnityEngine;


//public class SaveSystem
//{
//    public static string SaveDataPath = Application.persistentDataPath + "/";


//    public static void Save(object aObj, string aFileName)
//    {
//        BinaryFormatter bf = new BinaryFormatter();
//        FileStream file = File.Open(SaveDataPath + aFileName + ".dat",
//                                    FileMode.OpenOrCreate);

//        bf.Serialize(file, aObj);
//        file.Close();
//    }

//    public static object Load(string aFileName)
//    {
//        if (File.Exists(SaveDataPath + aFileName + ".dat"))
//        {
//            BinaryFormatter bf = new BinaryFormatter();
//            FileStream file = File.Open(SaveDataPath + aFileName + ".dat",
//                                        FileMode.Open);
//            object data = bf.Deserialize(file);
//            file.Close();

//            return data;
//        }
//        return null;
//    }


//    public static bool DataExists(string aFileName)
//    {
//        if (File.Exists(SaveDataPath + aFileName + ".dat"))
//        {
//            return true;
//        }
//        return false;
//    }


//    public static Vector3 GetVector3FromString(string rString)
//    {
//        string[] temp = rString.Substring(1, rString.Length - 2).Split(',');
//        float x = float.Parse(temp[0]);
//        float y = float.Parse(temp[1]);
//        float z = float.Parse(temp[2]);
//        Vector3 rValue = new Vector3(x, y, z);
//        return rValue;
//    }

//    public static void Clear(string aFileName)
//    {
//        File.Delete(SaveDataPath + aFileName + ".dat");
//    }

//    public static void DestroyAllSaveData()
//    {
//        Directory.Delete(SaveDataPath, true);
//    }
//}
