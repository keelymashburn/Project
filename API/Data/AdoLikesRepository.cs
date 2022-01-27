using API.DTOs;
using API.Helpers;
using API.Extensions;
using API.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc;

namespace API.Data
{
    public class AdoLikesRepository : ILikesRepository
    {
        private readonly IConfiguration _configuration;
        public AdoLikesRepository(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUserLike";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@SourceId", sourceUserId);
            command.Parameters.AddWithValue("@LikedId", likedUserId);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                return new UserLike()
                {
                    SourceUserId = reader.GetInt32("SourceUserId"),
                    LikedUserId = reader.GetInt32("LikedUserId")
                };
            }

            return null;
        }

        public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;

            if(likesParams.predicate == "liked")
            {
                command.CommandText = "dbo.GetUserLikes";
                command.Parameters.AddWithValue("@SourceId", likesParams.UserId);
            }

            if(likesParams.predicate == "likedBy")
            {
                command.CommandText = "dbo.GetUserLikes1";
                command.Parameters.AddWithValue("@LikedUser", likesParams.UserId);
            }

            using var reader = await command.ExecuteReaderAsync();
            var result = new List<LikeDto>();
            while (reader.Read())
            {
                result.Add(new LikeDto()
                {
                    Id  = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Age =  reader.GetDateTime("DateOfBirth").CalculateAge(),
                    KnownAs = reader.GetString("KnownAs"),
                    PhotoUrl = reader.GetString("Url"),
                    City = reader.GetString("City")
                });
            }

            var pagedlist = new PagedList<LikeDto>(result, result.Count(), likesParams.PageNumber, likesParams.PageSize);

            if(pagedlist != null) return pagedlist;

            return null;

        }

        public async Task<AppUser> GetUserWithLikes(int userId)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUserWithLikes";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@id", userId);

            //props
            var appUser = new AppUser();
            var photos = new List<Photo>();
            var likedByUsers = new List<UserLike>();
            var likedUsers = new List<UserLike>();
            var messagesReceived = new List<Message>();
            var messagesSent = new List<Message>();
            var userRoles = new List<AppUserRole>();

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                appUser.Id = reader.GetInt32("Id");
                appUser.DateOfBirth = reader.GetDateTime("DateOfBirth");
                appUser.UserName = reader.GetString("UserName");
                appUser.KnownAs = reader.GetString("KnownAs");
                appUser.Created = reader.GetDateTime("Created");
                appUser.LastActive = reader.GetDateTime("LastActive");
                appUser.Gender = reader.GetString("Gender");
                appUser.Introduction = reader.GetString("Introduction");
                appUser.LookingFor = reader.GetString("LookingFor");
                appUser.Interests = reader.GetString("Interests");
                appUser.City = reader.GetString("City");
                appUser.Country = reader.GetString("Country");
            }

            reader.NextResult();

            while (reader.Read())
            {
                photos.Add(
                    new Photo
                    {
                        Id = reader.GetInt32("PhotoID"),
                        Url = reader.GetString("Url"),
                        IsApproved = reader.GetBoolean("IsApproved"),
                        IsMain = reader.GetBoolean("IsMain"),
                        AppUserId = reader.GetInt32("AppUserId")
                    });
            }

            reader.NextResult();

            while(reader.Read())
            {
                likedByUsers.Add(
                    new UserLike
                    {
                        SourceUserId = reader.GetInt32("SourceUserId"),
                        LikedUserId = reader.GetInt32("LikedUserId")
                    });
            }

            reader.NextResult();

            while (reader.Read())
            {
                likedUsers.Add(
                    new UserLike
                    {
                        SourceUserId = reader.GetInt32("SourceUserId"),
                        LikedUserId = reader.GetInt32("LikedUserId")
                    });
            }

            reader.NextResult();

            while (reader.Read())
            {
                messagesReceived.Add(
                    new Message
                    {
                        Id = reader.GetInt32("Id"),
                        SenderId = reader.GetInt32("SenderId"),
                        SenderUsername = reader.GetString("SenderUsername"),
                        RecipientId = reader.GetInt32("RecipientId"),
                        RecipientUsername = reader.GetString("RecipientUsername"),
                        Content = reader.GetString("Content"),
                        DateRead = reader.GetDateTime("DateRead"),
                        MessageSent = reader.GetDateTime("MessageSent"),
                        SenderDeleted = reader.GetBoolean("SenderDeleted"),
                        RecipientDeleted = reader.GetBoolean("RecipientDeleted")
                    });
            }

            reader.NextResult();

            while (reader.Read())
            {
                messagesReceived.Add(
                new Message
                {
                    Id = reader.GetInt32("Id"),
                    SenderId = reader.GetInt32("SenderId"),
                    SenderUsername = reader.GetString("SenderUsername"),
                    RecipientId = reader.GetInt32("RecipientId"),
                    RecipientUsername = reader.GetString("RecipientUsername"),
                    Content = reader.GetString("Content"),
                    DateRead = reader.GetDateTime("DateRead"),
                    MessageSent = reader.GetDateTime("MessageSent"),
                    SenderDeleted = reader.GetBoolean("SenderDeleted"),
                    RecipientDeleted = reader.GetBoolean("RecipientDeleted")
                });

            }

            foreach (Photo photo in photos)
            {
                photo.AppUser = appUser;
            }

            foreach (UserLike user in likedByUsers)
            {
                user.LikedUser = appUser;
            }

            foreach (UserLike user in likedUsers)
            {
                user.SourceUser = appUser;
            }

            foreach (Message message in messagesReceived)
            {
                message.Recipient = appUser;
            }

            foreach (Message message in messagesSent)
            {
                message.Sender = appUser;
            }

            appUser.Photos = photos;
            appUser.LikedByUsers = likedByUsers;
            appUser.LikedUsers = likedUsers;
            appUser.MessagesReceived = messagesReceived;
            appUser.MessagesSent = messagesSent;
            

            if(appUser != null) return appUser;

            return null;
        }

        public void AddLike(UserLike like)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.AddLike";
            command.Parameters.AddWithValue("@SourceUserId", like.SourceUserId);
            command.Parameters.AddWithValue("@LikedUserId", like.LikedUserId);
            command.ExecuteNonQueryAsync();
        }
    }
}
    
