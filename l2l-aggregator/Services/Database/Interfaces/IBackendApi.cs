using l2l_aggregator.Models;
using MedtechtdApp.Models;
using Refit;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Database.Interfaces
{
    public interface IAuthApi
    {
        [Post("/api/Auth/ARM_DEVICE_REGISTRATION")]
        [Headers("MTDApikey: e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de")]
        Task<ArmDeviceRegistrationResponse> RegisterDevice([Body] ArmDeviceRegistrationRequest registrationData);

        [Post("/api/Auth/USER_AUTH")]
        [Headers("MTDApikey: e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de")]
        Task<UserAuthResponse> UserAuth([Body] UserAuthRequest request);
    }
    public interface ITaskApi
    {
        [Post("/api/data/ARM_JOB")]
        [Headers("MTDApikey: e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de")]
        Task<ArmJobResponse> GetJobs([Body] ArmJobRequest request);

        [Post("/api/data/ARM_JOB_INFO")]
        [Headers("MTDApikey: e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de")]
        Task<ArmJobInfoResponse> GetJob([Body] ArmJobInfoRequest request);

        [Post("/api/data/ARM_JOB_SGTIN")]
        [Headers("MTDApikey: e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de")]
        Task<ArmJobSgtinResponse> GetJobSgtin([Body] ArmJobSgtinRequest request);

        [Post("/api/data/ARM_JOB_SSCC")]
        [Headers("MTDApikey: e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de")]
        Task<ArmJobSsccResponse> GetJobSscc([Body] ArmJobSsccRequest request);
    }
}
