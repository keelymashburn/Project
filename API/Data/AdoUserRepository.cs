using API.DTOs;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API.Data
{
    public class AdoUserRepository : IUserRepository
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public AdoUserRepository(IMapper mapper, IConfiguration configuration, DataContext context)
        {
            _mapper = mapper;
            _context = context;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<MemberDto> GetMemberAsync(string username, bool isCurrentUser)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetMemberAsync";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@username", username);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                return new MemberDto()
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("UserName"),
                    PhotoUrl = reader.GetString("Url"),
                    Age = reader.GetDateTime("DateOfBirth").CalculateAge(),
                    KnownAs = reader.GetString("KnownAs"),
                    Created = reader.GetDateTime("Created"),
                    LastActive = reader.GetDateTime("LastActive"),
                    Gender = reader.GetString("Gender"),
                    Introduction = reader.GetString("Introduction"),
                    LookingFor = reader.GetString("LookingFor"),
                    Interests = reader.GetString("Interests"),
                    City = reader.GetString("City"),
                    Country = reader.GetString("Country"),
                    Photos = new List<PhotoDto> 
                    {
                        new PhotoDto
                        {
                            Id = reader.GetInt32("PhotoID"),
                            Url = reader.GetString("Url"),
                            IsApproved = reader.GetBoolean("IsApproved"),
                            IsMain = reader.GetBoolean("IsMain")
                        }
                    }
                };
            }

            return null;
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetMembersAsync";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@gender", userParams.Gender);
            command.Parameters.AddWithValue("@MaxAge", userParams.MaxAge);
            command.Parameters.AddWithValue("@MinAge", userParams.MinAge);

            using var reader = await command.ExecuteReaderAsync();
            var result = new List<MemberDto>();
            while (reader.Read())
            {
                result.Add(
                    new MemberDto
                    {
                        Id = reader.GetInt32("Id"),
                        Username = reader.GetString("UserName"),
                        PhotoUrl = reader.GetString("Url"),
                        Age = reader.GetDateTime("DateOfBirth").CalculateAge(),
                        KnownAs = reader.GetString("KnownAs"),
                        Created = reader.GetDateTime("Created"),
                        LastActive = reader.GetDateTime("LastActive"),
                        Gender = reader.GetString("Gender"),
                        Introduction = reader.GetString("Introduction"),
                        LookingFor = reader.GetString("LookingFor"),
                        Interests = reader.GetString("Interests"),
                        City = reader.GetString("City"),
                        Country = reader.GetString("Country"),
                        Photos = new List<PhotoDto>
                        {
                            new PhotoDto
                            {
                                Id = reader.GetInt32("PhotoID"),
                                Url = reader.GetString("Url"),
                                IsApproved = reader.GetBoolean("IsApproved"),
                                IsMain = reader.GetBoolean("IsMain")
                            }
                        }
                    });
            }

            var pagedlist = new PagedList<MemberDto>(result, result.Count(), userParams.PageNumber, userParams.PageSize);

            if (pagedlist != null) return pagedlist;

            return null;
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUserByIdAsync";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@id", id);

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

            while (reader.Read())
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
                        DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
                        MessageSent = reader.GetDateTime("MessageSent"),
                        SenderDeleted = reader.GetBoolean("SenderDeleted"),
                        RecipientDeleted = reader.GetBoolean("RecipientDeleted")
                    });
            }

            reader.NextResult();

            while (reader.Read())
            {
                messagesSent.Add(
                new Message
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
                });

            }

            foreach (Photo photo in photos)
            {
                photo.AppUser = appUser;
            }

            foreach(UserLike user in likedByUsers)
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

            if (appUser != null) return appUser;

            return null;
        }

        public async Task<AppUser> GetUserByPhotoId(int photoId)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUserByPhotoId";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@photoId", photoId);

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

            while (reader.Read())
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
                        DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
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
                    DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
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

            if (appUser != null) return appUser;

            return null;
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUserByUsername";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@username", username);

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

            while (reader.Read())
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
                        DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
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
                    DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
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

            if (appUser != null) return appUser;

            return null;
        }

        public async Task<string> GetUserGender(string username)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUserGender";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Username", username);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
               var gender = reader.GetString("Gender");
               return gender;
            }

            return null;
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUsersAsync";
            command.CommandType = CommandType.StoredProcedure;

            var appUsers = new List<AppUser>();
            var photos = new List<Photo>();
            var likedByUsers = new List<UserLike>();
            var likedUsers = new List<UserLike>();
            var messagesReceived = new List<Message>();
            var messagesSent = new List<Message>();
            var userRoles = new List<AppUserRole>();

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                appUsers.Add(
                    new AppUser
                    {
                        Id = reader.GetInt32("Id"),
                        DateOfBirth = reader.GetDateTime("DateOfBirth"),
                        UserName = reader.GetString("UserName"),
                        KnownAs = reader.GetString("KnownAs"),
                        Created = reader.GetDateTime("Created"),
                        LastActive = reader.GetDateTime("LastActive"),
                        Gender = reader.GetString("Gender"),
                        Introduction = reader.GetString("Introduction"),
                        LookingFor = reader.GetString("LookingFor"), 
                        Interests = reader.GetString("Interests"),
                        City = reader.GetString("City"),
                        Country = reader.GetString("Country"),
                    });
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

            while (reader.Read())
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
                        DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
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
                    DateRead = reader.IsDBNull("DateRead") ? null : reader.GetDateTime("DateRead"),
                    MessageSent = reader.GetDateTime("MessageSent"),
                    SenderDeleted = reader.GetBoolean("SenderDeleted"),
                    RecipientDeleted = reader.GetBoolean("RecipientDeleted")
                });

            }

            foreach (AppUser appUser in appUsers)
            {
                foreach (Photo photo in photos)
                {
                    if(photo.AppUserId == appUser.Id)
                    {
                        photo.AppUser = appUser;
                        appUser.Photos.Add(photo);
                    }
                }
                foreach (UserLike like in likedByUsers)
                {
                    if (like.LikedUserId == appUser.Id)
                    {
                        like.LikedUser = appUser;
                        appUser.LikedByUsers.Add(like);
                    }
                }
                foreach (UserLike like in likedUsers)
                {
                    if (like.SourceUserId == appUser.Id)
                    {
                        like.SourceUser = appUser; ;
                        appUser.LikedUsers.Add(like);
                    }
                }
                foreach (Message message in messagesReceived)
                {
                    if(message.RecipientId == appUser.Id)
                    {
                        message.Recipient = appUser;
                        appUser.MessagesReceived.Add(message);
                    }
                }
                foreach (Message message in messagesSent)
                {
                    if (message.SenderId == appUser.Id)
                    {
                        message.Sender = appUser;
                        appUser.MessagesSent.Add(message);
                    }
                }
            }


            if (appUsers != null) return appUsers;

            return null;
        }

        public async void Update(AppUser user)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.UpdateUser";
            command.CommandType = CommandType.StoredProcedure;
            //message params
            command.Parameters.AddWithValue("@CurrentUsername", user.UserName);
            command.Parameters.AddWithValue("@Introduction", user.Introduction);
            command.Parameters.AddWithValue("@LookingFor", user.LookingFor);
            command.Parameters.AddWithValue("@Interests", user.Interests);
            command.Parameters.AddWithValue("@City", user.City);
            command.Parameters.AddWithValue("@Country", user.Country);
            await command.ExecuteNonQueryAsync();
        }
    }
}
