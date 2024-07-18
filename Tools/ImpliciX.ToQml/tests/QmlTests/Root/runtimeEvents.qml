import QtTest 1.2
import QtQuick 2.13
import Runtime 1.0
import Helpers 1.0

TestCase {
    id: root
    name: "runtimeEvents"

    function test_foo() {
        var sut = sutFactory.createObject(root);
        sut.runtime.router.navigate("foo");
        compare(sut.spyEnterScreen.count, 1, "1st enter");
        compare(sut.spyEnterScreen.signalArguments[0][0], "foo", "enter foo");
        compare(sut.spyLeaveScreen.count, 1, "left default screen");
        compare(sut.spyLeaveScreen.signalArguments[0][0], "bar", "leave bar");
        sut.runtime.router.navigate("bar");
        compare(sut.spyEnterScreen.count, 2, "2nd enter");
        compare(sut.spyEnterScreen.signalArguments[1][0], "bar", "enter bar");
        compare(sut.spyLeaveScreen.count, 2, "2nd leave");
        compare(sut.spyLeaveScreen.signalArguments[1][0], "foo", "leave foo");
    }

    Component {
        id: sutFactory

        RuntimeEvents {
            id: rte
            runtime: FakeRuntime {         
                defaultPath: "bar"
                routes: {
                    "foo": { file:"foo.qml" },
                    "bar": { file:"bar.qml" }
                }
            }

            property var spyEnterScreen: SignalSpy {
                target: rte
                signalName: "enterScreen"
            }    
        
            property var spyLeaveScreen: SignalSpy {
                target: rte
                signalName: "leaveScreen"
            }    

        }
    }
}
