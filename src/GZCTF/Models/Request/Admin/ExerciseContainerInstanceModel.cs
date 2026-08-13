using GZCTF.Utils;

namespace GZCTF.Models.Request.Admin;

/// <summary>
/// Exercise (training range) container instance information (Admin/Monitor)
/// </summary>
public class ExerciseContainerInstanceModel
{
    /// <summary>
    /// User who owns the instance
    /// </summary>
    public UserInfoModel? User { get; set; }

    /// <summary>
    /// Challenge
    /// </summary>
    public ChallengeModel? Challenge { get; set; }

    /// <summary>
    /// Container image
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Container database ID
    /// </summary>
    public Guid ContainerGuid { get; set; }

    /// <summary>
    /// Container ID
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Container creation time
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Expected container stop time
    /// </summary>
    public DateTimeOffset ExpectStopAt { get; set; } = DateTimeOffset.UtcNow + TimeSpan.FromHours(2);

    /// <summary>
    /// Access IP
    /// </summary>
    public string IP { get; set; } = string.Empty;

    /// <summary>
    /// Access port
    /// </summary>
    public int Port { get; set; }

    internal static ExerciseContainerInstanceModel FromContainer(Container container)
    {
        var instance = container.ExerciseInstance;
        var chal = instance?.Exercise;

        var model = new ExerciseContainerInstanceModel
        {
            Image = container.Image,
            ContainerGuid = container.Id,
            ContainerId = container.ContainerId,
            StartedAt = container.StartedAt,
            ExpectStopAt = container.ExpectStopAt,
            // fallback to host if public is null
            IP = container.PublicIP ?? container.IP,
            Port = container.PublicPort ?? container.Port
        };

        if (instance?.User is not null)
            model.User = UserInfoModel.FromUserInfo(instance.User);

        if (chal is not null)
            model.Challenge = new ChallengeModel(chal.Id, chal.Title, chal.Category);

        return model;
    }
}
