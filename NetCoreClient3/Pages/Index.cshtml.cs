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

namespace NetCoreClient3.Pages
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

        [BindProperty]
        public string UserName { get; set; }
        private static string userName { get; set; }
        private static string userMessages { get; set; }

        [BindProperty]
        public string GroupName { get; set; } = "GroupOne";
        private static string groupName { get; set; } = "GroupOne";
        private static string groupMessages { get; set; }


        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            if (connection.State == HubConnectionState.Disconnected)
                await connection.StartAsync();

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                var newMessage = $"{user}: {message}";
                messagesList.Add(newMessage);
            });

            connection.On<string, string>("GroupReceiveMessage", (message, groupName) =>
            {
                var newGroupMessage = $"{groupName}: {message}";
                groupMessagesList.Add(newGroupMessage);
            });
        }

        public async Task OnPost()
        {
            userName = UserName;
            userMessages = Request.Form["Message"];

            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                    await connection.StartAsync();

                await sendMessageAPI();
            }
            catch (Exception ex)
            {
                messagesList.Add(ex.Message);
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
                messagesList.Add(ex.Message);
            }
        }

        public async Task OnPostJoinGroup()
        {
            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                    await connection.StartAsync();

                Debug.WriteLine(connection.ConnectionId + " Joining Group");

                await joinGroup(connection.ConnectionId);
            }
            catch (Exception ex)
            {

            }
        }

        private async Task sendMessageAPI()
        {
            Message message = new Message();

            message.user = userName;
            message.messages = userMessages;

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(message);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(signalrServer + @"/SendMessageToAll", data);
                var result = await response.Content.ReadAsStringAsync();
            }
        }

        private async Task sendMessageToGroupAPI()
        {
            MessageToGroup messageToGroup = new MessageToGroup();

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

        private async Task joinGroup(string connectionId)
        {
            UserSignalrInformation userSignalrInformation = new UserSignalrInformation();

            userSignalrInformation.connectionId = connectionId;
            userSignalrInformation.groupName = groupName;
            userSignalrInformation.userEmail = @"limzhiyong324@yahoo.com";

            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(userSignalrInformation);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(signalrServer + @"/JoinGroup", data);
                var result = await response.Content.ReadAsStringAsync();
            }
        }

        private async Task sendMessage()
        {
            try
            {
                await connection.InvokeAsync("SendMessage", userName, userMessages);
            }
            catch (Exception ex)
            {
                messagesList.Add(ex.Message);
            }
        }
    }

    public class Message
    {
        public string user { get; set; }
        public string messages { get; set; }
    }

    public class MessageToGroup
    {
        public string messages { get; set; }
        public string groupName { get; set; }
    }

    public class UserSignalrInformation
    {
        public string connectionId { get; set; }
        public string userEmail { get; set; }
        public string groupName { get; set; }
    }
}
