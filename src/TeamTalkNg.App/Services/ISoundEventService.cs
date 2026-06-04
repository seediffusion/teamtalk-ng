namespace TeamTalkNg.App.Services;

public interface ISoundEventService
{
    IReadOnlyList<SoundPackOption> GetSoundPacks();

    void Configure(bool enabled, string soundPack);

    void Play(SoundEvent soundEvent);
}
