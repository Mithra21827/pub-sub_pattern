using MESSGEBROKER.Data;
using MESSGEBROKER.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=MessageBroker.db"));

var app = builder.Build();



app.UseHttpsRedirection();


//Create Topic
app.MapPost("api/topics", async (AppDbContext context, Topic topic) =>
{
    await context.Topics.AddAsync(topic);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{topic.Id}", topic);
});

//Return All Topic

app.MapGet("api/topics", async(AppDbContext context) =>{ 
    var topics = await context.Topics.ToListAsync();

    return Results.Ok(topics);
});

//Publish message

app.MapPost("api/topics/{id}/messages", async (AppDbContext context, int id, Message message) =>
{
    bool topics = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topics) {
        return Results.NotFound("Topic not found");
    }

    var subs =  context.Subscriptions.Where(s => s.TopicId == id);

    if (subs.Count() == 0) {
        return Results.NotFound("There is no subscriptions for this topic");
    }

    foreach (var sub in subs)
    {
        Message msg = new Message()
        {
            TopicMessage = message.TopicMessage,
            SubsrciptionId = sub.Id,
            ExpiresAfter = message.ExpiresAfter,
            MessageStatus = message.MessageStatus,
        };

        await context.Messages.AddAsync(msg);
    }
    await context.SaveChangesAsync();

    return Results.Ok("Message has been published");
});

//Create subsrciption

app.MapPost("api/topics/{id}/subscriptions", async (AppDbContext context, int id, Subscription sub) =>
{
    bool topics = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topics)
    {
        return Results.NotFound("Topic not found");
    }

    sub.TopicId = id;

    await context.Subscriptions.AddAsync(sub);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{id}/subscriptions/{sub.Id}",sub);

});

//Get Subscriber Message
app.MapGet("api/subscriptions/{id}/messages", async (AppDbContext context, int id) =>
{
    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subs)
    {
        return Results.NotFound("Subscription not found");
    }

    var messages = context.Messages.Where(m => m.SubsrciptionId == id && m.MessageStatus != "SENT");
    if (messages.Count() == 0)
    {
        return Results.NotFound("No New Message");
    }

    foreach (var msg in messages)
    {
        msg.MessageStatus = "REQUESTED";
    }
    await context.SaveChangesAsync();
    return Results.Ok(messages);
});

//ACK messages for subscriber

app.MapPost("api/subscriptions/{id}/messages", async (AppDbContext context, int id, int[] confs) => {

    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);
    if (!subs)
    {
        return Results.NotFound("Subscription not found");
    }

    if (confs.Length <= 0)
    {
        return Results.BadRequest();
    }

    int count = 0;

    foreach (int i in confs)
    {
        var msg = context.Messages.FirstOrDefault(m=> m.Id == i );

        if (msg != null) {
            msg.MessageStatus = "SENT";
            await context.SaveChangesAsync();   
            count++;
        }
    }
    return Results.Ok($"Acknowledged {count}/{confs.Length} messages");
});

app.Run();
