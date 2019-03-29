using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram;
using TeleSharp.TL;
using TLSharp.Core;

namespace TG_Inviter.Scripts
{
    class Func
    {
        public static TcpClient _connect(string ip, int port)
        {
            string proxyUsername = "";
            string proxyPassword = "";
            ProxyTcpClient client = new ProxyTcpClient();
            return client.GetClient(Values.tg_ip, Values.tg_port, Values.proxyIP, Values.proxyPORT, proxyUsername, proxyPassword);
        }

        private static readonly FileSessionStore Store = new FileSessionStore();
        public static TelegramClient NewClient()
        {
            try
            {
                String path_sess = Values.SessionsPath + "\\" + Values.apiID + "_" + Values.apiPhone + "\\" + Values.apiPhone.Replace(" + ", "");
                string sessionPath = Path.Combine(Application.StartupPath, path_sess);
                Store.Load(sessionPath);

                return new TelegramClient(Values.apiID, Values.apiHash, Store, sessionPath, handler: _connect);
            }
            catch (MissingApiConfigurationException ex)
            {
                throw new Exception($"Missing app.config file",
                                    ex);
            }
            catch (SocketException ex)
            {
                throw new Exception($"Dont can connect", ex);
            }
        }
        
        public static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bitm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bitm;
        }

        /*
        Client 1
	    543402
	    4af6a66e90f7c0b6d6325e1c783a2dd3

        Client 2
	    698920
	    23c015d12a7e8895a750c38b41f20222
        */
        public class configs
        {
            public class api_Auth
            {
                public int apiID { get; set; }
                public String apiHash { get; set; }
                public String apiPhone { get; set; }
            }
            public List<api_Auth> apiAuth { get; set; } = new List<api_Auth>();
            public class apis
            {
                public int apiID { get; set; }
                public String apiHash { get; set; }
            }
            public List<apis> Apis { get; set; } = new List<apis>();
        }
        public static List<configs> configApis = new List<configs>();

        static Stream myStream;
        public static void load_config()
        {
            try
            {
                using (myStream = File.Open(Application.StartupPath + "\\" + Values.SessionsPath + "\\config.json", FileMode.OpenOrCreate, FileAccess.Read))
                {
                    StreamReader myReader = new StreamReader(myStream);
                    string json = myReader.ReadToEnd();
                    configApis = JsonConvert.DeserializeObject<List<configs>>(json);
                }
            }
            catch (Exception ex) {
                MessageBox.Show("load_config:" + ex);
                Console.WriteLine("load_config Exception:" + ex.Message);
            }
        }
        
        public static void save_config( )
        {
            bool exist = configApis.Exists(apis => apis.apiAuth.Exists(users => users.apiPhone == Values.apiPhone) );
            if (!exist)
            {
                foreach (var apis in configApis)
                {
                    apis.apiAuth.Add(new configs.api_Auth()
                        {
                            apiID = Values.apiID,
                            apiPhone = Values.apiPhone,
                            apiHash = Values.apiHash
                        }
                    );
                }
            }
            string json = JsonConvert.SerializeObject(configApis.ToArray(), Formatting.Indented);
            using (myStream = File.Open(Application.StartupPath + "\\" + Values.SessionsPath + "\\config.json", FileMode.Truncate, FileAccess.Write))
            {
                StreamWriter myWriter = new StreamWriter(myStream);
                myWriter.Write(json);
                myWriter.Flush();
            }
        }

