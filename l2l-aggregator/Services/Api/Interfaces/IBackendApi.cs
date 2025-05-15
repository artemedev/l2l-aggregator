using l2l_aggregator.Models;
using Refit;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Api.Interfaces
{
    public interface IAuthApi
    {
        [Post("/api/Auth/ARM_DEVICE_REGISTRATION")]
        Task<ArmDeviceRegistrationResponse> RegisterDevice([Body] ArmDeviceRegistrationRequest registrationData);

        [Post("/api/Auth/USER_AUTH")]
        Task<UserAuthResponse> UserAuth([Body] UserAuthRequest request);
    }

    public interface ITaskApi
    {
        [Post("/api/data/ARM_JOB")]
        Task<ArmJobResponse> GetJobs([Body] ArmJobRequest request);

        [Post("/api/data/ARM_JOB_INFO")]
        Task<ArmJobInfoResponse> GetJob([Body] ArmJobInfoRequest request);

        [Post("/api/data/ARM_JOB_SGTIN")]
        Task<ArmJobSgtinResponse> GetJobSgtin([Body] ArmJobSgtinRequest request);

        [Post("/api/data/ARM_JOB_SSCC")]
        Task<ArmJobSsccResponse> GetJobSscc([Body] ArmJobSsccRequest request);
    }
}
