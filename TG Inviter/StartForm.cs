using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram;
using TeleSharp.TL;
using TeleSharp.TL.Channels;
using TeleSharp.TL.Messages;
using TG_Inviter.Scripts;
using TLSharp.Core;

namespace TG_Inviter
{
    public partial class StartForm : Form
    {
        public StartForm()
        {
            InitializeComponent();
            if (!Directory.Exists(Values.AccountsPath))
            {
                Directory.CreateDirectory(Values.AccountsPath);
            }
            if (!Directory.Exists(Values.SessionsPath))
            {
                Directory.CreateDirectory(Values.SessionsPath);
            }
            if (!Directory.Exists(Values.UsersPath))
            {
                Directory.CreateDirectory(Values.UsersPath);
            }
            if (!Directory.Exists(Values.ChannelsPath))
            {
                Directory.CreateDirectory(Values.ChannelsPath);
            }
            if (!Directory.Exists(Values.InvitedPath))
            {
                Directory.CreateDirectory(Values.InvitedPath);
            }
            Load_config();
            Load_proxy();
            Update_users_list();
            count_db_invited.Text = "" + Func.ListInvitedUsers.Count;
            Timers();
        }

        private async void Cli_auth_btn_Click(object sender, EventArgs e)
        {
            await Auth_cli();
        }

        private static readonly FileSessionStore Store = new FileSessionStore();
        private TelegramClient client;
        List<UserMessage> _resultUserMessages = new List<UserMessage>();
        private class UserMessage
        {
            public UserMessage()
            {
            }
            public int CreateDateTime { get; set; }
            public int? FromId { get; set; }
            public string Message { get; set; }
            public TLAbsMessageMedia Media { get; set; }
        }

        private async void Cli_get_code_btn_Click(object sender, EventArgs e)
        {
            if (Values.apiID > 0)
            {

            }
            else
            {
                MessageBox.Show("Please select Apis");
                return;
            }
            Values.number_send_message = cli_user_phone_tv.Text;
            if (string.IsNullOrWhiteSpace(Values.number_send_message))
            {
                Console.WriteLine("IsNullOrWhiteSpace");
                return;
            }
            var number_phone = Values.number_send_message.StartsWith("+") ?
                Values.number_send_message.Substring(1, Values.number_send_message.Length - 1) :
                Values.number_send_message;

            Values.apiPhone = number_phone;
            Save_config();

            client = Func.NewClient();
            await client.ConnectAsync();
            try
            {
                Console.WriteLine("SendCodeRequestAsync :" + number_phone);
                Values.hash = await client.SendCodeRequestAsync(number_phone);
                cli_user_phone_tv.Enabled = false;
                cli_get_code_btn.Enabled = false;
                cli_user_code_tv.Enabled = true;
                cli_login_btn.Enabled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendCodeRequestAsync ex:" + ex);
                message_box.Text = ex.Message;
            }
        }

        private async void Cli_login_btn_Click(object sender, EventArgs e)
        {
            if (Values.apiID > 0)
            {

            }
            else
            {
                MessageBox.Show("Please select Apis");
                return;
            }
            Values.code = cli_user_code_tv.Text;
            if (String.IsNullOrWhiteSpace(Values.code))
            {
                message_box.Text = "CodeToAuthenticate empty :" + Values.code;
            }
            TLUser user = null;
            try
            {
                user = await client.MakeAuthAsync(cli_user_phone_tv.Text, Values.hash, Values.code);
            }
            catch (CloudPasswordNeededException ex)
            {
                if (cli_user_pass_tv.Text.Length > 0)
                {
                    var password = await client.GetPasswordSetting();
                    var password_str = cli_user_pass_tv.Text;
                    user = await client.MakeAuthWithPasswordAsync(password, password_str);
                }
                else
                {
                    Console.WriteLine("CloudPasswordNeededException ex:" + ex);
                    message_box.Text = "Please put Cloud password";
                }
            }
            catch (InvalidPhoneCodeException ex)
            {
                Console.WriteLine("InvalidPhoneCodeException ex:" + ex);
                message_box.Text = ex.Message;
            }
            catch (Exception ex)
            {
                Console.WriteLine("cli_login_btn_Click ex:" + ex);
                message_box.Text = ex.Message;
            }
            try
            {
                var photo = user.Photo as TLUserProfilePhoto;
                var photoLocation = photo.PhotoSmall as TLFileLocation;
                var resFile = await client.GetFile(new TLInputFileLocation()
                {
                    LocalId = photoLocation.LocalId,
                    Secret = photoLocation.Secret,
                    VolumeId = photoLocation.VolumeId
                }, 512 * 1024, 0);
                Bitmap image;
                using (MemoryStream stream = new MemoryStream(resFile.Bytes))
                {
                    image = new Bitmap(stream);
                }
                cli_picture.Image = image;
            }
            catch (Exception ex)
            {
                Console.WriteLine("cli_login_btn_Click TLUserProfilePhoto ex:" + ex);
                message_box.Text = ex.Message;
            }
        }