        public static List<String> ProxyList = new List<String>();
        public static void load_proxy()
        {
            try
            {
                using (myStream = File.Open(Application.StartupPath + "\\proxy.json", FileMode.OpenOrCreate, FileAccess.Read))
                {
                    StreamReader myReader = new StreamReader(myStream);
                    string json = myReader.ReadToEnd();
                    dynamic array = JsonConvert.DeserializeObject(json);

                    Random rand = new Random();
                    Values.proxyID = rand.Next(1, array.Count);
                    Console.WriteLine("load_proxy set proxyID: "+ Values.proxyID);
                    ProxyList.Clear();

                    int i = 0; int set = 0;
                    foreach (var item in array)
                    {
                        String prox = item;
                        if (i == Values.proxyID && set == 0)
                        {
                            try
                            {
                                String[] prx = prox.Split(':');
                                Values.proxyID = Values.proxyID + 1;
                                Values.proxyIP = prx[0];
                                Values.proxyPORT = Int32.Parse(prx[1]);
                                set = 1;
                            }
                            catch (Exception) { }
                        }
                        i++;
                        ProxyList.Add(prox);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("load_proxy Exception:" + ex.Message);
            }
        }

        public static void Store_proxy( String type, String proxy )
        {
            try
            {
                if (type.Equals("delete"))
                {
                    ProxyList.Remove(Values.proxyIP+":"+Values.proxyPORT);
                }
                if (type.Equals("add"))
                {
                    ProxyList.Add(proxy);
                }
                string json = JsonConvert.SerializeObject(ProxyList.ToArray(), Formatting.Indented);
                using (saveStream = File.Open(Application.StartupPath + "\\proxy.json", FileMode.Truncate, FileAccess.Write))
                {
                    StreamWriter myWriter = new StreamWriter(saveStream);
                    myWriter.Write(json);
                    myWriter.Flush();
                }

                Random rand = new Random();
                Values.proxyID = rand.Next(1, ProxyList.Count);
                load_proxy();
            }
            catch (Exception ex)
            {
                Console.WriteLine("store_proxy Exception:" + ex.Message);
            }
        }

        public class InvitedUsers
        {
            public Int64 Id { get; set; }
            public long? AccessHash { get; set; }
            public String FirstName { get; set; }
            public String LastName { get; set; }
            public String Username { get; set; }
            public String Phone { get; set; }
        }
        public static List<InvitedUsers> ListInvitedUsers = new List<InvitedUsers>();

        public static void LoadInvitedUsers( int ChannelId )
        {
            try
            {
                using (myStream = File.Open(Application.StartupPath + "\\" + Values.InvitedPath + "\\InvitedUser_" + ChannelId + ".json", FileMode.OpenOrCreate, FileAccess.Read))
                {
                    ListInvitedUsers.Clear();
                    StreamReader myReader = new StreamReader(myStream);
                    string json = myReader.ReadToEnd();
                    if (json.Length == 0) { json = " [ ] "; }
                    ListInvitedUsers = JsonConvert.DeserializeObject<List<InvitedUsers>>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadInvitedUsers Exception:" + ex.Message);
            }
        }

        public static void SaveInvitedUsers( int ChannelId, bool clean )
        {
            if (clean) { ListInvitedUsers.Clear(); }
            string json = JsonConvert.SerializeObject(ListInvitedUsers.ToArray(), Formatting.Indented);
            using (saveStream = File.Open(Application.StartupPath + "\\" + Values.InvitedPath + "\\InvitedUser_" + ChannelId + ".json", FileMode.OpenOrCreate, FileAccess.Write))
            {
                StreamWriter myWriter = new StreamWriter(saveStream);
                myWriter.Write(json);
                myWriter.Flush();
            }
        }

        public class store_users
        {
            public String Channel_Title { get; set; }
            public Int64 Channel_Id { get; set; }
            public long? Channel_AccessHash { get; set; }
            public String Channel_Username { get; set; }
            public int Channel_Date { get; set; }
            public int Channel_ParticipantsCount { get; set; }
            public int Channel_AdminsCount { get; set; }
            public int Channel_KickedCount { get; set; }
            public class users
            {
                public bool MutualContact { get; set; }
                public bool Deleted { get; set; }
                public bool Bot { get; set; }
                public bool Verified { get; set; }
                public Int64 Id { get; set; }
                public long? AccessHash { get; set; }
                public String FirstName { get; set; }
                public String LastName { get; set; }
                public String Username { get; set; }
                public String Phone { get; set; }
            }
            public List<users> Users { get; set; } = new List<users>();
        }

        static Stream saveStream;
        public static void Save_users( store_users result )
        {
            List<store_users> _users = new List<store_users>();
            _users.Add(new store_users()
            {
                Channel_Title = result.Channel_Title,
                Channel_Id = result.Channel_Id,
                Channel_AccessHash = result.Channel_AccessHash,
                Channel_Username = result.Channel_Username,
                Channel_Date = result.Channel_Date,
                Channel_ParticipantsCount = result.Channel_ParticipantsCount,
                Channel_AdminsCount = result.Channel_AdminsCount,
                Channel_KickedCount = result.Channel_KickedCount,
                Users = result.Users
            });

            string json = JsonConvert.SerializeObject(_users.ToArray(), Formatting.Indented);
            String name_of_file = Values.rgx_files.Replace(result.Channel_Title, "");
            Int32 timestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            if (name_of_file.Length <= 1) { name_of_file = "" + timestamp; }
            using (saveStream = File.Create(Application.StartupPath + "\\" + Values.UsersPath + "\\users_" + name_of_file + ".json"))
            {
                StreamWriter myWriter = new StreamWriter(saveStream);
                myWriter.Write(json);
                myWriter.Flush();
            }
        }

        public class stored_users
        {
            public String Name { get; set; }
            public Int64 Total { get; set; }
        }

        public static List<string> get_stored_users()
        {
            List<string> fileList = new List<string>();
            try
            {
                Directory.CreateDirectory(Values.UsersPath);
                string path = Directory.GetCurrentDirectory();
                DirectoryInfo dir = new DirectoryInfo(@path);

                foreach (var file in dir.GetDirectories())
                {
                    if (file.Name == Values.UsersPath)
                    {
                        foreach (var item in file.GetFiles("*.json"))
                        {
                            fileList.Add(item.Name);
                        }
                    }
                }
                return fileList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("get_stored_users Exception:" + ex.Message);
                return fileList;
            }
        }

        public static List<store_users> load_users_to_invite( String group_name )
        {
            List<store_users> users = new List<store_users>();
            try
            {
                group_name = Values.rgx_files.Replace(group_name, "");
                using (myStream = File.Open(Application.StartupPath + "\\" + Values.UsersPath + "\\" + group_name + ".json", FileMode.OpenOrCreate, FileAccess.Read))
                {
                    StreamReader myReader = new StreamReader(myStream);
                    string json = myReader.ReadToEnd();
                    users = JsonConvert.DeserializeObject<List<store_users>>(json);
                }
                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine("load_users_to_invite Exception:" + ex.Message);
                return users;
            }
        }

        public class ItemChannel
        {
            public String telegram { get; set; }
        }
        public List<ItemChannel> ListChannels { get; set; } = new List<ItemChannel>();

        public static List<ItemChannel> load_channels_to_join(String group_name)
        {
            List<ItemChannel> ListChannels = new List<ItemChannel>();
            try
            {
                group_name = Values.rgx_files.Replace(group_name, "");
                using (myStream = File.Open(Application.StartupPath + "\\" + Values.UsersPath + "\\" + group_name + ".json", FileMode.OpenOrCreate, FileAccess.Read))
                {
                    StreamReader myReader = new StreamReader(myStream);
                    string json = myReader.ReadToEnd();
                    ListChannels = JsonConvert.DeserializeObject<List<ItemChannel>>(json);
                }
                return ListChannels;
            }
            catch (Exception ex)
            {
                Console.WriteLine("load_users_to_invite Exception:" + ex.Message);
                return ListChannels;
            }
        }
    }
}
