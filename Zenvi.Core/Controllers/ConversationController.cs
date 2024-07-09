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
            var createdConversation = await conversationService.CreateConversationAsync(User, conversationDto.User2UserName, conversationDto.Description);
            return Ok(CreatedAtAction(nameof(GetConversationById), new { id = createdConversation.ConversationId }, MapToDto(createdConversation)));
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
            return Ok();
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
            return Ok(MapToDto(conversation));
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
            var updatedConversation = await conversationService.GetConversationByIdAsync(id, User);
            return Ok(CreatedAtAction(nameof(GetConversationById), new { id = updatedConversation.ConversationId }, MapToDto(updatedConversation)));
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
            return Ok(CreatedAtAction(nameof(GetMessageById), new { id = createdMessage.MessageId }, MapToDto(createdMessage)));
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
            var updatedMessage = new Message { Content = messageDto.Content };
            await conversationService.UpdateMessageAsync(id, User, updatedMessage, messageDto.MediaNames);
            var updatedMsg = await conversationService.GetMessageByIdAsync(id, User);
            return Ok(CreatedAtAction(nameof(GetConversationById), new { id = updatedMsg.ConversationId }, MapToDto(updatedMsg)));
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
            return Ok();
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
            return Ok(CreatedAtAction(nameof(GetMessageById), new { id = createdMessage.MessageId }, MapToDto(createdMessage)));
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
            var messageDtos = messages.Select(MapToDto).ToList();

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
            return Ok(MapToDto(message));
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
            var conversationDtos = conversations.Select(MapToDto).ToList();

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

    private static ConversationDto MapToDto(Conversation conversation)
    {
        return new ConversationDto
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
    }

    private static MessageDto MapToDto(Message message)
    {
        return new MessageDto
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
    }
}
