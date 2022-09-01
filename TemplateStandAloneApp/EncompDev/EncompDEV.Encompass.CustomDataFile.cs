using System.IO;
using System.Xml.Serialization;
using EllieMae.Encompass.Client;

namespace EncompDEV.Encompass
{
    public class CustomDataFile
    {
        // params in constructor
        private Session SESSION = null;
        private string sFileTitle = null;

        // we want to reuse same object between repeated FileExists & Read
        EllieMae.Encompass.BusinessObjects.DataObject obj_file = null;

        public CustomDataFile(Session session, string sFileTitle)
        {
            if (session == null || sFileTitle == null)
            {
                return;
            }
            this.SESSION = session;
            this.sFileTitle = sFileTitle;
        }

        public string GetFileTitle()
        {
            return sFileTitle;
        }

        public bool FileExists()
        {
            try
            {
                LoadFileObjectIfNeeded();
                if (obj_file != null)
                {
                    if (obj_file.Data != null)
                    {
                        return true; // file exists, data read
                    }
                }
            }
            catch { }
            // fallthrough - file does not exist or could not be read
            return false;
        }

        public string ReadFile()
        {
            return System.Text.Encoding.ASCII.GetString(ReadFileBytes());
        }

        public byte[] ReadFileBytes()
        {
            LoadFileObjectIfNeeded();
            return obj_file.Data;
        }

        public void SaveFile(string sContent)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(sContent);
            SaveFile(bytes);
        }

        public void SaveFile(byte[] bytes)
        {
            obj_file = new EllieMae.Encompass.BusinessObjects.DataObject(bytes);
            SESSION.DataExchange.SaveCustomDataObject(sFileTitle, obj_file);
        }

        public void AppendToFile(string sContent)
        {
            // when we append - object changes.  clear original if it's loaded
            obj_file = null;
            // do append
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(sContent);
            EllieMae.Encompass.BusinessObjects.DataObject obj_append = new EllieMae.Encompass.BusinessObjects.DataObject(bytes);
            SESSION.DataExchange.AppendToCustomDataObject(sFileTitle, obj_append);
        }

        // deserialize file as a known XML
        // throw on error
        public T DeserializeXML<T>()
        {
            string s = ReadFile();
            using (StringReader sr = new StringReader(s))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                return (T)xmlSerializer.Deserialize(sr);
            }
        }

        // Serialize file from known XML
        // throw on error
        public void SerializeXML<T>(T obj)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
            using (StringWriter sw = new StringWriter())
            {
                xmlSerializer.Serialize(sw, obj);
                SaveFile(sw.ToString());
            }
        }

        // load obj_file, store it for future use 
        private void LoadFileObjectIfNeeded()
        {
            if (obj_file == null)
            {
                obj_file = SESSION.DataExchange.GetCustomDataObject(sFileTitle);
            }
        }
    }
}
