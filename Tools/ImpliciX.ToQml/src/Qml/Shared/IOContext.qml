pragma Singleton
import QtQuick 2.5
import QtQml 2.5

QtObject {
  function editTextBox(textBox) {
    if(textBox.focus)
      keyboardHeader = textBox.helper;
  }
  property string keyboardHeader : ''
}