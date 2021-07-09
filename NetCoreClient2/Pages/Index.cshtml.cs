using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreClient2.Pages
{
    public class IndexModel : PageModel
    {
        private readonly string signalrServer = @"https://localhost:44364/Chat";

        private static List<string> _messagesList;
        public List<string> messagesList
        {
            get
            {
                if (_messagesList == null)
                    _messagesList = new List<string>();

                return _messagesList;
            }
        }

        private static List<string> _groupMessagesList;
        public List<string> groupMessagesList
        {
            get
            {
                if (_groupMessagesList == null)
                    _groupMessagesList = new List<string>();

                return _groupMessagesList;
            }
        }

        private static HubConnection _connection;
        public HubConnection connection
        {
            get
            {
                if (_connection == null)
                    _connection = new HubConnectionBuilder()
                                .WithUrl("https://localhost:44364/ChatHub")
                                .WithAutomaticReconnect()
                                .Build();

                return _connection;
            }
        }

        private readonly ILogger<IndexModel> _logger;
        private static string _signalrConnectionId;
        public string signalrConnectionId
        {
            get
            {
                return _signalrConnectionId;
            }
        }

        [BindProperty]
        public string Sender { get; set; } = "Whale";
        [BindProperty]
        public string ReceiverId { get; set; }
        private static string userMessages { get; set; }

        [BindProperty]
        public string GroupName { get; set; }
        private static string groupName { get; set; }
        private static string groupMessages { get; set; }

        private string MOCK_USERID { get; set; } = "ID_33333";

        private static bool isSignalrEventRegistered { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

            //connection.Closed += async (error) =>
            //{
            //    await Task.Delay(new Random().Next(0, 5) * 1000);
            //    await connection.StartAsync();
            //};

            //var task = Task.Run(async () => { await InitializeConnection(); });
            //task.Wait();

        }

        public async Task InitializeConnection()
        {
            if (connection.State == HubConnectionState.Disconnected)
                await connection.StartAsync();

            var newMessage = "";
            var newGroupMessage = "";

            connection.On<MessageToGroup>("GroupReceiveMessage", (MessageToGroup) =>
            {
                newGroupMessage = $"({MessageToGroup.messageDate}) {MessageToGroup.groupName} {MessageToGroup.sender}: {MessageToGroup.messages}";
                groupMessagesList.Add(newGroupMessage);

                Debug.WriteLine("[Client 2] " + newGroupMessage);
            });

            connection.On<Message>("ReceiveMessage", (message) =>
            {
                newMessage = $"({message.messageDate}) {message.sender}: {message.messages}";
                messagesList.Add(newMessage);

                Debug.WriteLine("[Client 2] " + newMessage);
            });

            isSignalrEventRegistered = true;
        }

        public async Task OnGet()
        {
            if (isSignalrEventRegistered != true)
                await InitializeConnection();

            await UpdateUserStatusToOnline();
            //messagesList.AddRange(await GetAllMessageHistory());

            _signalrConnectionId = _connection.ConnectionId;
            groupName = GroupName;
        }

        public async Task OnPostMessage()
        {
            userMessages = Request.Form["Message"];

            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                    await connection.StartAsync();

                await sendClientSpecificMessageAPI();
                //await sendMessageAPI();
            }
            catch (Exception ex)
            {

            }
        }

        public async Task OnPostGroupMessage()
        {
            groupName = GroupName;
            groupMessages = Request.Form["GroupMessage"];

            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                    await connection.StartAsync();

                await sendMessageToGroupAPI();
            }
            catch (Exception ex)
            {

            }
        }

        public async Task OnPostJoinGroup()
        {
            var groupName = Request.Form["nameOfGroup"];

            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                    await connection.StartAsync();

                await joinGroup(groupName, connection.ConnectionId);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task OnPostExitGroup()
        {
            var groupName = Request.Form["nameOfGroup"];

            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                    await connection.StartAsync();

                await exitGroup(groupName, connection.ConnectionId);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task OnPostCreateGroup()
        {

        }

        public async Task OnPostNewConnection()
        {
            await _connection.StopAsync();

            _messagesList.Clear();
            _groupMessagesList.Clear();

            _connection = new HubConnectionBuilder()
                                .WithUrl("https://localhost:44364/ChatHub")
                                .WithAutomaticReconnect()
                                .Build();

            if (connection.State == HubConnectionState.Disconnected)
                await connection.StartAsync();

            await InitializeConnection();
            await UpdateUserStatusToOnline();

            _signalrConnectionId = _connection.ConnectionId;
        }

        public async Task UpdateUserStatusToOnline()
        {
            UserSignalrInformation userSignalrInformation = new UserSignalrInformation();

            userSignalrInformation.connectionId = _connection.ConnectionId;
            userSignalrInformation.userId = MOCK_USERID;
            userSignalrInformation.userName = Sender;

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(userSignalrInformation);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(signalrServer + @"/UpdateUsersHubConnection", data);
                var result = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<List<Message>> GetAllMessageHistory()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(signalrServer + @"/GetAllMessageHistory");
                var result = await response.Content.ReadAsStringAsync();

                var allMessages = JsonConvert.DeserializeObject<List<Message>>(result);

                return allMessages;
            }
        }

        private async Task sendClientSpecificMessageAPI()
        {
            Message message = new Message();

            message.sender = Sender;
            message.senderId = MOCK_USERID;
            message.receiverId = ReceiverId;
            message.messages = userMessages;

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(message);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(signalrServer + @"/SendClientSpecificMessage", data);
                var result = await response.Content.ReadAsStringAsync();
            }
        }

        private async Task sendMessageToGroupAPI()
        {
            MessageToGroup messageToGroup = new MessageToGroup();

            messageToGroup.sender = Sender;
            messageToGroup.senderId = MOCK_USERID;
            messageToGroup.groupName = groupName;
            messageToGroup.messages = groupMessages;

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(messageToGroup);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(signalrServer + @"/SendMessageToGroup", data);
                var result = await response.Content.ReadAsStringAsync();
            }
        }

        private async Task joinGroup(string joinGroupName, string connectionId)
        {
            UserSignalrInformation userSignalrInformation = new UserSignalrInformation();

            userSignalrInformation.connectionId = connectionId;
            userSignalrInformation.groupName = joinGroupName;
            userSignalrInformation.userId = MOCK_USERID;

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(userSignalrInformation);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(signalrServer + @"/JoinGroup", data);
                var result = await response.Content.ReadAsStringAsync();
            }
        }

        private async Task exitGroup(string groupName, string connectionId)
        {
            UserSignalrInformation userSignalrInformation = new UserSignalrInformation();

            userSignalrInformation.connectionId = connectionId;
            userSignalrInformation.groupName = groupName;
            userSignalrInformation.userId = MOCK_USERID;

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(userSignalrInformation);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(signalrServer + @"/ExitGroup", data);
                var result = await response.Content.ReadAsStringAsync();
            }
        }

        //private async Task sendMessage()
        //{
        //    try
        //    {
        //        await connection.InvokeAsync("SendMessage", userName, userMessages);
        //    }
        //    catch (Exception ex)
        //    {
        //        messagesList.Add(ex.Message);
        //    }
        //}
    }

    public class Message
    {
        public DateTime messageDate { get; set; } = DateTime.Now;
        public string sender { get; set; }
        public string senderId { get; set; }
        public string receiverId { get; set; }
        public string messages { get; set; }
    }

    public class MessageToGroup
    {
        public DateTime messageDate { get; set; } = DateTime.Now;
        public string sender { get; set; }
        public string senderId { get; set; }
        public string messages { get; set; }
        public string groupName { get; set; }
    }

    public class UserSignalrInformation
    {
        public string connectionId { get; set; }
        public string userId { get; set; }
        public string userName { get; set; }
        public string groupName { get; set; }
    }
}
