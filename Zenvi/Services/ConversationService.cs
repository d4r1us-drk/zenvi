using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zenvi.Data;
using Zenvi.Models;
using Zenvi.Utils;

namespace Zenvi.Services;

public interface IConversationService
{
    Task<Conversation> CreateConversationAsync(ClaimsPrincipal user, string TargetUserUserName, string? description);
    Task DeleteConversationAsync(int id, ClaimsPrincipal user);
    Task<Conversation> GetConversationByIdAsync(int id, ClaimsPrincipal user);
    Task UpdateConversationAsync(int id, ClaimsPrincipal user, string description);
    Task<Message> SendMessageAsync(ClaimsPrincipal user, Message message, List<string>? mediaNames);
    Task<Message> UpdateMessageAsync(int id, ClaimsPrincipal user, Message updatedMessage, List<string>? mediaNames);
    Task DeleteMessageAsync(int id, ClaimsPrincipal user);
    Task<Message> ReplyToMessageAsync(ClaimsPrincipal user, Message message, List<string>? mediaNames, int repliedToId);
    Task<List<Message>> GetMessagesInConversationAsync(int conversationId, ClaimsPrincipal user);
    Task<Message> GetMessageByIdAsync(int id, ClaimsPrincipal user);
    Task<List<Conversation>> GetConversationsForUserAsync(ClaimsPrincipal user);
}

public class ConversationService(ApplicationDbContext context) : IConversationService
{
    private readonly LogHandler _logHandler = new(typeof(ConversationService));

