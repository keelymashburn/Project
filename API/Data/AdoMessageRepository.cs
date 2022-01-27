using API.DTOs;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API.Data
{
    public class AdoMessageRepository : IMessageRepository
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public AdoMessageRepository(IMapper mapper, IConfiguration configuration, DataContext context)
        {
            _mapper = mapper;
            _context = context;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void AddGroup(Group group)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.AddGroup";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Name", group.Name);
            command.Parameters.AddWithValue("@ConnectionId", new Guid());

            command.ExecuteNonQueryAsync();
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetConnection";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ConnectionId", connectionId);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                return new Connection
                {
                    ConnectionId = reader.GetString("ConnectionId"),
                    Username = reader.GetString("Username")
                };
            }

            return null;
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetGroupForConnection";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Id", connectionId);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                return new Group
                {
                     Name = reader.GetString("Name"),
                     Connections = new List<Connection>
                     {
                        new Connection
                        {
                            ConnectionId = reader.GetString("ConnectionId"),
                            Username = reader.GetString("Username")
                        }
                     }
                };
            }

            return null;
        }

        public async Task<Message> GetMessage(int id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetMessage";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                return new Message
                {
                    Id = reader.GetInt32("Id"),
                    SenderId = reader.GetInt32("SenderId"),
                    SenderUsername = reader.GetString("SenderUsername"),
                    RecipientId = reader.GetInt32("RecipientId"),
                    RecipientUsername = reader.GetString("RecipientUsername"),
                    Content = reader.GetString("Content"),
                    DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
                    MessageSent = reader.GetDateTime("MessageSent"),
                    SenderDeleted = reader.GetBoolean("SenderDeleted"),
                    RecipientDeleted = reader.GetBoolean("RecipientDeleted")
                };
            }

            return null;
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetMessageGroup";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@GroupName", groupName);

            using var reader = await command.ExecuteReaderAsync();

            while (reader.Read())
            {
                return new Group
                {
                    Name = reader.GetString("Name"),
                    Connections = new List<Connection>
                     {
                        new Connection
                        {
                            ConnectionId = reader.GetString("ConnectionId"),
                            Username = reader.GetString("Username")
                        }
                     }
                };
            }

            return null;
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CurrentUsername", messageParams.Username);
            command.CommandText = "dbo.GetMessagesForUser";

            if(messageParams.Container == "Inbox")
            {
                command.CommandText = "dbo.GetMessagesForUser";
            }
            else if(messageParams.Container == "Outbox")
            {
                command.CommandText = "dbo.GetMessagesForUser1";
            }
            else if (messageParams.Container == "Unread")
            {
                command.CommandText = "dbo.GetMessagesForUser2";
            }

            using var reader = await command.ExecuteReaderAsync();

            var result = new List<MessageDto>();
            while (reader.Read())
            {
                result.Add(
                    new MessageDto
                    {
                        Id = reader.GetInt32("Id"),
                        SenderId = reader.GetInt32("SenderId"),
                        SenderUsername = reader.GetString("SenderUsername"),
                        SenderPhotoUrl = reader.GetString("SenderPhoto"),
                        RecipientId = reader.GetInt32("RecipientId"),
                        RecipientUsername = reader.GetString("RecipientUsername"),
                        RecipientPhotoUrl = reader.GetString("RecipientPhoto"),
                        Content = reader.GetString("Content"),
                        DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
                        MessageSent = reader.GetDateTime("MessageSent"),
                        SenderDeleted = reader.GetBoolean("SenderDeleted"),
                        RecipientDeleted = reader.GetBoolean("RecipientDeleted")
                    });
            }

            var pagedlist = new PagedList<MessageDto>(result, result.Count(), messageParams.PageNumber, messageParams.PageSize);

            if (pagedlist != null) return pagedlist;

            return null;
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetMessageThread";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@CurrentUsername", currentUsername);
            command.Parameters.AddWithValue("@RecipientUsername", recipientUsername);

            using var reader = await command.ExecuteReaderAsync();

            var result = new List<MessageDto>();
            while (reader.Read())
            { 
                result.Add(
                    new MessageDto
                    {
                        Id = reader.GetInt32("Id"),
                        SenderId = reader.GetInt32("SenderId"),
                        SenderUsername = reader.GetString("SenderUsername"),
                        SenderPhotoUrl = reader.GetString("SenderPhoto"),
                        RecipientId = reader.GetInt32("RecipientId"),
                        RecipientUsername = reader.GetString("RecipientUsername"),
                        RecipientPhotoUrl = reader.GetString("RecipientPhoto"),
                        Content = reader.GetString("Content"),
                        DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
                        MessageSent = reader.GetDateTime("MessageSent"),
                        SenderDeleted = reader.GetBoolean("SenderDeleted"),
                        RecipientDeleted = reader.GetBoolean("RecipientDeleted")
                    });
            }

            return result;
        }

        public void RemoveConnection(Connection Appconnection)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.RemoveConnection";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ConnectionId", Appconnection.ConnectionId);

            command.ExecuteNonQueryAsync();
        }

        public async void AddMessage(Message message) 
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.AddMessage";
            command.CommandType = CommandType.StoredProcedure;
            //message params
            command.Parameters.AddWithValue("@SenderId", message.SenderId);
            command.Parameters.AddWithValue("@SenderUsername", message.SenderUsername);
            command.Parameters.AddWithValue("@RecipientId", message.RecipientId);
            command.Parameters.AddWithValue("@RecipientUsername", message.RecipientUsername);
            command.Parameters.AddWithValue("@Content", message.Content);
            if (message.DateRead != null)
            {
                command.Parameters.AddWithValue("@DateRead", message.DateRead);
            }
            else
            {
                command.Parameters.AddWithValue("@DateRead", DBNull.Value);
            }
            command.Parameters.AddWithValue("@MessageSent", message.MessageSent);
            command.Parameters.AddWithValue("@SenderDeleted", message.SenderDeleted);
            command.Parameters.AddWithValue("@RecipientDeleted", message.RecipientDeleted);

            await command.ExecuteNonQueryAsync();
        }

        public void DeleteMessage(Message message) 
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.RemoveMessage";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Id", message.Id);

            command.ExecuteNonQueryAsync();
        }
    }
}
