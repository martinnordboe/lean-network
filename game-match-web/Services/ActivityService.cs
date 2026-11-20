using game_match.Models;
using game_match.Repositories;

namespace game_match.Services
{
    public interface IActivityService
    {
        int ParticipationCount { get; set; }

        Task<List<Activity>> GetAllUserActivities(User user);

        Activity CreateNewActivity(int ownerId);

        Task<List<Activity>> GetAllActivitiesAsync();

        Task AddToRepoAsync(Activity activity);

        Task<Activity> GetActivityById(int id);

        Task RemoveActivityAsync(int id);

        Task UpdateToRepoAsync(Activity activity);

        // Tilmeldinger
        Task<bool> SignUpAsync(int activityId, int userId, int maxParticipant);

        Task SignOffAsync(int activityId, int userId);

        Task<bool> IsSignedUpAsync(int activityId, int userId);

        Task<int> GetParticipantCountAsync(int activityId);
    }

    public class ActivityService : IActivityService
    {
        //private static readonly HashSet<(int ActivityId, int UserId)> signUps = new();

        //private static readonly object signUpsLock = new();

        private readonly IActivityRepository repo;

        public int ParticipationCount { get; set; } = 7;

        public ActivityService(IActivityRepository repo)
        {
            this.repo = repo;
        }

        public Activity CreateNewActivity(int ownerId)
        {
            return new Activity(ownerId);
        }

        public async Task<List<Activity>> GetAllActivitiesAsync()
        {
            return await repo.GetAllAsync();
        }

        public async Task<Activity> GetActivityById(int id)
        {
            return await repo.GetActivityByIdAsync(id);
        }

        public async Task AddToRepoAsync(Activity activity)
        {
            await repo.AddAsync(activity);
        }

        public async Task RemoveActivityAsync(int id)
        {
            Activity activityToBeDeleted = await GetActivityById(id);

            if (activityToBeDeleted != null)
            {
                await repo.RemoveActivityByIdAsync(id);
            }
        }

        public async Task UpdateToRepoAsync(Activity activity)
        {
            await repo.UpdateActivityAsync(activity);
        }

        // ---------- Tilmeldinger ----------

        public async Task<bool> SignUpAsync(int activityId, int userId, int maxParticipant)
        {
            int count = await repo.GetActivityParticipantCount(activityId);
            if (count >= maxParticipant)
            {
                // Aktiviteten er fuld
                //return Task.FromResult(false);
                return false;
            }

            //signUps.Add((activityId, userId));
            bool signedUp = await repo.SignUpToActivityAsync(activityId, userId);

            //return Task.FromResult(true);
            return signedUp;
        }

        public async Task SignOffAsync(int activityId, int userId)
        {
            //lock (signUpsLock)
            //{
            //    signUps.Remove((activityId, userId));
            //}

            //return Task.CompletedTask;
            await repo.RemoveFromActivityAsync(activityId, userId);

        }

        public async Task<bool> IsSignedUpAsync(int activityId, int userId)
        {
            //lock (signUpsLock)
            //{
            //    return Task.FromResult(signUps.Contains((activityId, userId)));
            //}
            return await repo.IsSignedUpToActivityAsync(activityId, userId);
        }

        public async Task<int> GetParticipantCountAsync(int activityId)
        {
            //lock (signUpsLock)
            //{
            //    return Task.FromResult(signUps.Count(s => s.ActivityId == activityId));
            //}
            return await repo.GetActivityParticipantCount(activityId);
        }

        public async Task<List<Activity>> GetAllUserActivities(User user)
        {
            return await repo.GetAllUserActivities(user.Id);
        }
    }
}