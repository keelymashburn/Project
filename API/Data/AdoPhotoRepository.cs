using API.DTOs;
using API.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API.Data
{
    public class AdoPhotoRepository : IPhotoRepository
    {
        private readonly IConfiguration _configuration;

        public AdoPhotoRepository(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<Photo> GetPhotoById(int id)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetPhotoById";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                return new Photo()
                {
                    Id = reader.GetInt32("Id"),
                    Url = reader.GetString("Url"),  
                    IsMain = reader.GetBoolean("IsMain"),
                    IsApproved = reader.GetBoolean("IsApproved"),
                    PublicId = reader.GetString("PublicId"),
                    AppUserId = reader.GetInt32("AppUserId")
                };
            }

            return null;

        }

        public async Task<IEnumerable<PhotoForApprovalDto>> GetUnapprovedPhotos()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.GetUnapprovedPhotos";
            command.CommandType = CommandType.StoredProcedure;

            using var reader = await command.ExecuteReaderAsync();
            var result = new List<PhotoForApprovalDto>();
            while (reader.Read())
            {
                result.Add(new PhotoForApprovalDto()
                {
                    Id = reader.GetInt32("Id"),
                    Url = reader.GetString("Url"),
                    IsApproved = reader.GetBoolean("IsApproved"),
                    Username = reader.GetString("Username")
                });
            }

            return null;
        }

        public async void RemovePhoto(Photo photo)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.RemovePhoto";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Id", photo.Id);

            await command.ExecuteNonQueryAsync();
        
        }

        public async void AddPhoto(Photo photo)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.AddPhoto";
            command.Parameters.AddWithValue("@Url", photo.Url);
            command.Parameters.AddWithValue("@IsMain", photo.IsMain);
            command.Parameters.AddWithValue("@IsApproved", photo.IsApproved);
            command.Parameters.AddWithValue("@PublicId", photo.PublicId);
            command.Parameters.AddWithValue("@AppUserId", photo.AppUserId);

            await command.ExecuteNonQueryAsync();
        }

        public async void UpdatePhoto(Photo photo)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "dbo.UpdatePhoto";
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.AddWithValue("@Id", photo.Id);
            command.Parameters.AddWithValue("@Url", photo.Url);
            command.Parameters.AddWithValue("@IsMain", photo.IsMain);
            command.Parameters.AddWithValue("@IsApproved", photo.IsApproved);
            command.Parameters.AddWithValue("@AppUserId", photo.AppUserId);

            await command.ExecuteNonQueryAsync();
        }
    }
}