        public static int timeoutSocket = 0;
        int trycount = 0;
        private async Task Auth_cli()
        {
            try
            {
                if (cli_user_phone_tv.Text.Length == 0)
                {
                    MessageBox.Show("Please enter Phone Number");
                    return;
                }
                else
                {
                    Values.apiPhone = cli_user_phone_tv.Text;
                }
                if (Values.apiID > 0)
                {

                }
                else
                {
                    MessageBox.Show("Please select Apis");
                    return;
                }
                Load_config();

                if (!Directory.Exists(Values.SessionsPath + "\\" + Values.apiID + "_" + Values.apiPhone))
                {
                    Directory.CreateDirectory(Values.SessionsPath + "\\" + Values.apiID + "_" + Values.apiPhone);
                }
                string sessionPath = Path.Combine(Application.StartupPath, Values.SessionsPath + "\\" + Values.apiID + "_" + Values.apiPhone + "\\session_app");
                Store.Load(sessionPath);

                timeoutSocket = (int)timeout_Socket.Value;
                client = new TelegramClient(Values.apiID, Values.apiHash, Store, sessionPath, handler: Func._connect);

                await client.ConnectAsync();

                if (client.IsUserAuthorized())
                {
                    cli_auth_tv.Text = "Failed to login";
                    cli_auth_tv.BackColor = System.Drawing.Color.DarkRed;
                }
                else
                {
                    cli_auth_tv.Text = "Success Login";
                    cli_auth_tv.BackColor = System.Drawing.Color.LightGreen;
                    try
                    {
                        client = Func.NewClient();
                        await client.ConnectAsync();
                        var result = await client.GetContactsAsync();

                        Console.WriteLine("auth_cli 0 Users:" + result.Users.Count);
                        Console.WriteLine("auth_cli 0 Contacts:" + result.Contacts.Count);

                        tab_contact_count_list.Text = "Total Users " + result.Users.Count;

                        try
                        {
                            if (result.Users.Count > 0) {

                                var user = result.Users.ToList()
                                    .Where(x => x.GetType() == typeof(TLUser))
                                    .Cast<TLUser>()
                                    .FirstOrDefault(x => x.Self);

                                cli_id.Text = "" + user.Id;
                                cli_name.Text = user.Username;
                                cli_user_phone_tv.Text = user.Phone;
                                var photo = user.Photo as TLUserProfilePhoto;
                                var photoLocation = photo.PhotoSmall as TLFileLocation;
                                var resFile = await client.GetFile(new TLInputFileLocation()
                                {
                                    LocalId = photoLocation.LocalId,
                                    Secret = photoLocation.Secret,
                                    VolumeId = photoLocation.VolumeId
                                }, 512 * 1024, 0);

                                Bitmap image;
                                using (MemoryStream stream = new MemoryStream(resFile.Bytes))
                                {
                                    image = new Bitmap(stream);
                                }
                                cli_picture.Image = image;
                            }
                        }
                        catch (System.NullReferenceException ex)
                        {
                            Console.WriteLine("auth_cli 0 ex:" + ex);
                            message_box.Text = "Client Nullable";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("auth_cli 0 ex:" + ex);
                            message_box.Text = "Fail load picture";
                        }
                        try
                        {
                            if (result.Contacts.Count > 0)
                            {
                                Update_contact_list();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("auth_cli 1 ex:" + ex.Message);
                            message_box.Text = "Empty Contacts";
                        }
                        message_box.Text = "Users:" + result.Users.Count + " Contacts:" + result.Contacts.Count;
                        Update_groups_list();
                    }
                    catch (System.Net.WebException ex)
                    {
                        Console.WriteLine("auth_cli [" + trycount + "] WebException ex:" + ex.Message);
                        if (trycount < 5)
                        {
                            trycount++;
                            Func.Store_proxy("delete", "");
                            proxy_now.Text = "Set: " + Values.proxyIP + ":" + Values.proxyPORT;
                            await Auth_cli();
                        }
                        else
                        {
                            trycount = 0;
                            message_box.Text = "auth_cli " + ex.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        trycount = 0;
                        Console.WriteLine("auth_cli 2 ex:" + ex.Message);
                        message_box.Text = "auth_cli " + ex.Message;
                    }
                }
            }
            catch (System.Net.WebException ex)
            {
                Console.WriteLine("auth_cli [" + trycount + "] WebException ex:" + ex.Message);
                if (trycount < 5)
                {
                    trycount++;
                    Func.Store_proxy("delete", "");
                    proxy_now.Text = "Set: " + Values.proxyIP + ":" + Values.proxyPORT;
                    await Auth_cli();
                }
                else {
                    trycount = 0;
                    message_box.Text = "auth_cli " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                trycount = 0;
                Console.WriteLine("auth_cli 3 ex:" + ex);
                message_box.Text = "auth_cli " + ex.Message;
            }
        }

        private void Select_ApiAuth_Click(object sender, EventArgs e)
        {
            try
            {
                cli_selected_api.Text = auths_list.SelectedItems[0].SubItems[1].Text;
                cli_user_phone_tv.Text = auths_list.SelectedItems[0].SubItems[2].Text;

                Values.apiID = Convert.ToInt32(auths_list.SelectedItems[0].SubItems[1].Text);
                Values.apiPhone = auths_list.SelectedItems[0].SubItems[2].Text;
                Values.apiHash = auths_list.SelectedItems[0].SubItems[3].Text;
            }
            catch (Exception) { }
        }

        private void Select_Apis_Click(object sender, EventArgs e)
        {
            try
            {
                cli_selected_api.Text = apis_list.SelectedItems[0].SubItems[1].Text;
                cli_user_phone_tv.Text = "";
                Values.apiID = Convert.ToInt32(apis_list.SelectedItems[0].SubItems[1].Text);
                Values.apiHash = apis_list.SelectedItems[0].SubItems[2].Text;
            }
            catch (Exception) { }
        }

        public void Load_config()
        {
            try
            {
                Func.load_config();
                auths_list.Items.Clear();
                apis_list.Items.Clear();
                foreach (var apis in Func.configApis)
                {
                    foreach (var api in apis.apiAuth)
                    {
                        string apiID = "" + api.apiID;
                        ListViewItem list = new ListViewItem(api.apiPhone != null ? api.apiPhone : "0000");
                        list.SubItems.Add(apiID);
                        list.SubItems.Add(api.apiPhone);
                        list.SubItems.Add(api.apiHash);
                        auths_list.Items.AddRange(new ListViewItem[] { list });
                    }
                    foreach (var api in apis.Apis)
                    {
                        string apiID = "" + api.apiID;
                        ListViewItem list = new ListViewItem(apiID != null ? apiID : "0000");
                        list.SubItems.Add(apiID);
                        list.SubItems.Add(api.apiHash);
                        apis_list.Items.AddRange(new ListViewItem[] { list });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load_config ex:" + ex);
                message_box.Text = "Load_config " + ex.Message;
            }
        }

        public void Save_config()
        {
            Func.save_config();
            Load_config();
        }

        public void Update_contact_list_Click(object sender, EventArgs e)
        {
            Update_contact_list();
        }

        public async void Update_contact_list() {
            try
            {
                tab_contact_list_contact.Items.Clear();
                await client.ConnectAsync();
                var result = await client.GetContactsAsync();

                tab_contact_count_list.Text = "Total Users " + result.Users.Count;

                foreach (var item in result.Contacts.ToList())
                {
                    var user = result.Users.ToList()
                               .OfType<TLUser>()
                               .FirstOrDefault(x => x.Id == item.UserId);

                    if (user.Phone.ToString().Contains(""))
                    {
                        ListViewItem list = new ListViewItem(user.Username != null ? user.Username : " ");
                        list.SubItems.Add(user.Phone);
                        list.SubItems.Add(user.FirstName != null ? user.FirstName : " ");
                        list.SubItems.Add(user.LastName != null ? user.LastName : " ");
                        tab_contact_list_contact.Items.AddRange(new ListViewItem[] { list });
                    }
                }

                var dialogs = (TLDialogs)await client.GetUserDialogsAsync();
                var chats = dialogs.Chats.ToList().OfType<TLChannel>().ToList();

                tab_groups_count_list.Text = "Total Groups " + chats.Count;

                tab_groups_list_groups.Items.Clear();
                mass_mess_list.Items.Clear();
                mass_invite_list.Items.Clear();
                foreach (var chat in chats)
                {
                    ListViewItem list = new ListViewItem(chat.Title != null ? chat.Title : "---");
                    list.SubItems.Add(chat.Id + "");
                    list.SubItems.Add(chat.AccessHash + "");
                    tab_groups_list_groups.Items.AddRange(new ListViewItem[] { list });
                }
                foreach (var chat in chats)
                {
                    ListViewItem list = new ListViewItem(chat.Title != null ? chat.Title : "---");
                    list.SubItems.Add(chat.Id + "");
                    list.SubItems.Add(chat.AccessHash + "");
                    mass_mess_list.Items.AddRange(new ListViewItem[] { list });
                }
                foreach (var chat in chats)
                {
                    ListViewItem list = new ListViewItem(chat.Title != null ? chat.Title : "---");
                    list.SubItems.Add(chat.Id + "");
                    list.SubItems.Add(chat.AccessHash + "");
                    mass_invite_list.Items.AddRange(new ListViewItem[] { list });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("update_contact_list ex:" + ex);
                message_box.Text = "update_contact_list " + ex.Message;
            }
        }

        private async void Load_selected_contact(object sender, EventArgs e)
        {
            try
            {
                selected_user_pnumber.Text = tab_contact_list_contact.SelectedItems[0].SubItems[1].Text;
                try
                {
                    await client.ConnectAsync();
                    var result = await client.GetContactsAsync();

                    var user = result.Users.ToList()
                        .Where(x => x.GetType() == typeof(TLUser))
                        .Cast<TLUser>()
                        .FirstOrDefault(x => x.Phone == selected_user_pnumber.Text);

                    selected_user_id.Text = "" + user.Id;

                    var photo = user.Photo as TLUserProfilePhoto;
                    var photoLocation = photo.PhotoSmall as TLFileLocation;

                    var resFile = await client.GetFile(new TLInputFileLocation()
                    {
                        LocalId = photoLocation.LocalId,
                        Secret = photoLocation.Secret,
                        VolumeId = photoLocation.VolumeId
                    }, 512 * 1024, 0);

                    Bitmap image;
                    using (MemoryStream stream = new MemoryStream(resFile.Bytes))
                    {
                        image = new Bitmap(stream);
                    }
                    selected_user_img.Image = image;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("load_selected_contact 1 ex:" + ex);
                    message_box.Text = "load_selected_contact " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("load_selected_contact 2 ex:" + ex);
                message_box.Text = "load_selected_contact " + ex.Message;
            }
        }

        public void Update_groups_list_Click(object sender, EventArgs e)
        {
            Update_groups_list();
        }

        public async void Update_groups_list()
        {
            try
            {
                tab_groups_list_groups.Items.Clear();
                mass_mess_list.Items.Clear();
                mass_invite_list.Clear();
                await client.ConnectAsync();
                var dialogs = (TLDialogs)await client.GetUserDialogsAsync();
                var chats = dialogs.Chats.ToList().OfType<TLChannel>().ToList();

                tab_groups_count_list.Text = "Total Groups " + chats.Count;

                foreach (var chat in chats)
                {
                    ListViewItem list = new ListViewItem(chat.Title != null ? chat.Title : "---");
                    list.SubItems.Add(chat.Id + "");
                    list.SubItems.Add(chat.AccessHash + "");
                    tab_groups_list_groups.Items.AddRange(new ListViewItem[] { list });
                }
                foreach (var chat in chats)
                {
                    ListViewItem list = new ListViewItem(chat.Title != null ? chat.Title : "---");
                    list.SubItems.Add(chat.Id + "");
                    list.SubItems.Add(chat.AccessHash + "");
                    mass_mess_list.Items.AddRange(new ListViewItem[] { list });
                }
                foreach (var chat in chats)
                {
                    ListViewItem list = new ListViewItem(chat.Title != null ? chat.Title : "---");
                    list.SubItems.Add(chat.Id + "");
                    list.SubItems.Add(chat.AccessHash + "");
                    mass_invite_list.Items.AddRange(new ListViewItem[] { list });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("update_groups_list ex:" + ex);
                message_box.Text = "update_groups_list " + ex.Message;
            }
        }

        private async void Join_to_channel(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("join_to_channel");
                if (client.IsUserAuthorized() == false)
                {
                    message_box.Text = "join_to_channel IsUserAuthorized:" + client.IsUserAuthorized();
                    return;
                }
                await client.ConnectAsync();

                var Update = await client.SearchUserAsync(groups_invite_to_group.Text);
                var ch = Update.Chats.Where(c => c.GetType() == typeof(TLChannel)).Cast<TLChannel>().FirstOrDefault();
                
                var request = new TeleSharp.TL.Channels.TLRequestJoinChannel()
                {
                    Channel = new TLInputChannel
                    {
                        ChannelId = ch.Id,
                        AccessHash = (long)ch.AccessHash
                    }
                };
                var responsjoin = await client.SendRequestAsync<TLUpdates>(request);
                message_box.Text = "join_to_channel " + responsjoin.ToString();
                Update_groups_list();
            }
            catch (Exception ex)
            {
                Console.WriteLine("join_to_channel ex:" + ex);
                message_box.Text = "join_to_channel " + ex.Message;
            }
        }

        private async void Join_to_chat(object sender, EventArgs e)
        {
            try
            {
                if (client.IsUserAuthorized() == false)
                {
                    message_box.Text = "join_to_chat IsUserAuthorized:" + client.IsUserAuthorized();
                    return;
                }
                await client.ConnectAsync();
                var inviteReq = new TLRequestImportChatInvite()
                {
                    Hash = groups_invite_to.Text.Replace("https://t.me/joinchat/", "")
                };

                var inviteResp = await client.SendRequestAsync<object>(inviteReq);
                Console.WriteLine("join_to_chat:" + inviteResp.ToString());
                message_box.Text = "join_to_chat " + inviteResp.ToString();
                Update_groups_list();
            }
            catch (Exception ex)
            {
                Console.WriteLine("join_to_chat ex:" + ex);
                message_box.Text = "join_to_chat " + ex.Message;
            }
        }

        private async void Leave_channel(object sender, EventArgs e)
        {
            try
            {
                if (client.IsUserAuthorized() == false)
                {
                    message_box.Text = "Leave_channel IsUserAuthorized:" + client.IsUserAuthorized();
                    return;
                }
                try
                {
                    int Id = Int32.Parse(tab_groups_list_groups.SelectedItems[0].SubItems[1].Text);
                    long AccessHash = Int64.Parse(tab_groups_list_groups.SelectedItems[0].SubItems[2].Text);

                    var requestLeave = new TeleSharp.TL.Channels.TLRequestLeaveChannel()
                    {
                        Channel = new TLInputChannel
                        {
                            ChannelId = Id,
                            AccessHash = AccessHash
                        }
                    };
                    var leaveResponse = await client.SendRequestAsync<TLUpdates>(requestLeave);
                    Console.WriteLine("Leave_channel:" + leaveResponse.ToString());
                    message_box.Text = "Leave_channel " + leaveResponse.ToString();
                    message_box.Text = "Success leave";
                    Update_groups_list();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Leave_channel 1 ex:" + ex);
                    message_box.Text = "Leave_channel " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Leave_channel 2 ex:" + ex);
                message_box.Text = "Leave_channel " + ex.Message;
            }
        }

        private async void Mass_Join(object sender, MouseEventArgs e)
        {
            try
            {
                if (client == null)
                {
                    message_box.Text = "Mass_Join : Client Nullable";
                    return;
                }
                if (client.IsUserAuthorized() == false)
                {
                    message_box.Text = "Mass_Join IsUserAuthorized:" + client.IsUserAuthorized();
                    return;
                }
                try
                {
                    await client.ConnectAsync();
                    String groupName = list_to_join.SelectedItem.ToString();

                    if (!groupName.Contains("https://t.me/joinchat/"))
                    {
                        // Join to Group
                        var Update = await client.SearchUserAsync(groupName);
                        var ch = Update.Chats.Where(c => c.GetType() == typeof(TLChannel)).Cast<TLChannel>().FirstOrDefault();

                        var request = new TeleSharp.TL.Channels.TLRequestJoinChannel()
                        {
                            Channel = new TLInputChannel
                            {
                                ChannelId = ch.Id,
                                AccessHash = (long)ch.AccessHash
                            }
                        };
                        await client.SendRequestAsync<TLUpdates>(request);
                    }
                    else
                    {
                        //Join to Chat
                        var inviteReq = new TLRequestImportChatInvite()
                        {
                            Hash = groups_invite_to.Text.Replace("https://t.me/joinchat/", "")
                        };
                        await client.SendRequestAsync<object>(inviteReq);
                    }
                    message_box.Text = "Success Mass Join";
                    Update_groups_list();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Mass_Join 1 ex:" + ex);
                    message_box.Text = "Mass_Join " + ex.Message;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Mass_Join 2 ex:" + ex);
                message_box.Text = "Mass_Join " + ex.Message;
            }
        }

        private async void Parce_selected_chat( object sender, MouseEventArgs e)
        {
            try
            {
                Random rand_ms = new Random();
                int pause_ms = (int)groups_pause_ms.Value;
                decimal max_parce = groups_count_get_users.Value;
                String groupName = tab_groups_list_groups.SelectedItems[0].SubItems[0].Text;
                if (groupName.Length > 3)
                {
                    message_box.Text = "Selected :" + groupName;
                    await client.ConnectAsync();
                    var result = new Func.store_users();
                    var dialogs = (TLDialogs)await client.GetUserDialogsAsync();
                    var main = dialogs.Chats.ToList().Where(c => c.GetType() == typeof(TLChannel))
                                .Cast<TLChannel>()
                                .FirstOrDefault(c => c.Title == (groupName));
                    var req = new TLRequestGetFullChannel()
                    {
                        Channel = new TLInputChannel() { AccessHash = main.AccessHash.Value, ChannelId = main.Id }
                    };

                    var res = await client.SendRequestAsync<TeleSharp.TL.Messages.TLChatFull>(req);

                    var offset = 0;

                    result.Channel_Title = main.Title;
                    result.Channel_Id = main.Id;
                    result.Channel_AccessHash = main.AccessHash;
                    result.Channel_Username = main.Username;
                    result.Channel_Date = main.Date;
                    try { result.Channel_ParticipantsCount = (int)(res.FullChat as TLChannelFull).ParticipantsCount; }
                    catch (Exception) { result.Channel_KickedCount = 0; }
                    try { result.Channel_AdminsCount = (int)(res.FullChat as TLChannelFull).AdminsCount; }
                    catch (Exception) { result.Channel_AdminsCount = 0; }
                    try { result.Channel_KickedCount = (int)(res.FullChat as TLChannelFull).KickedCount; }
                    catch (Exception) { result.Channel_KickedCount = 0; }

                    count_db_invited.Text = "0";
                    Func.ListInvitedUsers.Clear();
                    Func.LoadInvitedUsers(main.Id);

                    while (offset < (res.FullChat as TLChannelFull).ParticipantsCount && offset < max_parce)
                    {
                        var pReq = new TLRequestGetParticipants()
                        {
                            Channel = new TLInputChannel() { AccessHash = main.AccessHash.Value, ChannelId = main.Id },
                            Filter = new TLChannelParticipantsRecent() { },
                            Limit = 200,
                            Offset = offset
                        };
                        var pRes = await client.SendRequestAsync<TLChannelParticipants>(pReq);
                        
                        bool totalUsr = false;
                        try
                        {
                            if (Func.ListInvitedUsers.Count > 0)
                            {
                                totalUsr = true;
                            }
                        }
                        catch (Exception) { }

                        Func.InvitedUsers _users = new Func.InvitedUsers();
                        foreach (var user in pRes.Users.ToList().Cast<TLUser>())
                        {
                            var listUsersTG = new Func.store_users.users();

                            listUsersTG.MutualContact = user.MutualContact;
                            listUsersTG.Deleted = user.Deleted;
                            listUsersTG.Bot = user.Bot;
                            listUsersTG.Verified = user.Verified;
                            listUsersTG.Id = user.Id;
                            listUsersTG.AccessHash = user.AccessHash;
                            listUsersTG.FirstName = user.FirstName;
                            listUsersTG.LastName = user.LastName;
                            listUsersTG.Username = user.Username;
                            listUsersTG.Phone = user.Phone;
                            result.Users.Add(listUsersTG);

                            bool addUsr = true;
                            if (totalUsr)
                            {
                                addUsr = Func.ListInvitedUsers.Exists(u => u.Id == user.Id);
                            }

                            if (!addUsr || !totalUsr)
                            {
                                if (user.Bot == false && user.Deleted == false) {
                                    Console.WriteLine("ADD " + user.Id);
                                    var NewUser = new Func.InvitedUsers();
                                    NewUser.Id = user.Id;
                                    NewUser.AccessHash = user.AccessHash;
                                    NewUser.FirstName = user.FirstName;
                                    NewUser.LastName = user.LastName;
                                    NewUser.Username = user.Username;
                                    NewUser.Phone = user.Phone;

                                    Func.ListInvitedUsers.Add(NewUser);
                                }
                            }
                            else { Console.WriteLine("NOT ADD " + user.Id); }
                        }
                        
                        Func.SaveInvitedUsers(main.Id, false);
                        count_db_invited.Text = "" + Func.ListInvitedUsers.Count;

                        offset += 200;

                        Console.WriteLine("parce_selected_chat :" + pRes.Users.ToList() + " max :" + max_parce);
                        message_box.Text = "Parce "+ groupName +" :" + offset + " max :" + max_parce;
                        
                        await Task.Delay(pause_ms + rand_ms.Next(100, 300));
                    }
                    Func.Save_users(result);
                    message_box.Text = "All done";
                }
                else
                {
                    message_box.Text = "Selected short name of Group :" + groupName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("parce_selected_chat ex:" + ex);
                message_box.Text = "parce_selected_chat " + ex.Message;
            }
        }

        private void Clear_db_invited_users(object sender, EventArgs e)
        {
            try
            {
                int ChannelId = Int32.Parse(tab_mass_invites_id_group.Text);
                Func.SaveInvitedUsers(ChannelId, true);
                count_db_invited.Text = "" + Func.ListInvitedUsers.Count;
            }
            catch (Exception ex) { message_box.Text = "clear_db_invited_users " + ex.Message; }
        }

        private void Reload_stored_users(object sender, EventArgs e)
        {
            Update_users_list();
        }

        public void Update_users_list()
        {
            try
            {
                tab_mass_invites_list_groups.Items.Clear();
                foreach (var stored in Func.get_stored_users())
                {
                    tab_mass_invites_list_groups.Items.Add(stored.Replace(".json", ""));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("update_users_list ex:" + ex);
                message_box.Text = "update_users_list " + ex.Message;
            }
        }

        private void Select_group_to_message(object sender, EventArgs e)
        {
            String selected_title = "";
            try { selected_title = mass_mess_list.SelectedItems[0].SubItems[0].Text; } catch (Exception) { }
            String selected_id = "";
            try { selected_id = mass_mess_list.SelectedItems[0].SubItems[1].Text; } catch (Exception) { }
            String selected_hash = "";
            try { selected_hash = mass_mess_list.SelectedItems[0].SubItems[2].Text; } catch (Exception) { }
            if (selected_title.Length > 3)
            {
                mass_mess_selected_title.Text = "" + selected_title;
                mass_mess_selected_id.Text = "" + selected_id;
                mass_mess_selected_hash.Text = "" + selected_hash;
            }
        }

        public async void Send_message_to_group(object sender, EventArgs e)
        {
            Random rand_ms = new Random();
            int pause_ms = (int)mass_mess_pause.Value;
            string message = mass_mess_message.Text;
            if (message.Length < 3) { message_box.Text = "Please Enter message!"; return; }

            for (int i = 0; i < mass_mess_list.Items.Count; i++)
            {

                mass_mess_list.Items[i].Focused = true;
                mass_mess_list.Items[i].BackColor = System.Drawing.Color.DarkGray;

                String selected_title = "";
                try { selected_title = mass_mess_list.Items[i].SubItems[0].Text; } catch (Exception) { }
                String selected_id = "";
                try { selected_id = mass_mess_list.Items[i].SubItems[1].Text; } catch (Exception) { }
                String selected_hash = "";
                try { selected_hash = mass_mess_list.Items[i].SubItems[2].Text; } catch (Exception) { }
                if (selected_title.Length > 3)
                {
                    mass_mess_selected_title.Text = "" + selected_title;
                    mass_mess_selected_id.Text = "" + selected_id;
                    mass_mess_selected_hash.Text = "" + selected_hash;
                }
                try
                {
                    string ChannelTitle = mass_mess_list.Items[i].SubItems[0].Text;
                    int ChannelId = Convert.ToInt32(mass_mess_list.Items[i].SubItems[1].Text);
                    long ChannelAccessHash = Convert.ToInt64(mass_mess_list.Items[i].SubItems[2].Text);
                    
                    await client.ConnectAsync();
                    await client.SendMessageAsync(new TLInputPeerChannel() { ChannelId = ChannelId, AccessHash = ChannelAccessHash }, message);

                    mass_mess_list.Items[i].BackColor = System.Drawing.Color.ForestGreen;
                    mass_mess_list.Items[i].ForeColor = System.Drawing.Color.White;
                }
                catch (Exception ex) {
                    mass_mess_list.Items[i].BackColor = System.Drawing.Color.DarkRed;
                    mass_mess_list.Items[i].ForeColor = System.Drawing.Color.White;
                    Console.WriteLine(ex.Message);
                }
                await Task.Delay(pause_ms + rand_ms.Next(100, 300));
            }
        }

        private void Select_group_to_invite(object sender, EventArgs e)
        {
            String selected_title = "";
            try { selected_title = mass_invite_list.SelectedItems[0].SubItems[0].Text; } catch (Exception) { }
            String selected_id = "";
            try { selected_id = mass_invite_list.SelectedItems[0].SubItems[1].Text; } catch (Exception) { }
            String selected_hash = "";
            try { selected_hash = mass_invite_list.SelectedItems[0].SubItems[2].Text; } catch (Exception) { }
            if (selected_title.Length > 3)
            {
                tab_mass_invites_name_group.Text = "" + selected_title;
                tab_mass_invites_id_group.Text = "" + selected_id;
                tab_mass_invites_hash_group.Text = "" + selected_hash;
            }
        }

        private void Import_list_groups(object sender, EventArgs e)
        {
            StreamReader sr;
            Stream myStream = null;
            OpenFileDialog importFile = new OpenFileDialog();
            importFile.InitialDirectory = Directory.GetCurrentDirectory()+ "\\Channels";
            importFile.Filter = "json files (*.json)|*.json";
            importFile.FilterIndex = 2;
            importFile.RestoreDirectory = true;

            if (importFile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = importFile.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            int inputCount = 0;
                            if (File.Exists(importFile.FileName))
                            {
                                sr = new StreamReader(importFile.FileName);

                                list_to_join.Items.Clear();
                                List<Func.ItemChannel> ListChannels = new List<Func.ItemChannel>();
                                string json = sr.ReadToEnd();
                                ListChannels = JsonConvert.DeserializeObject<List<Func.ItemChannel>>(json);

                                foreach (var Channel in ListChannels)
                                {
                                    if ( Channel.telegram.Contains("https://t.me"))
                                    {
                                        inputCount++;
                                        mass_join_total_channels.Text = inputCount+ " Channels";
                                        list_to_join.Items.Add(Channel.telegram);
                                    }
                                }
                            }
                            else
                            {
                                message_box.Text = "FILE DONT EXIST";
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    message_box.Text = "ERROR OPENED FILE";
                    MessageBox.Show("ERROR OPENED FILE");
                }
            }
        }
        
        List<Func.store_users> UsersForInvite = new List<Func.store_users>();
        private void Select_stored_users(object sender, EventArgs e)
        {
            try
            {
                string sel = "";
                foreach (var item in tab_mass_invites_list_groups.SelectedItems)
                {
                    sel += tab_mass_invites_list_groups.GetItemText(item);
                }
                UsersForInvite = Func.load_users_to_invite(sel);
                foreach (var groups in UsersForInvite)
                {
                    tab_mass_invites_messages.Text = "Success loaded";
                    tab_mass_invites_admins.Text = "" + groups.Channel_AdminsCount;
                    tab_mass_invites_users.Text = "" + groups.Users.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("select_stored_users ex:" + ex.Message);
                message_box.Text = "select_stored_users " + ex.Message;
            }
        }

        private void Select_invited_db(object sender, EventArgs e)
        {
            int ChannelId = Convert.ToInt32(mass_invite_list.SelectedItems[0].SubItems[1].Text);
            Func.LoadInvitedUsers(ChannelId);
            count_db_invited.Text = ""+Func.ListInvitedUsers.Count;
        }

        bool active = false;
        private void Stop_mass_invite(object sender, EventArgs e)
        {
            active = false;
        }
        private async void Start_mass_invite(object sender, EventArgs e)
        {
            try
            {
                active = true;
                Random rand_ms = new Random();
                int pause_ms = (int)tab_mass_invites_pause_ms.Value;
                string ChannelTitle = mass_invite_list.SelectedItems[0].SubItems[0].Text;
                int ChannelId = Convert.ToInt32(mass_invite_list.SelectedItems[0].SubItems[1].Text);
                long ChannelAccessHash = Convert.ToInt64(mass_invite_list.SelectedItems[0].SubItems[2].Text);

                int invited = 0; int skipped = 0;

                foreach (var groups in UsersForInvite)
                {
                    for (int i = 0; i < groups.Users.Count; i++)
                    {
                        int total = Convert.ToInt32(tab_mass_invites_total_invite.Text);
                        if (!active || invited >= total)
                        {
                            Func.SaveInvitedUsers( ChannelId, false );
                            message_box.Text = "Inviter done";
                            return;
                        }

                        Func.SaveInvitedUsers(ChannelId, false);
                        bool exist = true;
                        exist = Func.ListInvitedUsers.Exists(u => u.Id == groups.Users[i].Id);

                        if (groups.Users[i].Bot == false && groups.Users[i].Deleted == false && exist == false)
                        {
                            invited++;
                            mass_invite_total_invited.Text = "[+" + invited + "] [" + skipped + "]";
                            
                            var NewUser = new Func.InvitedUsers();
                            NewUser.Id = groups.Users[i].Id;
                            NewUser.AccessHash = groups.Users[i].AccessHash;
                            NewUser.FirstName = groups.Users[i].FirstName;
                            NewUser.LastName = groups.Users[i].LastName;
                            NewUser.Username = groups.Users[i].Username;
                            NewUser.Phone = groups.Users[i].Phone;
                            Func.ListInvitedUsers.Add(NewUser);

                            count_db_invited.Text = "" + Func.ListInvitedUsers.Count;
                            
                            try
                            {
                                await client.ConnectAsync();
                                var r = new TLRequestInviteToChannel
                                {
                                    Channel = new TLInputChannel
                                    {
                                        ChannelId = ChannelId,
                                        AccessHash = ChannelAccessHash
                                    },
                                    Users = new TLVector<TLAbsInputUser>
                                    {
                                        new TLInputUser {
                                             UserId = Convert.ToInt32(groups.Users[i].Id),
                                             AccessHash = Convert.ToInt64(groups.Users[i].AccessHash)
                                        }
                                    }
                                };
                                await client.SendRequestAsync<object>(r);

                                invited++;
                                mass_invite_total_invited.Text = "[+" + invited + "] [" + skipped + "]";
                                count_db_invited.Text = ""+Func.ListInvitedUsers.Count;
                                message_box.Text = "Success";
                            }
                            catch (Exception ex)
                            {
                                mass_invite_total_invited.Text = "[+" + invited + "] [" + skipped + "]";
                                count_db_invited.Text = ""+Func.ListInvitedUsers.Count;
                                Console.WriteLine(ex.Message);
                                if (ex.Message.Equals("USER_PRIVACY_RESTRICTED"))
                                {
                                    message_box.Text = "USER_PRIVACY_RESTRICTED";
                                }
                                else if (ex.Message.Equals("USER_ID_INVALID"))
                                {
                                    message_box.Text = "USER_ID_INVALID";
                                }
                                else if (ex.Message.Equals("USER_NOT_MUTUAL_CONTACT"))
                                {
                                    message_box.Text = "USER_NOT_MUTUAL_CONTACT";
                                }
                                else
                                {
                                    message_box.Text = "start_mass_invite " + ex.Message;
                                    return;
                                }
                            }
                            await Task.Delay(pause_ms + rand_ms.Next(100, 300));
                        }
                        else
                        {
                            skipped++;
                            mass_invite_total_invited.Text = "[+" + invited + "] [" + skipped + "]";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("start_mass_invite ex:" + ex);
                message_box.Text = "start_mass_invite " + ex.Message;
            }
        }

        public void Add_proxy_Click(object sender, EventArgs e)
        {
            String proxy = "";
            if (new_proxy.Text.Length > 10)
            {
                proxy = new_proxy.Text;
                try
                {
                    String[] p = proxy.Split(':');
                    if (p[0].Length >= 8 && p[1].Length >= 2)
                    {
                        proxy_result.Text = "Success";
                        Func.Store_proxy("add", proxy);
                        Load_proxy();
                    }
                    else { proxy_result.Text = "Missing parce"; }
                }
                catch (Exception) { proxy_result.Text = "Fail Split :"; }
            }
            else { proxy_result.Text = "Length < 10"; }
        }


        private void Load_proxy()
        {
            try
            {
                Func.load_proxy();
                list_proxy.Items.Clear();
                int inputCount = 0;
                foreach (String item in Func.ProxyList.ToArray())
                {
                    inputCount++;
                    total_proxy.Text = inputCount + " Proxyes";
                    list_proxy.Items.Add(item);
                }
                proxy_now.Text = "Set: " + Values.proxyIP + ":" + Values.proxyPORT;
            }
            catch (Exception)
            {
                message_box.Text = "ERROR OPENED FILE";
                MessageBox.Show("ERROR OPENED FILE");
            }
        }

        static System.Windows.Forms.Timer timerAuth = new System.Windows.Forms.Timer();
        static System.Windows.Forms.Timer timerMessage = new System.Windows.Forms.Timer();
        public void Timers()
        {
            timerAuth.Interval = 3000;
            timerAuth.Tick += new EventHandler(Auth_Tick);
            timerAuth.Enabled = true;
            timerMessage.Interval = 5000;
            timerMessage.Tick += new EventHandler(Message_Tick);
            timerMessage.Enabled = false;
        }
        private void Auth_Tick(object Sender, EventArgs e)
        {
            try
            {
                if (client != null) {
                    if (client.IsConnected)
                    {
                        cli_auth_tv.Text = "Success Login";
                        cli_auth_tv.BackColor = System.Drawing.Color.LightGreen;
                        cli_auth_tv.ForeColor = System.Drawing.Color.White;
                    }
                    else
                    {
                        cli_auth_tv.Text = "Failed to login";
                        cli_auth_tv.BackColor = System.Drawing.Color.DarkRed;
                        cli_auth_tv.ForeColor = System.Drawing.Color.White;
                    }
                    if (client.IsUserAuthorized())
                    {

                    }
                    else
                    {
                        /*
                        cli_user_phone_tv.Enabled = false;
                        cli_get_code_btn.Enabled = false;
                        cli_user_code_tv.Enabled = true;
                        cli_login_btn.Enabled = true;
                        */
                    }
                }
            }
            catch (Exception) { }
        }
        private void Clean_Message(object Sender, EventArgs e)
        {
            timerMessage.Enabled = true;
        }
        private void Message_Tick(object Sender, EventArgs e)
        {
            message_box.Text = "...";
            timerMessage.Enabled = false;
        }
    }
}
