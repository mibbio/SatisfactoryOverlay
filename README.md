# SatisfactoryOverlay
Embed live information of your savegame/session in OBS. Displayed data will be updated on every autosave for selected savegame.

![application window](https://i.imgur.com/YUQlIhP.png)

[Short video of application in action](https://www.youtube.com/watch?v=7cgMVmdSLbA)
(don't mind the german text, the latest version is in english & german, depending on your OS locale)
***
## Usage / Setup
### For OBS Studio only
- Install and setup [WebSocket plugin for OBS](https://github.com/Palakis/obs-websocket/releases)
### For StreamlabsOBS only
- Open `Settings->Remote Control` in StreamlabsOBS
- Click on QR code and then on `Details` to get the `port` and `API Token`
### For both
- Create new (empty) `Text (GDI+)` source in OBS
- Download an extract [latest version](https://github.com/mibbio/SatisfactoryOverlay/releases/latest) of SatisfactoryOverlay
- Start `SatisfactoryOverlay.exe`
- To close the application, right click on it's icon in the system tray
***
## Submit translations
- make a copy of `lang\translation_de.txt` file and rename to specific language
- replace `Comment.de` and `.de` column titles with specific language and use that columns for the translation
- columns are tab seperated
- create pull request for translated file
***
## 3rd party content
- Application icon made by <a href="http://www.freepik.com/" title="Freepik">Freepik</a> from <a href="https://www.flaticon.com/" title="Flaticon"> www.flaticon.com</a>
