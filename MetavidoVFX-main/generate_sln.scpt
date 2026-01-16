tell application "Terminal"
    do script "open -a \"/Applications/Unity/Hub/Editor/6000.2.14f1/Unity.app\" \"/Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main\""
end tell
delay 15 -- wait 15 seconds for Unity to load and generate files
tell application "System Events"
    tell process "Unity"
        keystroke "q" using command down -- Cmd+Q to quit
    end tell
end tell