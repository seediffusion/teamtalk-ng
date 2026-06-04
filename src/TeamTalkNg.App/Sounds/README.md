# TeamTalk NG Sound Packs

Place default TeamTalk sound files directly in this `Sounds` folder. Place alternate sound packs in their own subfolders.

The bundled WAV files and official file-name mapping follow BearWare TeamTalk 5's `Setup/Client/Sounds` layout. Bundled selectable packs include `Majorly-G`, `Old`, and `Galaxia`.

```text
Sounds\
  newuser.wav
  removeuser.wav
  channel_msg.wav
  user_msg.wav
  My Pack\
    newuser.wav
    removeuser.wav
    channel_msg.wav
    user_msg.wav
```

TeamTalk NG follows the official TeamTalk sound pack layout: the root `Sounds` folder is the Default pack, and each subfolder is a selectable sound pack. Existing TeamTalk sound pack folders can be copied here as-is.

Official TeamTalk event file names:

- `newuser.wav`
- `removeuser.wav`
- `serverlost.wav`
- `user_msg.wav`
- `user_msg_sent.wav`
- `channel_msg.wav`
- `channel_msg_sent.wav`
- `broadcast_msg.wav`
- `hotkey.wav`
- `videosession.wav`
- `desktopsession.wav`
- `fileupdate.wav`
- `filetx_complete.wav`
- `questionmode.wav`
- `desktopaccessreq.wav`
- `logged_on.wav`
- `logged_off.wav`
- `vox_enable.wav`
- `vox_disable.wav`
- `mute_all.wav`
- `unmute_all.wav`
- `txqueue_start.wav`
- `txqueue_stop.wav`
- `voiceact_on.wav`
- `voiceact_off.wav`
- `vox_me_enable.wav`
- `vox_me_disable.wav`
- `intercept.wav`
- `interceptEnd.wav`
- `typing.wav`

TeamTalk NG first looks for these official names. It also accepts friendly fallback names case-insensitively, ignoring punctuation, so custom files like `userjoined.wav`, `user_joined.wav`, and `User Joined.wav` can still work.
