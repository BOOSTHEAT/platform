import QtQuick 2.13
import QtQuick.Controls 2.13
import Shared 1.0

SwipeView {
  id: root
  property var runtime
  property var name
  property var position : currentIndex
  property var complete : false
  
  Component.onCompleted: {
    complete = true
  }
  
  onPositionChanged: {
    Logger.debug(`Initialize swipe to position ${position} in ${name}`);
    setCurrentIndex(position);
  }
  
  onCurrentIndexChanged: {
    if(!complete)
      return;
    Logger.debug(`Swipe to index ${currentIndex} in ${name}`);
    
    runtime.router.changeCurrentPathBy( isAtMyRoute );
    runtime.notifyUserAction()
  }
  
  function isAtMyRoute(route) {
    return route && route.file.includes(name) && route.args && route.args.position === currentIndex;
  }

}
