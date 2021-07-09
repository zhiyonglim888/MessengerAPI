using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApplicationNetCore.Hubs
{
    public class ChatHub : Hub
    {
        public Task SendMessage(string user, string message)
        {
            return Clients.All.SendAsync("ReceiveMessage", user, message);
        }


        //public async Task Send(object sender, string message)
        //{
        //    // Broadcast the message to all clients except the sender
        //    await Clients.Others.SendAsync("broadcastMessage", sender, message);
        //}

        //public async Task SendTyping(object sender)
        //{
        //    // Broadcast the typing notification to all clients except the sender
        //    await Clients.Others.SendAsync("typing", sender);
        //}


        //public Task CreateNewGroup(string connectionId, string groupName)
        //{
        //    return Groups.AddToGroupAsync(connectionId, groupName);
        //}

        //public Task AddUserToGroup(string connectionId, string groupName)
        //{
        //    return Groups.AddToGroupAsync(connectionId, groupName);
        //}
    }
}
