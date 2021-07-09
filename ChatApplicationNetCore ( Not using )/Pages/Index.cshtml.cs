using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ChatApplicationNetCore.Pages
{
    public class IndexModel : PageModel
    {
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
       
        private static string userName { get; set; }
        private static string userMessages { get; set; }


        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

            //connection.Closed += async (error) =>
            //{
            //    await Task.Delay(new Random().Next(0, 5) * 1000);
            //    await connection.StartAsync();
            //};
        }

        public void OnGet()
        {
            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                var newMessage = $"{user}: {message}";
                messagesList.Add(newMessage);
            });
        }

        public async Task OnPost()
        {
            userName = Request.Form["UserName"];
            userMessages = Request.Form["Message"];

            Debug.WriteLine("Connection state post: " + connection.State);

            try
            {
                if(connection.State == HubConnectionState.Disconnected)
                    await connection.StartAsync();

                await sendMessageAPI();
                //await sendMessage();

                //await _connection.StopAsync();
            }
            catch (Exception ex)
            {
                messagesList.Add(ex.Message);
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

                var response = await client.PostAsync($@"https://localhost:44364/Chat/SendMessageToAll", data);
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
}
