using SpotifyKnob.MVVM.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;

namespace SpotifyKnob.Core
{
    static class HotkeysSaveDataHelper
    {
        private static string _keysSaveDataPath = AppDomain.CurrentDomain.BaseDirectory + "Keys.xml";

        public static void SaveHotkeys(HotkeysSaveData hotkeys)
        {
            TextWriter writer = new StreamWriter(_keysSaveDataPath);
            var serializer = new XmlSerializer(typeof(HotkeysSaveData));

            serializer.Serialize(writer, hotkeys);
            writer.Close();
        }

        public static bool LoadHotkeys(out HotkeysSaveData hotkeys)
        {
            hotkeys = new HotkeysSaveData();
            var serializer = new XmlSerializer(typeof(HotkeysSaveData));

            if (!File.Exists(_keysSaveDataPath))
            {
                return false;
            }

            bool success = true;

            using (Stream reader = new FileStream(_keysSaveDataPath, FileMode.Open))
            {
                try
                {
                    hotkeys = (HotkeysSaveData)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    File.Delete(_keysSaveDataPath);

                    success = false;
                }
            }

            return success;
        }
    }
}
