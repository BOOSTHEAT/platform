import QtQuick 2.13
import QtQuick.Controls 2.13
import Runtime 1.0

Column {
  id: root
  property var runtime
  property var categories
  property var categoryFromPath
  property var screensForCategory
  property var defaultValues
  
  function getDefaultValue(urn) {
    var screenValues = root.defaultValues[root.runtime.router.currentPath];
    return screenValues[urn];
  }

  RuntimeEvents {
    runtime: root.runtime
    onEnterScreen: {
      categorySelector.initialize(path);
    }
  }
  ComboBox {
    id: categorySelector
    width: 300
    function initialize(screenPath) {
      var category = root.categoryFromPath[screenPath];
      currentIndex = indexOfValue(category);
      screenSelector.initialize(currentText,screenPath);
    }
    onActivated: {
      screenSelector.initialize(currentText,root.runtime.router.currentPath);
      root.runtime.router.navigate(screenSelector.currentValue);
    }
    model: root.categories
  }
  ComboBox {
    id: screenSelector
    width: 300
    textRole: 'title'
    valueRole: 'path'
    function initialize(categoryText,screenPath) {
      model = root.screensForCategory[categoryText];
      var index = indexOfValue(screenPath);
      currentIndex = index < 0 ? 0 : index;
    }
    onActivated: {
      root.runtime.router.navigate(currentValue);
    }
  }
}