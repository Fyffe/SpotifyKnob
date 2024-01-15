using SpotifyKnob.MVVM.Model;
using System;
using System.IO;
using System.Xml.Serialization;

namespace SpotifyKnob.Core
{
    static class AuthSaveDataHelper
    {
        private static string _authSaveDataPath = AppDomain.CurrentDomain.BaseDirectory + "Auth.xml";

        public static bool LoadAuthData(out AuthSaveData authSaveData)
        {
            authSaveData = new AuthSaveData();
            var serializer = new XmlSerializer(typeof(AuthSaveData));

            if (!File.Exists(_authSaveDataPath))
            {
                return false;
            }

            bool succeeded = true;

            using (Stream reader = new FileStream(_authSaveDataPath, FileMode.Open))
            {
                try
                {
                    authSaveData = (AuthSaveData)serializer.Deserialize(reader);
                }
                catch (Exception)
                {
                    File.Delete(_authSaveDataPath);

                    succeeded = false;
                }
            }

            return succeeded;
        }

        public static void SaveAuthData(AuthSaveData authSaveData)
        {
            TextWriter writer = new StreamWriter(_authSaveDataPath);
            var serializer = new XmlSerializer(typeof(AuthSaveData));

            serializer.Serialize(writer, authSaveData);
            writer.Close();
        }

        public static void CleanAuthData()
        {
            File.Delete(_authSaveDataPath);
        }
    }
}
