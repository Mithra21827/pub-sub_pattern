
using Subscriber.Dto;
using System.Net.Http.Json;
using System.Security.Claims;


Console.WriteLine("Press ESC to stop");
do{ 
    HttpClient client = new HttpClient();
    Console.WriteLine("Listing...");
    while (!Console.KeyAvailable) {
        List<int> ackId = await GetMessagesAsync(client);  
        Thread.Sleep(2000);

        if (ackId.Count > 0) {
            await AckMessageAsync(client, ackId);   
        }
    }
}while (Console.ReadKey(true).Key != ConsoleKey.Escape);

static async Task<List<int>> GetMessagesAsync(HttpClient httpClient) 
{ 
    List<int> ackIds = new List<int>();
    List<MessageReadDto>? newMessages = new List<MessageReadDto>();

    try {
        newMessages = await httpClient.GetFromJsonAsync<List<MessageReadDto>>("https://localhost:7258/api/subscriptions/2/messages");
    }
    catch {
        return ackIds;
    }

    foreach (MessageReadDto msg in newMessages)
    {
        Console.WriteLine($"{msg.Id} - {msg.TopicMessage} - {msg.MessageStatus}"); 
        ackIds.Add(msg.Id);
    }
    return ackIds;
}

static async Task AckMessageAsync(HttpClient httpClient, List<int> ackId) {
    var response = await httpClient.PostAsJsonAsync("https://localhost:7258/api/subscriptions/2/messages", ackId);
    var returnMessage = await response.Content.ReadAsStringAsync();

    Console.WriteLine(returnMessage);
}