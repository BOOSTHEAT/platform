import QtQuick 2.13
import QtQuick.Controls 2.13

Loader {
  active: SwipeView.isCurrentItem || SwipeView.isNextItem || SwipeView.isPreviousItem
}