    public async Task<Conversation> CreateConversationAsync(ClaimsPrincipal user, string targetUserUserName, string? description)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }
        
        var user1 = await context.Users.FindAsync(userId);

        var user2 = await context.Users.FindAsync(targetUserUserName);

        if (user1 == null || user2 == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var conversation = new Conversation
        {
            User1 = user1,
            User2 = user2,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            conversation.Description = description;
        }

        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        _logHandler.LogInfo($"Conversation created between {user1.UserName} and {user2.UserName}.");

        return conversation;
    }

    public async Task DeleteConversationAsync(int id, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var conversation = await context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .FirstOrDefaultAsync(c => c.ConversationId == id);

        if (conversation == null)
        {
            _logHandler.LogWarn($"Conversation with id {id} not found.", new KeyNotFoundException($"Conversation with id {id} not found."));
            throw new KeyNotFoundException("Conversation not found");
        }

        if (conversation.User1.Id != userId && conversation.User2.Id != userId)
        {
            _logHandler.LogWarn($"User {userId} not authorized to delete conversation {id}.", new UnauthorizedAccessException($"User {userId} not authorized to delete conversation {id}."));
            throw new UnauthorizedAccessException();
        }

        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync();
        _logHandler.LogInfo($"Conversation with id {id} deleted by user {userId}.");
    }

    public async Task<Conversation> GetConversationByIdAsync(int id, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var conversation = await context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .FirstOrDefaultAsync(c => c.ConversationId == id);

        if (conversation == null)
        {
            _logHandler.LogWarn($"Conversation with id {id} not found.", new KeyNotFoundException($"Conversation with id {id} not found."));
            throw new KeyNotFoundException("Conversation not found");
        }

        if (conversation.User1.Id != userId && conversation.User2.Id != userId)
        {
            _logHandler.LogWarn($"User {userId} not authorized to access conversation {id}.", new UnauthorizedAccessException($"User {userId} not authorized to access conversation {id}."));
            throw new UnauthorizedAccessException();
        }

        _logHandler.LogInfo($"Conversation with id {id} retrieved by user {userId}.");
        return conversation;
    }

    public async Task UpdateConversationAsync(int id, ClaimsPrincipal user, string description)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var conversation = await context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .FirstOrDefaultAsync(c => c.ConversationId == id);

        if (conversation == null)
        {
            _logHandler.LogWarn($"Conversation with id {id} not found.", new KeyNotFoundException($"Conversation with id {id} not found."));
            throw new KeyNotFoundException("Conversation not found");
        }

        if (conversation.User1.Id != userId && conversation.User2.Id != userId)
        {
            _logHandler.LogWarn($"User {userId} not authorized to update conversation {id}.", new UnauthorizedAccessException($"User {userId} not authorized to update conversation {id}."));
            throw new UnauthorizedAccessException();
        }

        conversation.Description = description;
        await context.SaveChangesAsync();
        _logHandler.LogInfo($"Conversation with id {id} updated by user {userId}.");
    }

    public async Task<Message> SendMessageAsync(ClaimsPrincipal user, Message message, List<string>? mediaNames)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var sender = await context.Users.FindAsync(userId);
        var conversation = await context.Conversations.FindAsync(message.ConversationId);

        if (sender == null || conversation == null)
        {
            throw new KeyNotFoundException("User or conversation not found");
        }

        message.SentAt = DateTime.UtcNow;

        if (mediaNames != null && mediaNames.Any())
        {
            var mediaContent = await context.Media.Where(m => mediaNames.Contains(m.Name)).ToListAsync();
            foreach (var media in mediaContent)
            {
                media.MessageId = message.MessageId;
            }
            message.MediaContent = mediaContent;
        }

        context.Messages.Add(message);
        await context.SaveChangesAsync();
        _logHandler.LogInfo($"Message sent in conversation {conversation.ConversationId} by {sender.UserName}.");

        return message;
    }

    public async Task<Message> UpdateMessageAsync(int id, ClaimsPrincipal user, Message updatedMessage, List<string>? mediaNames)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var message = await context.Messages
            .Include(m => m.MediaContent)
            .Include(m => m.Conversation)
            .ThenInclude(c => c.User1)
            .Include(m => m.Conversation)
            .ThenInclude(c => c.User2)
            .FirstOrDefaultAsync(m => m.MessageId == id);

        if (message == null)
        {
            _logHandler.LogWarn($"Message with id {id} not found.", new KeyNotFoundException($"Message with id {id} not found."));
            throw new KeyNotFoundException("Message not found");
        }

        var conversation = message.Conversation;
        if (conversation.User1.Id != userId && conversation.User2.Id != userId)
        {
            _logHandler.LogWarn($"User {userId} not authorized to update message {id}.", new UnauthorizedAccessException($"User {userId} not authorized to update message {id}."));
            throw new UnauthorizedAccessException();
        }

        if (!string.IsNullOrWhiteSpace(updatedMessage.Content))
        {
            message.Content = updatedMessage.Content;
        }

        if (mediaNames != null && mediaNames.Any())
        {
            message.MediaContent?.Clear();
            var mediaContent = await context.Media.Where(m => mediaNames.Contains(m.Name)).ToListAsync();
            foreach (var media in mediaContent)
            {
                media.MessageId = message.MessageId;
            }
            message.MediaContent = mediaContent;
        }

        message.UpdatedAt = DateTime.UtcNow;
        context.Messages.Update(message);
        await context.SaveChangesAsync();
        _logHandler.LogInfo($"Message with id {id} updated by user {userId}.");

        return message;
    }

    public async Task DeleteMessageAsync(int id, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var message = await context.Messages
            .Include(m => m.Conversation).ThenInclude(conversation => conversation.User1)
            .Include(message => message.Conversation).ThenInclude(conversation => conversation.User2)
            .FirstOrDefaultAsync(m => m.MessageId == id);

        if (message == null)
        {
            _logHandler.LogWarn($"Message with id {id} not found.", new KeyNotFoundException($"Message with id {id} not found."));
            throw new KeyNotFoundException("Message not found");
        }

        var conversation = message.Conversation;
        if (conversation.User1.Id != userId && conversation.User2.Id != userId)
        {
            _logHandler.LogWarn($"User {userId} not authorized to delete message {id}.", new UnauthorizedAccessException($"User {userId} not authorized to delete message {id}."));
            throw new UnauthorizedAccessException();
        }

        context.Messages.Remove(message);
        await context.SaveChangesAsync();
        _logHandler.LogInfo($"Message with id {id} deleted in conversation {conversation.ConversationId} by user {userId}.");
    }

    public async Task<Message> ReplyToMessageAsync(ClaimsPrincipal user, Message message, List<string>? mediaNames, int repliedToId)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var conversation = await context.Conversations.FindAsync(message.ConversationId);

        if (conversation == null)
        {
            _logHandler.LogWarn($"Conversation with id {message.ConversationId} not found.", new KeyNotFoundException($"Conversation with id {message.ConversationId} not found."));
            throw new KeyNotFoundException("Conversation not found");
        }

        message.RepliedToId = repliedToId;
        message.SentAt = DateTime.UtcNow;

        if (mediaNames != null && mediaNames.Any())
        {
            var mediaContent = await context.Media.Where(m => mediaNames.Contains(m.Name)).ToListAsync();
            foreach (var media in mediaContent)
            {
                media.MessageId = message.MessageId;
            }
            message.MediaContent = mediaContent;
        }

        context.Messages.Add(message);
        await context.SaveChangesAsync();
        _logHandler.LogInfo($"Message with id {message.MessageId} replied to in conversation {conversation.ConversationId} by user {userId}.");

        return message;
    }

    public async Task<List<Message>> GetMessagesInConversationAsync(int conversationId, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var conversation = await context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
        {
            _logHandler.LogWarn($"Conversation with id {conversationId} not found.", new KeyNotFoundException($"Conversation with id {conversationId} not found."));
            throw new KeyNotFoundException("Conversation not found");
        }

        if (conversation.User1.Id != userId && conversation.User2.Id != userId)
        {
            _logHandler.LogWarn($"User {userId} not authorized to access conversation {conversationId}.", new UnauthorizedAccessException($"User {userId} not authorized to access conversation {conversationId}."));
            throw new UnauthorizedAccessException();
        }

        var messages = await context.Messages
            .Where(m => m.Conversation.ConversationId == conversationId)
            .Include(m => m.MediaContent)
            .ToListAsync();

        _logHandler.LogInfo($"Messages retrieved for conversation {conversationId} by user {userId}.");
        return messages;
    }

    public async Task<Message> GetMessageByIdAsync(int id, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var message = await context.Messages
            .Include(m => m.MediaContent)
            .Include(m => m.Conversation)
            .ThenInclude(c => c.User1)
            .Include(m => m.Conversation)
            .ThenInclude(c => c.User2)
            .FirstOrDefaultAsync(m => m.MessageId == id);

        if (message == null)
        {
            _logHandler.LogWarn($"Message with id {id} not found.", new KeyNotFoundException($"Message with id {id} not found."));
            throw new KeyNotFoundException("Message not found");
        }

        var conversation = message.Conversation;
        if (conversation.User1.Id != userId && conversation.User2.Id != userId)
        {
            _logHandler.LogWarn($"User {userId} not authorized to access message {id}.", new UnauthorizedAccessException($"User {userId} not authorized to access message {id}."));
            throw new UnauthorizedAccessException();
        }

        _logHandler.LogInfo($"Message with id {id} retrieved by user {userId}.");
        return message;
    }

    public async Task<List<Conversation>> GetConversationsForUserAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            throw new UnauthorizedAccessException();
        }

        var conversations = await context.Conversations
            .Where(c => c.User1.Id == userId || c.User2.Id == userId)
            .Include(c => c.User1)
            .Include(c => c.User2)
            .ToListAsync();

        if (conversations.Count == 0)
        {
            _logHandler.LogWarn($"No conversations found for user {userId}.", new KeyNotFoundException("No conversations found."));
            throw new KeyNotFoundException("No conversations found");
        }

        _logHandler.LogInfo($"Conversations retrieved for user {userId}.");
        return conversations;
    }
}
