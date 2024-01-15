using SpotifyKnob.MVVM.Model;
using System;
using System.IO;
using System.Xml.Serialization;

namespace SpotifyKnob.Core
{
    static class UserSaveDataHelper
    {
        private static string _userSaveDataPath = AppDomain.CurrentDomain.BaseDirectory + "User.xml";

        public static bool LoadUserData(out UserSaveData userSaveData)
        {
            userSaveData = null;
            var serializer = new XmlSerializer(typeof(UserSaveData));

            if (!File.Exists(_userSaveDataPath))
            {
                return false;
            }

            bool succeeded = true;

            using (Stream reader = new FileStream(_userSaveDataPath, FileMode.Open))
            {
                try
                {
                    userSaveData = (UserSaveData)serializer.Deserialize(reader);
                }
                catch (Exception)
                {
                    File.Delete(_userSaveDataPath);

                    succeeded = false;
                }
            }

            return succeeded;
        }

        public static void SaveUserData(UserSaveData userSaveData)
        {
            TextWriter writer = new StreamWriter(_userSaveDataPath);
            var serializer = new XmlSerializer(typeof(UserSaveData));

            serializer.Serialize(writer, userSaveData);
            writer.Close();
        }
    }
}
