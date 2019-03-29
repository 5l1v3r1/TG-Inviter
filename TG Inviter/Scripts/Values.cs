using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TG_Inviter.Scripts
{
    class Values
    {
        /*
        Client 1
	    543402
	    4af6a66e90f7c0b6d6325e1c783a2dd3

        Client 2
	    698920
	    23c015d12a7e8895a750c38b41f20222
    
        RiccoTZ
        658804
        84a2e4ef0b55ab7b05de45d530b6e6e9
            149.154.167.50:443
        +79852302657
        */

        //Proxy
        public static string proxyIP = "95.164.7.27";
        public static int proxyPORT = 32231;
        public static int proxyID = 1;

        public static int apiID { get; set; }
        public static String apiHash { get; set; }
        public static String apiPhone { get; set; }

        //TG Auth
        public static string hash = "";
        public static string code = "";

        //TG Path
        public const string AccountsPath = "Accounts";
        public const string SessionsPath = "Sessions";
        public const string UsersPath = "Users";
        public const string ChannelsPath = "Channels";
        public const string InvitedPath = "Invited";

        public static String tg_ip = "149.154.167.50";
        public static int tg_port = 443;
        
        public static String number_send_message = "";

        public static Regex rgx_files = new Regex("[^а-яА-Яa-zA-Z0-9-_ ]");
    }
}
