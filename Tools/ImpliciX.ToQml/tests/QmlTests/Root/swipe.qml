import QtTest 1.2
import QtQuick 2.13
import QtQuick.Controls 2.13
import Runtime 1.0
import Helpers 1.0
import Shared 1.0

TestCase {
    id: root
    name: "swipe"

    function test_swipe_is_updated_on_navigation() {
        Logger.initialize('VERBOSE');
        var sut = createSut("bar");
        assertEqual("start index", sut.currentIndex, 1);
        
        sut.runtime.router.navigate('qix')
        assertEqual("new index", sut.currentIndex, 2);
        
        sut.runtime.router.navigate('foo')
        assertEqual("new index", sut.currentIndex, 0);
        
        sut.runtime.router.navigate('bar')
        assertEqual("new index", sut.currentIndex, 1);
    }

    function test_current_route_is_changed_on_swipe() {
        var sut = createSut("qix");
        assertEqual("default path", sut.runtime.router.currentPath, "qix");

        sut.setCurrentIndex(1);
        assertEqual("new path", sut.runtime.router.currentPath, "bar");

        sut.setCurrentIndex(0);
        assertEqual("new path", sut.runtime.router.currentPath, "foo");

        sut.setCurrentIndex(2);
        assertEqual("new path", sut.runtime.router.currentPath, "qix");
    }
    
    function assertEqual(name,actual,expected) {
        verify(actual === expected, `${name} should be ${expected} instead of ${actual}`);
    }

    function createSut(defaultPath) {
        var sut = sutComponent.createObject(root, {defaultPath: defaultPath});
        return sut;
    }
    
    Component {
        id: sutComponent
        SwipeScreen {
            id: ss
            name: "MySwipe.qml"
            property var defaultPath
    
            runtime: FakeRuntime {
                id:rt
                defaultPath: ss.defaultPath
                routes: {
                    "foo": { file:"MySwipe.qml", args: { position: 0 } },
                    "bar": { file:"MySwipe.qml", args: { position: 1 } },
                    "qix": { file:"MySwipe.qml", args: { position: 2 } },
                    "zug": { file:"MySwipe.qml", args: { position: 3 } }
                }

                onViewReplaced: {
                  var expectedPath = router.getPathBy(r => r.args.position === rt.theStackView.properties.position);
                  ss.position = rt.theStackView.properties.position;
                  root.assertEqual("path", router.currentPath, expectedPath);
                }
            }
            
            SwipeItem {
              sourceComponent: Item {
                  Component.onCompleted: {
                    console.log('Loaded 0');
                  }
              }
            }
            SwipeItem {
              sourceComponent: Item {
                  Component.onCompleted: {
                    console.log('Loaded 1');
                  }
              }
            }
            SwipeItem {
              sourceComponent: Item {
                  Component.onCompleted: {
                    console.log('Loaded 2');
                  }
              }
            }
            SwipeItem {
              sourceComponent: Item {
                  Component.onCompleted: {
                    console.log('Loaded 3');
                  }
              }
            }
            
        }
    }
    


}
