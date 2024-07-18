import QtQuick 2.13
import QtQuick.Controls 2.13
import Shared 1.0

TextField {
  id: root
  property var runtime
  property string urn
  property string helper : runtime.cache.hasTranslations ? runtime.cache.translate(urn) : urn;

  onFocusChanged: {
    IOContext.editTextBox(this);
  }
  onTextEdited: {
    root.runtime.api.sendProperty(urn, text);
    root.runtime.notifyUserAction();
  }
}