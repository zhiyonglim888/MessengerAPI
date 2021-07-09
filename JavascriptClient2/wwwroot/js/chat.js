﻿document.addEventListener("DOMContentLoaded", () => {

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:44364/ChatHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveMessage", (user, message) => {
        const li = document.createElement("li");
        li.textContent = `${user}: ${message}`;
        document.getElementById("messageList").appendChild(li);
    });


    document.getElementById("send").addEventListener("click", async () => {
        const user = document.getElementById("user").value;
        const message = document.getElementById("message").value;


        try {
            await connection.invoke("SendMessage", user, message);
        } catch (err) {
            console.error(err);
        }

    });

    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.log(err);
            setTimeout(start, 5000);
        }
    };

    connection.onclose(start);

    start();
});