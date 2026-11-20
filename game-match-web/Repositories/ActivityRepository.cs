using game_match.Models;
using game_match.tools;
using MySqlConnector;



namespace game_match.Repositories
{
    public interface IActivityRepository
    {
        Task<bool> SignUpToActivityAsync(int activityId, int userId);
        Task<bool> RemoveFromActivityAsync(int activtyId, int userId);
        Task<bool> IsSignedUpToActivityAsync(int activtyId, int userId);
        Task<int> GetActivityParticipantCount(int activtyId);

        Task<List<Activity>> GetAllUserActivities(int userId);
        Task AddAsync(Activity activity);

        Task<List<Activity>> GetAllAsync();

        Task<Activity> GetActivityByIdAsync(int id);

        Task RemoveActivityByIdAsync(int id);

        Task UpdateActivityAsync(Activity activity);
    }
    public class ActivityRepository : IActivityRepository
    {

        private readonly DbConnectionFactory connectionFactory;

        public ActivityRepository(DbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public async Task AddAsync(Activity activity)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO activities (title, info, address, start, max_participants, owner_id)
                VALUES (@title, @info, @address, @start, @maxParticipants, @ownerId);
            ";

            command.Parameters.AddWithValue("@title", activity.ActivityTitle);
            command.Parameters.AddWithValue("@info", activity.ActivityInfo);
            command.Parameters.AddWithValue("@address", activity.Address);
            command.Parameters.AddWithValue("@start", activity.ActivityStartTime);
            command.Parameters.AddWithValue("@maxParticipants", activity.MaxParticipant);
            command.Parameters.AddWithValue("@ownerId", activity.OwnerId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Activity>> GetAllUserActivities(int userId)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
                SELECT a.*
                FROM activities a
                INNER JOIN activity_participants ap
                    ON a.id = ap.activity_id
                WHERE ap.user_id = @userId;
            ";

            command.Parameters.AddWithValue("@userId", userId);

            using MySqlDataReader reader = await command.ExecuteReaderAsync();

            List<Activity> activities = new List<Activity>();

            while (await reader.ReadAsync())
            {
                Activity activity = new Activity
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    //OwnerId = reader.GetInt32(reader.GetOrdinal("ownerId")),
                    ActivityTitle = reader.GetString(reader.GetOrdinal("title")),
                    ActivityInfo = reader.GetString(reader.GetOrdinal("info")),
                    Address = reader.GetString(reader.GetOrdinal("address")),
                    ActivityStartTime = reader.GetDateTime(reader.GetOrdinal("start")),
                    MaxParticipant = reader.GetInt32(reader.GetOrdinal("max_participants")),
                    OwnerId = reader.GetInt32(reader.GetOrdinal("owner_id")),
                };

                activities.Add(activity);
            }

            return activities;
        }

        public async Task<List<Activity>> GetAllAsync()
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
				SELECT * FROM activities;
			";

            using MySqlDataReader reader = await command.ExecuteReaderAsync();

            List<Activity> activities = new List<Activity>();

            while (await reader.ReadAsync())
            {
                Activity activity = new Activity
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    //OwnerId = reader.GetInt32(reader.GetOrdinal("ownerId")),
                    ActivityTitle = reader.GetString(reader.GetOrdinal("title")),
                    ActivityInfo = reader.GetString(reader.GetOrdinal("info")),
                    Address = reader.GetString(reader.GetOrdinal("address")),
                    ActivityStartTime = reader.GetDateTime(reader.GetOrdinal("start")),
                    MaxParticipant = reader.GetInt32(reader.GetOrdinal("max_participants")),
                    OwnerId = reader.GetInt32(reader.GetOrdinal("owner_id")),
                };

                activities.Add(activity);
            }

            return activities;
        }

        public async Task<Activity> GetActivityByIdAsync(int id)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @"
				SELECT * FROM activities WHERE id = @id;
			";

            command.Parameters.AddWithValue("@id", id);

            using MySqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Activity activity = new Activity
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    //OwnerId = reader.GetInt32(reader.GetOrdinal("ownerId")),
                    ActivityTitle = reader.GetString(reader.GetOrdinal("title")),
                    ActivityInfo = reader.GetString(reader.GetOrdinal("info")),
                    Address = reader.GetString(reader.GetOrdinal("address")),
                    ActivityStartTime = reader.GetDateTime(reader.GetOrdinal("start")),
                    MaxParticipant = reader.GetInt32(reader.GetOrdinal("max_participants")),
                    OwnerId = reader.GetInt32(reader.GetOrdinal("owner_id")),
                };

                return activity;
            }

            throw new Exception("Activity not found");
        }

        public async Task RemoveActivityByIdAsync(int id)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @" DELETE FROM activities WHERE id = @id;";

            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();


        }



        public async Task UpdateActivityAsync(Activity activity)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();

            await connection.OpenAsync();

            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = @" UPDATE activities SET title = @title, info = @info, owner_id = @ownerId, address = @address, start = @start, max_Participants = @maxParticipants WHERE id = @id; ";


            command.Parameters.AddWithValue("@id", activity.Id);
            command.Parameters.AddWithValue("@title", activity.ActivityTitle);
            command.Parameters.AddWithValue("@info", activity.ActivityInfo);
            command.Parameters.AddWithValue("@address", activity.Address);
            command.Parameters.AddWithValue("@start", activity.ActivityStartTime);
            command.Parameters.AddWithValue("@maxParticipants", activity.MaxParticipant);
            command.Parameters.AddWithValue("@ownerId", activity.OwnerId);

            await command.ExecuteNonQueryAsync();
        }

        // Bruges af Rasmus' ACtivity stuff

        public async Task<bool> SignUpToActivityAsync(int activityId, int userId)
        {
            try
            {
                using MySqlConnection connection = connectionFactory.CreateConnection();

                await connection.OpenAsync();

                using MySqlCommand command = connection.CreateCommand();

                command.CommandText = @"
                INSERT INTO activity_participants (activity_id, user_id)
                VALUES (@activity_id, @userId);
            ";

                command.Parameters.AddWithValue("@activity_id", activityId);
                command.Parameters.AddWithValue("@userId", userId);

                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveFromActivityAsync(int activityId, int userId)
        {
            try
            {
                using MySqlConnection connection = connectionFactory.CreateConnection();

                await connection.OpenAsync();

                using MySqlCommand command = connection.CreateCommand();

                command.CommandText = @"
                DELETE FROM activity_participants WHERE activity_id = @activity_id AND user_id = @userId;";

                command.Parameters.AddWithValue("@activity_id", activityId);
                command.Parameters.AddWithValue("@userId", userId);

                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsSignedUpToActivityAsync(int activityId, int userId)
        {
            try
            {
                using MySqlConnection connection = connectionFactory.CreateConnection();

                await connection.OpenAsync();

                using MySqlCommand command = connection.CreateCommand();

                command.CommandText = "SELECT COUNT(*) FROM activity_participants WHERE activity_id = @activity_id AND user_id = @userId;";

                command.Parameters.AddWithValue("@activity_id", activityId);
                command.Parameters.AddWithValue("@userId", userId);

                return Convert.ToBoolean(await command.ExecuteScalarAsync());

            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetActivityParticipantCount(int activityId)
        {
            using MySqlConnection connection = connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using MySqlCommand command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM activity_participants WHERE activity_id = @activity_id;";
            command.Parameters.AddWithValue("@activity_id", activityId);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }





    }
}
