namespace TeamTalkNg.App.Services;

public interface ISoundEventService
{
    IReadOnlyList<SoundEventDefinition> GetSoundEvents();

    IReadOnlyList<SoundPackOption> GetSoundPacks();

    string GetSoundFileName(SoundEvent soundEvent, string soundPack);

    void Configure(bool enabled, string soundPack, int volume, IReadOnlyDictionary<string, bool> eventEnabled);

    void Play(SoundEvent soundEvent);

    void Preview(SoundEvent soundEvent, string soundPack, int volume);
}
