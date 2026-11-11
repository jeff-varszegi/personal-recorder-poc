namespace PersonalRecorder.Tests.Core;

public sealed class IdentifierTests
{
    [Fact]
    public void New_identifiers_are_unique()
    {
        HashSet<RecordingId> ids = [];
        for (int i = 0; i < 10_000; i++)
        {
            Assert.True(ids.Add(RecordingId.New()));
        }
    }

    [Fact]
    public void New_identifiers_are_uuid_version_7()
    {
        Assert.Equal(7, RecordingId.New().Value.Version);
        Assert.Equal(7, CircleId.New().Value.Version);
    }

    [Fact]
    public void From_round_trips_the_underlying_value()
    {
        Guid value = Guid.CreateVersion7();

        Assert.Equal(value, DeviceId.From(value).Value);
        Assert.Equal(DeviceId.From(value), new DeviceId(value));
    }

    [Fact]
    public void Text_form_is_the_bare_value()
    {
        Guid value = Guid.CreateVersion7();

        Assert.Equal(value.ToString(), UserId.From(value).ToString());
    }

    [Fact]
    public void Default_is_the_empty_value()
    {
        Assert.Equal(Guid.Empty, default(AlertId).Value);
    }

    [Fact]
    public void Identifiers_of_different_kinds_are_never_equal_even_with_the_same_value()
    {
        Guid value = Guid.CreateVersion7();

        Assert.False(new RecordingId(value).Equals(new ArtifactId(value)));
        Assert.False(new ShareLinkId(value).Equals((object)new HeartbeatSessionId(value)));
    }
}
