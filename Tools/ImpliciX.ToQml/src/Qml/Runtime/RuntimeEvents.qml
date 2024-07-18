import QtQuick 2.13
import Shared 1.0

QtObject {
  property var runtime
  
  Component.onCompleted: {
    runtime.router.onDisplayedPathChanged.connect(onDisplayedPathChanged);
  }
  
  signal enterScreen(string path)
  signal leaveScreen(string path)
  
  function onDisplayedPathChanged() {
    if(!runtime || !runtime.router)
        return;
    if(runtime.router.previouslyDisplayedPath)
      leaveScreen(runtime.router.previouslyDisplayedPath);
    enterScreen(runtime.router.displayedPath);
  }
}