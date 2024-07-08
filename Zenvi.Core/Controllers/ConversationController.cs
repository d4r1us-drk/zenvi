using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvi.Core.Data.Entities;
using Zenvi.Core.Services;
using Zenvi.Shared;

namespace Zenvi.Core.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConversationsController(IConversationService conversationService) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto conversationDto)
    {
        try
        {
            var conversation = new Conversation
            {
                User2 = new User { Id = conversationDto.User2Id },
                Description = conversationDto.Description
            };
            var createdConversation = await conversationService.CreateConversationAsync(User, conversation);
            return CreatedAtAction(nameof(GetConversationById), new { id = createdConversation.ConversationId }, createdConversation);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        try
        {
            await conversationService.DeleteConversationAsync(id, User);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("get/{id:int}")]
    public async Task<IActionResult> GetConversationById(int id)
    {
        try
        {
            var conversation = await conversationService.GetConversationByIdAsync(id, User);
            var conversationDto = new ConversationDto
            {
                ConversationId = conversation.ConversationId,
                User1UserName = conversation.User1.UserName,
                User2UserName = conversation.User2.UserName,
                Description = conversation.Description,
                CreatedAt = conversation.CreatedAt,
                Messages = conversation.Messages.Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    ConversationId = m.ConversationId,
                    Content = m.Content,
                    MediaNames = m.MediaContent?.Select(media => media.Name).ToList(),
                    SentAt = m.SentAt,
                    ReadAt = m.ReadAt,
                    UpdatedAt = m.UpdatedAt,
                    RepliedToId = m.RepliedToId
                }).ToList()
            };

            return Ok(conversationDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("update/{id:int}")]
    public async Task<IActionResult> UpdateConversation(int id, [FromBody] UpdateConversationDto conversationDto)
    {
        try
        {
            await conversationService.UpdateConversationAsync(id, User, conversationDto.Description);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto messageDto)
    {
        try
        {
            var message = new Message
            {
                ConversationId = messageDto.ConversationId,
                Content = messageDto.Content
            };
            var createdMessage = await conversationService.SendMessageAsync(User, message, messageDto.MediaNames);
            return CreatedAtAction(nameof(GetMessageById), new { id = createdMessage.MessageId }, createdMessage);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("updateMessage/{id:int}")]
    public async Task<IActionResult> UpdateMessage(int id, [FromBody] UpdateMessageDto messageDto)
    {
        try
        {
            var updatedMessage = new Message
            {
                Content = messageDto.Content
            };
            await conversationService.UpdateMessageAsync(id, User, updatedMessage, messageDto.MediaNames);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("deleteMessage/{id:int}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        try
        {
            await conversationService.DeleteMessageAsync(id, User);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("reply")]
    public async Task<IActionResult> ReplyToMessage([FromBody] ReplyMessageDto replyMessageDto)
    {
        try
        {
            var message = new Message
            {
                ConversationId = replyMessageDto.ConversationId,
                Content = replyMessageDto.Content,
                RepliedToId = replyMessageDto.RepliedToId
            };
            var createdMessage = await conversationService.ReplyToMessageAsync(User, message, replyMessageDto.MediaNames, replyMessageDto.RepliedToId);
            return CreatedAtAction(nameof(GetMessageById), new { id = createdMessage.MessageId }, createdMessage);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("getMessages/{conversationId:int}")]
    public async Task<IActionResult> GetMessagesInConversation(int conversationId)
    {
        try
        {
            var messages = await conversationService.GetMessagesInConversationAsync(conversationId, User);
            var messageDtos = messages.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                ConversationId = m.ConversationId,
                Content = m.Content,
                MediaNames = m.MediaContent?.Select(media => media.Name).ToList(),
                SentAt = m.SentAt,
                ReadAt = m.ReadAt,
                UpdatedAt = m.UpdatedAt,
                RepliedToId = m.RepliedToId
            }).ToList();

            return Ok(messageDtos);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("getMessage/{id:int}")]
    public async Task<IActionResult> GetMessageById(int id)
    {
        try
        {
            var message = await conversationService.GetMessageByIdAsync(id, User);
            var messageDto = new MessageDto
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                Content = message.Content,
                MediaNames = message.MediaContent?.Select(m => m.Name).ToList(),
                SentAt = message.SentAt,
                ReadAt = message.ReadAt,
                UpdatedAt = message.UpdatedAt,
                RepliedToId = message.RepliedToId
            };

            return Ok(messageDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("getUserConversations")]
    public async Task<IActionResult> GetConversationsForUser()
    {
        try
        {
            var conversations = await conversationService.GetConversationsForUserAsync(User);
            var conversationDtos = conversations.Select(c => new ConversationDto
            {
                ConversationId = c.ConversationId,
                User1UserName = c.User1.UserName,
                User2UserName = c.User2.UserName,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                Messages = c.Messages.Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    ConversationId = m.ConversationId,
                    Content = m.Content,
                    MediaNames = m.MediaContent?.Select(media => media.Name).ToList(),
                    SentAt = m.SentAt,
                    ReadAt = m.ReadAt,
                    UpdatedAt = m.UpdatedAt,
                    RepliedToId = m.RepliedToId
                }).ToList()
            }).ToList();

            return Ok(conversationDtos);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}